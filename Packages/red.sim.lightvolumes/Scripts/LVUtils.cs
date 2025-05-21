using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using System.IO;
using UnityEditor.SceneManagement;
#endif

namespace VRCLightVolumes {
    public class LVUtils {

        // Transforms point with specified Position, Rotation and Scale
        public static Vector3 TransformPoint(Vector3 point, Vector3 position, Quaternion rotation, Vector3 scale) {
            return rotation * Vector3.Scale(point, scale) + position;
        }

        // Setting lossy scale to a specified transform
        public static void SetLossyScale(Transform transform, Vector3 targetLossyScale, int maxIterations = 20) {
            Vector3 guess = transform.localScale;
            for (int i = 0; i < maxIterations; i++) {
                transform.localScale = guess;
                Vector3 currentLossy = transform.lossyScale;
                Vector3 ratio = new Vector3(
                    currentLossy.x != 0 ? targetLossyScale.x / currentLossy.x : 1f,
                    currentLossy.y != 0 ? targetLossyScale.y / currentLossy.y : 1f,
                    currentLossy.z != 0 ? targetLossyScale.z / currentLossy.z : 1f
                );
                guess = new Vector3(guess.x * ratio.x, guess.y * ratio.y, guess.z * ratio.z);
            }
        }

        // Plane vertices for drawing a square
        public static Vector3[] GetPlaneVertices(Vector3 center, Quaternion rotation, float size) {
            Vector3 right = rotation * Vector3.right * size;
            Vector3 up = rotation * Vector3.up * size;
            return new Vector3[] { center - right - up, center - right + up, center + right + up, center + right - up };
        }

        // Check if it's previewed as a prefab, or it's a part of a scene
        public static bool IsInPrefabAsset(Object obj) {
#if UNITY_EDITOR
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabType = PrefabUtility.GetPrefabAssetType(obj);
        var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(obj);
        return prefabStatus == PrefabInstanceStatus.NotAPrefab && prefabType != PrefabAssetType.NotAPrefab && prefabStage == null;
#else
            return false;
#endif
        }

        public static void MarkDirty(Object obj) {
#if UNITY_EDITOR
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorUtility.SetDirty(obj);
            if (PrefabUtility.IsPartOfPrefabInstance(obj))
                PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
#endif
        }

        // Apply voxels to a 3D Texture
        public static bool Apply3DTextureData(Texture3D texture, Color[] colors) {
            try {
                texture.SetPixels(colors);
                texture.Apply(updateMipmaps: false);
                return true;
            } catch (UnityException ex) {
                Debug.LogError($"[LightVolumeUtils] Failed to SetPixels in the Texture3D. Error: {ex.Message}");
                return false;
            }
        }

        // Remaps value
        public static float Remap(float value, float MinOld, float MaxOld, float MinNew, float MaxNew) {
            return MinNew + (value - MinOld) * (MaxNew - MinNew) / (MaxOld - MinOld);
        }

        // Remaps value to 01 range
        public static float RemapTo01(float value, float MinOld, float MaxOld) {
            return (value - MinOld) / (MaxOld - MinOld);
        }

        // Saves 3D Texture to Assets
        public static bool SaveTexture3DAsAsset(Texture3D textureToSave, string assetPath) {
#if UNITY_EDITOR
        if (textureToSave == null) {
            Debug.LogError("[LightVolumeUtils] Error saving Texture3D: texture is null");
            return false;
        }

        if (string.IsNullOrEmpty(assetPath)) {
            Debug.LogError("[LightVolumeUtils] Error saving Texture3D: Saving path is null");
            return false;
        }

        try {
            string directoryPath = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath)) {
                Directory.CreateDirectory(directoryPath);
                AssetDatabase.Refresh();
            }
        } catch (System.Exception e) {
            Debug.LogError($"[LightVolumeUtils] Error while creating folders '{assetPath}': {e.Message}");
            return false;
        }

        try {
            AssetDatabase.CreateAsset(textureToSave, assetPath);
            EditorUtility.SetDirty(textureToSave);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[LightVolumeUtils] Texture3D saved at path: '{assetPath}'");
            return true;
        } catch (System.Exception e) {
            Debug.LogError($"[LightVolumeUtils] Error saving Texture3D at path: '{assetPath}': {e.Message}");
            return false;
        }
#else
            Debug.LogError($"[LightVolumeUtils] You can only save Texture3D in editor!");
            return false;
#endif
        }

        // Simple 3D denoiser
        public static Vector3[] BilateralDenoise3D(Vector3[] input, int w, int h, int d, float sigmaSpatial = 1f, float sigmaRange = 0.1f) {
            Vector3[] output = new Vector3[input.Length];
            int r = Mathf.CeilToInt(2f * sigmaSpatial);

            for (int z = 0; z < d; z++)
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++) {
                        int centerIdx = x + y * w + z * w * h;
                        Vector3 center = input[centerIdx];
                        Vector3 sum = Vector3.zero;
                        float weightSum = 0f;

                        for (int dz = -r; dz <= r; dz++)
                            for (int dy = -r; dy <= r; dy++)
                                for (int dx = -r; dx <= r; dx++) {
                                    int xx = x + dx;
                                    int yy = y + dy;
                                    int zz = z + dz;
                                    if (xx < 0 || yy < 0 || zz < 0 || xx >= w || yy >= h || zz >= d) continue;

                                    int nIdx = xx + yy * w + zz * w * h;
                                    Vector3 neighbor = input[nIdx];

                                    float spatialDist2 = dx * dx + dy * dy + dz * dz;
                                    float rangeDist2 = (neighbor - center).sqrMagnitude;

                                    float spatialWeight = Mathf.Exp(-spatialDist2 / (2f * sigmaSpatial * sigmaSpatial));
                                    float rangeWeight = Mathf.Exp(-rangeDist2 / (2f * sigmaRange * sigmaRange));

                                    float weight = spatialWeight * rangeWeight;
                                    sum += neighbor * weight;
                                    weightSum += weight;
                                }

                        output[centerIdx] = weightSum > 0f ? sum / weightSum : center;
                    }

            return output;
        }

        // Bounds of a transformed 1x1x1 cube
        public static Bounds BoundsFromTRS(Matrix4x4 trs) {
            Vector3 center = trs.GetColumn(3);
            Vector3 a = trs.GetColumn(0) * 0.5f;
            Vector3 b = trs.GetColumn(1) * 0.5f;
            Vector3 c = trs.GetColumn(2) * 0.5f;
            Vector3 extents = new Vector3(
                Mathf.Abs(a.x) + Mathf.Abs(b.x) + Mathf.Abs(c.x),
                Mathf.Abs(a.y) + Mathf.Abs(b.y) + Mathf.Abs(c.y),
                Mathf.Abs(a.z) + Mathf.Abs(b.z) + Mathf.Abs(c.z)
            );
            return new Bounds(center, extents * 2f);
        }

        // Generates an IcoSphere mesh
        public static Mesh GenerateIcoSphere(float radius = 0.5f, int subdivisions = 2) {

            const float t = 0.850650808f;   // ≈ φ/√5
            const float s = 0.525731112f;   // ≈ 1/(2φ)

            Vector3[] baseVerts = {
            new Vector3(-s,  t,  0), new Vector3( s,  t,  0), new Vector3(-s, -t,  0), new Vector3( s, -t,  0),
            new Vector3( 0, -s,  t), new Vector3( 0,  s,  t), new Vector3( 0, -s, -t), new Vector3( 0,  s, -t),
            new Vector3( t,  0, -s), new Vector3( t,  0,  s), new Vector3(-t,  0, -s), new Vector3(-t,  0,  s)
        };

            int[] baseTris = {
             0,11, 5, 0, 5, 1, 0, 1, 7, 0, 7,10, 0,10,11,
             1, 5, 9, 5,11, 4,11,10, 2,10, 7, 6, 7, 1, 8,
             3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
             4, 9, 5, 2, 4,11, 6, 2,10, 8, 6, 7, 9, 8, 1
        };

            var verts = new List<Vector3>(baseVerts);
            var tris = new List<int>(baseTris);
            var cache = new Dictionary<long, int>();

            subdivisions = Mathf.Clamp(subdivisions, 0, 8);   // 20·4⁸ ≈ 327 k tri max

            for (int level = 0; level < subdivisions; ++level) {
                cache.Clear();
                var newTris = new List<int>(tris.Count * 4);

                for (int i = 0; i < tris.Count; i += 3) {
                    int i0 = tris[i], i1 = tris[i + 1], i2 = tris[i + 2];

                    int a = Midpoint(i0, i1);
                    int b = Midpoint(i1, i2);
                    int c = Midpoint(i2, i0);

                    newTris.AddRange(new[] { i0, a, c, i1, b, a, i2, c, b, a, b, c });
                }
                tris = newTris;
            }

            for (int i = 0; i < verts.Count; ++i)
                verts[i] = verts[i].normalized * radius;

            var mesh = new Mesh { name = $"IcoSphere_{subdivisions}" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;

            int Midpoint(int ia, int ib) {
                long key = ia < ib ? ((long)ia << 32) | (uint)ib : ((long)ib << 32) | (uint)ia;
                if (cache.TryGetValue(key, out int idx)) return idx;
                Vector3 mid = (verts[ia] + verts[ib]).normalized;
                idx = verts.Count;
                cache[key] = idx;
                verts.Add(mid);
                return idx;
            }
        }

        // Fixes bakery L1 probe channel
        public static Vector3 LinearizeSingleSH(float L0, Vector3 L1) {
            L1 = L1 / 2;
            float L1length = L1.magnitude;
            if (L1length > 0.0 && L0 > 0.0) {
                L1 *= Mathf.Min(L0 / L1length, 1.13f);
            }
            return L1;
        }

        // Fizes bakery L1 probe
        public static SphericalHarmonicsL2 LinearizeSH(SphericalHarmonicsL2 sh) {

            const int r = 0;
            const int g = 1;
            const int b = 2;
            const int a = 0;
            const int x = 3;
            const int y = 1;
            const int z = 2;

            Vector3 L0 = new Vector3(sh[r, a], sh[g, a], sh[b, a]);
            Vector3 L1r = new Vector3(sh[r, x], sh[r, y], sh[r, z]);
            Vector3 L1g = new Vector3(sh[g, x], sh[g, y], sh[g, z]);
            Vector3 L1b = new Vector3(sh[b, x], sh[b, y], sh[b, z]);

            L1r = LinearizeSingleSH(L0.x, L1r);
            L1g = LinearizeSingleSH(L0.y, L1g);
            L1b = LinearizeSingleSH(L0.z, L1b);

            sh[r, x] = L1r.x;
            sh[r, y] = L1r.y;
            sh[r, z] = L1r.z;

            sh[g, x] = L1g.x;
            sh[g, y] = L1g.y;
            sh[g, z] = L1g.z;

            sh[b, x] = L1b.x;
            sh[b, y] = L1b.y;
            sh[b, z] = L1b.z;

            return sh;
        }

    }
}