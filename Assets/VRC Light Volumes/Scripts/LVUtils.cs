using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.SceneManagement;

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
            Debug.LogError($"[LightVolumeUtils] Failed to SetPixels in the final Texture3D atlas. Error: {ex.Message}");
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
            Debug.Log($"[LightVolumeUtils] 3D Atlas saved at path: '{assetPath}'");
            return true;
        } catch (System.Exception e) {
            Debug.LogError($"[LightVolumeUtils] Error saving 3D Atlas at path: '{assetPath}': {e.Message}");
            return false;
        }
#else
        Debug.LogError($"[LightVolumeUtils] You can only save 3D textures in editor!");
        return false;
#endif
    }

}
