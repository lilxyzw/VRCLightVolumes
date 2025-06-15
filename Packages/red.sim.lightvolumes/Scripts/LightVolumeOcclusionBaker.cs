#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace VRCLightVolumes
{
    public static class LightVolumeOcclusionBaker
    {
        private static float[] ComputeOcclusionFactors(
            Vector3[] probePositions,
            int[] perProbeLightIndices,
            IList<PointLightVolume> pixelLights,
            float[] pixelLightRadii,
            int resolution = 256) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Check how many probes need occlusion so we can report progress.
            int totalProbesNeedingOcclusion = 0;
            for (int probeIdx = 0; probeIdx < probePositions.Length; probeIdx++) {
                for (int channel = 0; channel < 4; channel++) {
                    if (perProbeLightIndices[probeIdx * 4 + channel] >= 0) {
                        totalProbesNeedingOcclusion++;
                    }
                }
            }
            
            // Get required assets
            Mesh sphereMesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            Shader occlusionShader = AssetDatabase.LoadAssetAtPath<Shader>("Packages/red.sim.lightvolumes/Shaders/OcclusionShader.shader");
            Material whiteMat = new Material(occlusionShader) { color = Color.white };
            Material blackMat = new Material(occlusionShader) { color = Color.black };
            ComputeShader countCS = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/red.sim.lightvolumes/Shaders/CountUnoccludedPixels.compute");
            int countKernel = countCS.FindKernel("CountUnoccludedPixels");
            countCS.GetKernelThreadGroupSizes(countKernel, out uint countKernelX, out uint countKernelY, out _);
            int ratioKernel = countCS.FindKernel("ComputeOcclusionRatio");
            
            // Create and initialize GPU resources
            RenderTexture tempRT = RenderTexture.GetTemporary(resolution, resolution, 16, RenderTextureFormat.R8);
            var nullRT = new RenderTargetIdentifier();
            Debug.Assert(tempRT.width % countKernelX == 0 && tempRT.height % countKernelY == 0);
            using GraphicsBuffer occlusionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, probePositions.Length * 4, sizeof(float));
            float[] occlusionBufferInit = new float[occlusionBuffer.count];
            Array.Fill(occlusionBufferInit, 1.0f); // Initialize to 1.0f - unoccluded
            occlusionBuffer.SetData(occlusionBufferInit);
            using GraphicsBuffer countBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 2, sizeof(uint));
            countBuffer.SetData(new uint[countBuffer.count]);

            // Find all GI contributors - these are the occluders
            MeshRenderer[] occluders = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(mr => GameObjectUtility.AreStaticEditorFlagsSet(mr.gameObject, StaticEditorFlags.ContributeGI))
                .ToArray();
            Mesh[] occluderMeshes = occluders.Select(mr => mr.GetComponent<MeshFilter>().sharedMesh).ToArray();
            Matrix4x4[] occluderMatrices = occluders.Select(mr => Matrix4x4.TRS(mr.transform.position, mr.transform.rotation, mr.transform.lossyScale)).ToArray();
            Bounds[] occluderBounds = occluders.Select(mr => mr.bounds).ToArray();
            
            // Set up command buffer with uniforms that don't change per probe
            using CommandBuffer cmd = new CommandBuffer();
            cmd.name = "Light Volume Occlusion Baking";
            
            cmd.SetComputeBufferParam(countCS, countKernel, "_Count", countBuffer);
            cmd.SetComputeTextureParam(countCS, countKernel, "_Texture", tempRT);
            cmd.SetComputeIntParam(countCS, "_TextureWidth", tempRT.width);
            cmd.SetComputeIntParam(countCS, "_TextureHeight", tempRT.height);
            
            cmd.SetComputeBufferParam(countCS, ratioKernel, "_Count", countBuffer);
            cmd.SetComputeBufferParam(countCS, ratioKernel, "_Occlusion", occlusionBuffer);
            
            // Setup some state for culling and progress reporting
            int probesProcessed = 0;
            Plane[] cullingPlanes = new Plane[6];
            List<int> occluderIndices = new List<int>(occluders.Length);
            
            // Rasterize the scene from each probes perspective, for each light that affects the probe.
            for (int probeIdx = 0; probeIdx < probePositions.Length; probeIdx++) {
                for (int channel = 0; channel < 4; channel++) {
                    // Check if we ran out of lights affecting the probe
                    int lightIndex = perProbeLightIndices[probeIdx * 4 + channel];
                    if (lightIndex < 0)
                        continue;
                    
                    Vector3 lightPosition = pixelLights[lightIndex].transform.position;
                    float lightRadius = pixelLightRadii[lightIndex];
                    
                    Vector3 probePosition = probePositions[probeIdx];
                    float distanceToLight = Vector3.Distance(probePosition, lightPosition);
                    float farClip = distanceToLight + lightRadius + 0.001f; // Add a bit of wiggle room to avoid precision issues
                    
                    // Calculate model, view, and projection matrices
                    Matrix4x4 lightToWorld = Matrix4x4.TRS(lightPosition, Quaternion.identity, 2 * lightRadius * Vector3.one); // Model
                    Matrix4x4 probeToWorld = Matrix4x4.LookAt(probePosition, lightPosition, Vector3.up);
                    Matrix4x4 yFlipMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
                    Matrix4x4 worldToProbe = yFlipMatrix * probeToWorld.inverse; // View
                    float fov = 2.0f * Mathf.Asin(lightRadius / distanceToLight) * Mathf.Rad2Deg;
                    Matrix4x4 probeToClip = Matrix4x4.Perspective(fov, 1, 0.01f, farClip); // Projection

                    // Cull occluders
                    GeometryUtility.CalculateFrustumPlanes(probeToClip * worldToProbe, cullingPlanes);
                    occluderIndices.Clear();
                    for (int occluderIdx = 0; occluderIdx < occluders.Length; occluderIdx++) {
                        if (GeometryUtility.TestPlanesAABB(cullingPlanes, occluderBounds[occluderIdx])) {
                            occluderIndices.Add(occluderIdx);
                        }
                    }
                    // If no potential occluders - the probe is fully unoccluded, so skip doing any work.
                    if (occluderIndices.Count == 0)
                        continue;
                    
                    // Draw the light mesh first
                    // TODO(pema99): Other light types
                    cmd.SetViewProjectionMatrices(worldToProbe, probeToClip);
                    cmd.SetRenderTarget(tempRT);
                    cmd.ClearRenderTarget(true, true, Color.black);
                    cmd.DrawMesh(sphereMesh, lightToWorld, whiteMat);
                    cmd.SetRenderTarget(nullRT);
                    
                    // Count unocccluded pixels - this is the total area of the light
                    cmd.SetBufferData(countBuffer, new uint[2]);
                    cmd.SetComputeIntParam(countCS, "_Pass", 0);
                    cmd.DispatchCompute(countCS, countKernel, tempRT.width / (int)countKernelX, tempRT.height / (int)countKernelY, 1);
                    
                    // Draw each occluder
                    cmd.SetRenderTarget(tempRT);
                    foreach (int occluderIdx in occluderIndices) {
                        cmd.DrawMesh(occluderMeshes[occluderIdx], occluderMatrices[occluderIdx], blackMat);
                    }
                    cmd.SetRenderTarget(nullRT);
                    
                    // Count unoccluded pixels again - this is the area of the light that is not occluded
                    cmd.SetComputeIntParam(countCS, "_Pass", 1);
                    cmd.DispatchCompute(countCS, countKernel, tempRT.width / (int)countKernelX, tempRT.height / (int)countKernelY, 1);
                    
                    // Compute the ratio of unoccluded pixels to total pixels
                    cmd.SetComputeIntParam(countCS, "_OcclusionIndex", probeIdx * 4 + channel);
                    cmd.DispatchCompute(countCS, ratioKernel, 1,1,1); // TODO: Do the ratio calculation at the end instead.
                    
                    // Report progress and flush the command buffer. We don't want to do this too often, as it will hurt bake performance.
                    // But doing flushing too rarely will cause CommandBuffer operations to become slow.
                    if (probesProcessed++ % 1024 == 0) {
                        float progress = (float)probesProcessed / totalProbesNeedingOcclusion;
                        string progressTitle = "Light Volume Occlusion Baking (1/2)";
                        string progressMessage = $"Dispatching probe bakes ({probesProcessed}/{totalProbesNeedingOcclusion})";
                        if (EditorUtility.DisplayCancelableProgressBar(progressTitle, progressMessage, progress)) {
                            EditorUtility.ClearProgressBar();
                            return occlusionBufferInit;
                        }
                        Graphics.ExecuteCommandBuffer(cmd);
                        cmd.Clear();
                    }
                }
            }
            
            // Read back the occlusion data
            EditorUtility.DisplayProgressBar("Light Volume Occlusion Baking (2/2)", "Waiting for GPU to finish...", -1);
            float[] occlusion = new float[occlusionBuffer.count];
            cmd.RequestAsyncReadback(occlusionBuffer, readback => {
                using NativeArray<float> occlusionReadback = readback.GetData<float>();
                occlusionReadback.CopyTo(occlusion);
            });
            cmd.WaitAllAsyncReadbackRequests();
            Graphics.ExecuteCommandBuffer(cmd);
            
            EditorUtility.ClearProgressBar();
            
            // Cleanup
            RenderTexture.ReleaseTemporary(tempRT);
            Object.DestroyImmediate(blackMat);
            Object.DestroyImmediate(whiteMat);

            stopwatch.Stop();
            Debug.Log("[LightVolumeOcclusionBaker] Occlusion baking took " + stopwatch.ElapsedMilliseconds + " ms for " + probePositions.Length + " probes.");
            
            return occlusion;
        }

        // Matches behavior of LV_ComputeAreaLightSquaredBoundingSphere() in LightVolumes.cginc
        private static float ComputeAreaLightBoundingRadius(float width, float height, Color color, float areaLightBrightnessCutoff) {
            float minSolidAngle = areaLightBrightnessCutoff / Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            float A = width * height;
            float w2 = width * width;
            float h2 = height * height;
            float B = 0.25f * (w2 + h2);
            float t = Mathf.Tan(0.25f * minSolidAngle);
            float T = t * t;
            float TB = T * B;
            float discriminant = Mathf.Sqrt(TB * TB + 4.0f * T * A * A);
            float d2 = (discriminant - TB) * 0.125f / T;
            return Mathf.Sqrt(d2);
        }
        
        // Compute shadowmask indices for each pixel light, in range [0; 3] (1 for each color channel).
        // Lights that don't use shadowmask will have -1 in the index.
        // Always returns 128 indices - the maximum number of pixel lights.
        public static sbyte[] ComputeShadowmaskIndices(IList<PointLightVolume> pixelLights, float areaLightBrightnessCutoff)
        {
            sbyte[] shadowmaskIndices = new sbyte[128];
            Array.Fill<sbyte>(shadowmaskIndices, -1);
            
            // Precompute bounding sphere radius of each shadow casting light
            float[] shadowLightInfluenceRadii = new float[pixelLights.Count];
            for (int lightIdx = 0; lightIdx < pixelLights.Count; lightIdx++)
            {
                // Don't care about non-shadow casting lights
                var light = pixelLights[lightIdx];
                if (light.Dynamic || !light.BakedShadows)
                    continue;
                
                float lightRadius = light.Range;
                if (light.Type == PointLightVolume.LightType.AreaLight)
                {
                    float width = Mathf.Max(Mathf.Abs(light.transform.lossyScale.x), 0.001f);
                    float height = Mathf.Max(Mathf.Abs(light.transform.lossyScale.y), 0.001f);
                    lightRadius = ComputeAreaLightBoundingRadius(width, height, light.Color, areaLightBrightnessCutoff);
                }
                shadowLightInfluenceRadii[lightIdx] = lightRadius;
            }
            
            // Pre-allocate some stuff outside the loop to avoid many allocations.
            List<int> overlaps = new List<int>();
            bool[] availableIndices = new bool[4];
            Array.Fill(availableIndices, true);
            
            // Compute shadowmask indices for each pixel light
            for (int lightIdx = 0; lightIdx < 128; lightIdx++)
            {
                // No light at this index
                if (lightIdx >= pixelLights.Count)
                    continue;
                
                // Skip non-shadow casting lights
                var light = pixelLights[lightIdx];
                if (light.Dynamic || !light.BakedShadows)
                    continue;
                
                // Get the bounding sphere
                Vector3 lightPosition = light.transform.position;
                float lightRadius = shadowLightInfluenceRadii[lightIdx];

                // Check each other shadow casting light for overlaps
                overlaps.Clear();
                for (int otherLightIdx = 0; otherLightIdx < pixelLights.Count; otherLightIdx++)
                {
                    // Skip self and non-shadow casting lights
                    var otherLight = pixelLights[otherLightIdx];
                    if (light == otherLight || otherLight.Dynamic || !otherLight.BakedShadows)
                        continue;
                    
                    // If the distance between the lights is less than the sum of their radii, they overlap
                    float distance = Vector3.Distance(lightPosition, otherLight.transform.position);
                    bool overlap = distance < (lightRadius + shadowLightInfluenceRadii[otherLightIdx]);
                    // TODO(pema99): For area lights, we can do better! If one light is behind an area light, there is no overlap

                    if (overlap)
                        overlaps.Add(otherLightIdx);
                }

                // For each overlapping light, remove its index
                Array.Fill(availableIndices, true);
                foreach (var otherLightIdx in overlaps)
                {
                    sbyte otherShadowmaskIndex = shadowmaskIndices[otherLightIdx];
                    
                    // No shadowmask index for this light
                    if (otherShadowmaskIndex < 0)
                        continue;
                    
                    availableIndices[otherShadowmaskIndex] = false;
                }
                
                // Pick the first available shadowmask index
                sbyte shadowmaskIndex = -1;
                for (sbyte bitIdx = 0; bitIdx < 4; bitIdx++)
                {
                    if (availableIndices[bitIdx])
                    {
                        shadowmaskIndex = bitIdx;
                        break;
                    }
                }
                shadowmaskIndices[lightIdx] = shadowmaskIndex;

                // Warn user if no shadowmask index is available
                if (shadowmaskIndex < 0)
                {
                    var go = light.gameObject;
                    Debug.LogWarning($"[LightVolumeOcclusionBaker] Failed to allocate a shadowmask for light '{go.name}'. There are too many other shadow casting lights nearby it!", go);
                }
            }

            return shadowmaskIndices;
        }

        // Computes a 3D texture with up to 4 occlusion values for each probe position.
        // Shadowmask indices must be computed for each light beforehand.
        public static Texture3D ComputeOcclusionTexture(Vector3Int resolution, Vector3[] probePositions, IList<PointLightVolume> pixelLights, float areaLightBrightnessCutoff)
        {
            // Precompute bounding sphere radius of each shadow casting light
            // TODO(pema99): Deduplicate this code, don't do it twice
            float[] shadowLightInfluenceRadii = new float[pixelLights.Count];
            float[] shadowLightRadii = new float[pixelLights.Count];
            for (int lightIdx = 0; lightIdx < pixelLights.Count; lightIdx++)
            {
                // Don't care about non-shadow casting lights
                var light = pixelLights[lightIdx];
                if (light.Dynamic || !light.BakedShadows)
                    continue;
                
                float lightInfluenceRadius = light.Range;
                float lightRadius = light.BakedShadowRadius;
                if (light.Type == PointLightVolume.LightType.AreaLight)
                {
                    float width = Mathf.Max(Mathf.Abs(light.transform.lossyScale.x), 0.001f);
                    float height = Mathf.Max(Mathf.Abs(light.transform.lossyScale.y), 0.001f);
                    lightInfluenceRadius = ComputeAreaLightBoundingRadius(width, height, light.Color, areaLightBrightnessCutoff);
                    lightRadius = Mathf.Sqrt(width * width + height * height);
                }
                shadowLightInfluenceRadii[lightIdx] = lightInfluenceRadius;
                shadowLightRadii[lightIdx] = lightRadius;
            }
            
            // For each probe, we need to find the lights that affect it. 4 entries per probe. -1 means no light affects this probe.
            int[] perProbeLights = new int[resolution.x * resolution.y * resolution.z * 4];
            Array.Fill(perProbeLights, -1);
            bool[] slotFilled = new bool[4];
            for (int probeIdx = 0; probeIdx < probePositions.Length; probeIdx++)
            {
                Array.Fill(slotFilled, false);
                for (int lightIdx = 0; lightIdx < pixelLights.Count; lightIdx++)
                {
                    // Don't care about lights with no shadowmask index
                    var light = pixelLights[lightIdx];
                    sbyte shadowmaskIndex = light.PointLightVolumeInstance.ShadowmaskIndex;
                    if (shadowmaskIndex < 0)
                        continue;

                    // Check if the probe is in range of the light
                    Vector3 lightPosition = light.transform.position;
                    float lightRadius = shadowLightInfluenceRadii[lightIdx];
                    if (Vector3.Distance(probePositions[probeIdx], lightPosition) > lightRadius)
                        continue;
                    
                    // Assign the light to the probe's shadowmask slot
                    perProbeLights[probeIdx * 4 + shadowmaskIndex] = lightIdx;
                    
                    // If we already filled all slots, we can stop
                    slotFilled[shadowmaskIndex] = true;
                    if (slotFilled[0] && slotFilled[1] && slotFilled[2] && slotFilled[3])
                        break;
                }
            }
            
            // Calculate occlusion factors for each probe position and populate the texture
            float[] occlusionFactors = ComputeOcclusionFactors(probePositions, perProbeLights, pixelLights, shadowLightRadii, 256);
            Color[] occlusionColors = new Color[resolution.x * resolution.y * resolution.z];
            for (int texelIdx = 0; texelIdx < occlusionColors.Length; texelIdx++)
            {
                occlusionColors[texelIdx] = new Color(
                    occlusionFactors[texelIdx * 4 + 0],
                    occlusionFactors[texelIdx * 4 + 1],
                    occlusionFactors[texelIdx * 4 + 2],
                    occlusionFactors[texelIdx * 4 + 3]);
            }

            TextureFormat format = TextureFormat.RGBAHalf;
            Texture3D tex = new Texture3D(resolution.x, resolution.y, resolution.z, format, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            tex.SetPixels(occlusionColors);
            tex.Apply(updateMipmaps: false);
            
            return tex;
        }
    }
}
#endif