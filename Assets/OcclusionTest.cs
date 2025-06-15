#if !COMPILER_UDONSHARP
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using VRCLightVolumes;

[ExecuteAlways]
public class OcclusionTest : MonoBehaviour
{
    public Transform target;
    
    public bool run = false;

    public void Update()
    {
        if (run)
        {
            run = false;
            
            //float occ = CalculateOcclusionFactors(new [] {transform.position}, target.position, target.lossyScale.x/2)[0];
            var ps = GameObject.Find("Light Volume Manager").GetComponent<LightVolumeSetup>().PointLightVolumes;
            var occ = LightVolumeOcclusionBaker.ComputeOcclusionTexture(Vector3Int.one, Vector3.one / 3.0f,
                new Vector3[] { target.position }, ps, 0);
            Debug.Log("Occlusion factor for target: " + occ.GetPixel(0,0,0).r);

            // var probePositions = LightmapSettings.lightProbes.positions;
            // var sh = LightmapSettings.lightProbes.bakedProbes;
            //
            // var sw = System.Diagnostics.Stopwatch.StartNew();
            // float[] occlusion = CalculateOcclusionFactors(probePositions, target.position, target.lossyScale.x/2);
            // sw.Stop();
            // Debug.Log($"Occlusion calculation took {sw.ElapsedMilliseconds} ms for {probePositions.Length} probes.");
            //
            // for (int i = 0; i < probePositions.Length; i++)
            // {
            //     var pos = probePositions[i];
            //     var newSh = new SphericalHarmonicsL2();
            //     newSh.AddAmbientLight(Color.white * occlusion[i]);
            //     sh[i] = newSh;
            // }
            // LightmapSettings.lightProbes.bakedProbes = sh;
        }
    }
 
    float[] CalculateOcclusionFactors(Vector3[] probePositions, Vector3 lightPosition, float lightRadius)
    {
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
        RenderTexture tempRT = RenderTexture.GetTemporary(256, 256, 16, RenderTextureFormat.R8);
        var nullRT = new RenderTargetIdentifier();
        Debug.Assert(tempRT.width % countKernelX == 0 && tempRT.height % countKernelY == 0);
        using GraphicsBuffer occlusionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, probePositions.Length, sizeof(float));
        occlusionBuffer.SetData(new float[probePositions.Length]);
        using GraphicsBuffer countBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 2, sizeof(uint));
        countBuffer.SetData(new uint[2]);

        // Find all GI contributors - these are the occluders
        MeshRenderer[] occluders = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(mr => GameObjectUtility.AreStaticEditorFlagsSet(mr.gameObject, StaticEditorFlags.ContributeGI))
            .ToArray();
        Mesh[] occluderMeshes = occluders.Select(mr => mr.GetComponent<MeshFilter>().sharedMesh).ToArray();
        Matrix4x4[] occluderMatrices = occluders.Select(mr => Matrix4x4.TRS(mr.transform.position, mr.transform.rotation, mr.transform.lossyScale)).ToArray();
        
        // Set up command buffer with uniforms that don't change per probe
        using CommandBuffer cmd = new CommandBuffer();
        cmd.name = "Light Volume Occlusion Baking";
        
        cmd.SetComputeBufferParam(countCS, countKernel, "_Count", countBuffer);
        cmd.SetComputeTextureParam(countCS, countKernel, "_Texture", tempRT);
        cmd.SetComputeIntParam(countCS, "_TextureWidth", tempRT.width);
        cmd.SetComputeIntParam(countCS, "_TextureHeight", tempRT.height);
        
        cmd.SetComputeBufferParam(countCS, ratioKernel, "_Count", countBuffer);
        cmd.SetComputeBufferParam(countCS, ratioKernel, "_Occlusion", occlusionBuffer);
        
        // Rasterize the scene from each probes perspective
        for (int probeIdx = 0; probeIdx < probePositions.Length; probeIdx++)
        {
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
            cmd.SetViewProjectionMatrices(worldToProbe, probeToClip);
            
            // Draw the light mesh first
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
            for (int i = 0; i < occluders.Length; i++)
            {
                cmd.DrawMesh(occluderMeshes[i], occluderMatrices[i], blackMat);
            }
            cmd.SetRenderTarget(nullRT);
            
            // Count unoccluded pixels again - this is the area of the light that is not occluded
            cmd.SetComputeIntParam(countCS, "_Pass", 1);
            cmd.DispatchCompute(countCS, countKernel, tempRT.width / (int)countKernelX, tempRT.height / (int)countKernelY, 1);
            
            // Compute the ratio of unoccluded pixels to total pixels
            cmd.SetComputeIntParam(countCS, "_OcclusionIndex", probeIdx);
            cmd.DispatchCompute(countCS, ratioKernel, 1,1,1); // TODO: Do the ratio calculation at the end instead.
        }
        
        // Read back the occlusion data
        float[] occlusion = new float[probePositions.Length];
        cmd.RequestAsyncReadback(occlusionBuffer, readback =>
        {
            using NativeArray<float> occlusionReadback = readback.GetData<float>();
            occlusionReadback.CopyTo(occlusion);
        });
        cmd.WaitAllAsyncReadbackRequests();
        Graphics.ExecuteCommandBuffer(cmd);
        
        // Cleanup
        RenderTexture.ReleaseTemporary(tempRT);
        Object.DestroyImmediate(blackMat);
        Object.DestroyImmediate(whiteMat);

        return occlusion;
    }
}
#endif