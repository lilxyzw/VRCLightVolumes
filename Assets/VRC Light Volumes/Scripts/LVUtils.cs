using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
using System.IO;
using UnityEditor.SceneManagement;
#endif

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


}
