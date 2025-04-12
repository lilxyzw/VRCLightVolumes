using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
#if UNITY_EDITOR
using System.IO;
using UnityEditor.SceneManagement;
#endif

public class LightVolumeSetup : MonoBehaviour {

    public BakeryVolume[] BakeryVolumes;
    public float[] BakeryVolumesWeights;

    public int StochasticIterations = 5000;
    public Texture3D LightVolumeAtlas;
    [SerializeField] public List<LightVolumeData> LightVolumeDataList = new List<LightVolumeData>();

    private LightVolumeManager _udonLightVolumeManager;

    public void SetShaderVariables() {
        if (_udonLightVolumeManager == null) _udonLightVolumeManager = GetComponent<LightVolumeManager>();
        if (_udonLightVolumeManager == null) return;
            _udonLightVolumeManager.SetShaderVariables();
    }

#if UNITY_EDITOR

    // Generates atlas and setups udon script
    public void GenerateAtlas() {

        if (BakeryVolumes.Length == 0) return;

        Texture3D[] textures = new Texture3D[BakeryVolumes.Length * 3];

        for (int i = 0; i < BakeryVolumes.Length; i++) {
            if (BakeryVolumes[i] == null) {
                Debug.LogError("[LightVolumeAtlaser] One of the bakery volumes is not setuped!");
                return;
            }
            if (BakeryVolumes[i].bakedTexture0 == null || BakeryVolumes[i].bakedTexture1 == null || BakeryVolumes[i].bakedTexture2 == null) {
                Debug.LogError("[LightVolumeAtlaser] One of the bakery volumes is not baked!");
                return;
            }
            textures[i * 3] = BakeryVolumes[i].bakedTexture0;
            textures[i * 3 + 1] = BakeryVolumes[i].bakedTexture1;
            textures[i * 3 + 2] = BakeryVolumes[i].bakedTexture2;
        }

        var atlas = Texture3DAtlasGenerator.CreateAtlasStochastic(textures, StochasticIterations);

        LightVolumeAtlas = atlas.Texture;

        LightVolumeDataList.Clear();

        for (int i = 0; i < BakeryVolumes.Length; i++) {
            int i3 = i * 3;
            LightVolumeDataList.Add(new LightVolumeData(
                i < BakeryVolumesWeights.Length ? BakeryVolumesWeights[i] : 0,
                BakeryVolumes[i].bounds.min,
                BakeryVolumes[i].bounds.max,
                atlas.BoundsUvwMin[i3],
                atlas.BoundsUvwMin[i3 + 1],
                atlas.BoundsUvwMin[i3 + 2],
                atlas.BoundsUvwMax[i3],
                atlas.BoundsUvwMax[i3 + 1],
                atlas.BoundsUvwMax[i3 + 2]
            ));
        }

        SaveTexture3DAsAsset(atlas.Texture, "Assets/BakeryLightmaps/Atlas3D.asset");

        SetupUdonBehaviour();

    }

    // Setups udon script
    [ContextMenu("Setup Udon Behaviour")]
    public void SetupUdonBehaviour() {

        if (IsInPrefabAsset(this)) return;
        if (_udonLightVolumeManager == null) _udonLightVolumeManager = GetComponent<LightVolumeManager>();
        if (_udonLightVolumeManager == null) return;

        // Update Weights because can be desynced
        for (int i = 0; i < BakeryVolumesWeights.Length; i++) {
            LightVolumeDataList[i] = new LightVolumeData(
                i < BakeryVolumesWeights.Length ? BakeryVolumesWeights[i] : 0,
                LightVolumeDataList[i].WorldMin,
                LightVolumeDataList[i].WorldMax,
                LightVolumeDataList[i].UvwMin[0],
                LightVolumeDataList[i].UvwMin[1],
                LightVolumeDataList[i].UvwMin[2],
                LightVolumeDataList[i].UvwMax[0],
                LightVolumeDataList[i].UvwMax[1],
                LightVolumeDataList[i].UvwMax[2]
            );
        }

        Vector4[] boundsWorldMin;
        Vector4[] boundsWorldMax;
        Vector4[] boundsUvwMin;
        Vector4[] boundsUvwMax;

        var sortedData = LightVolumeDataSorter.SortData(LightVolumeDataList);
        LightVolumeDataSorter.GetData(sortedData, out boundsWorldMin, out boundsWorldMax, out boundsUvwMin, out boundsUvwMax);

        _udonLightVolumeManager.BoundsUvwMin = boundsUvwMin;
        _udonLightVolumeManager.BoundsUvwMax = boundsUvwMax;
        _udonLightVolumeManager.BoundsWorldMin = boundsWorldMin;
        _udonLightVolumeManager.BoundsWorldMax = boundsWorldMax;
        _udonLightVolumeManager.LightVolume = LightVolumeAtlas;

        SetShaderVariables();

    }


    public static bool SaveTexture3DAsAsset(Texture3D textureToSave, string assetPath) {

        if (textureToSave == null) {
            Debug.LogError("[LightVolumeAtlaser] Error saving Texture3D: texture is null");
            return false;
        }

        if (string.IsNullOrEmpty(assetPath)) {
            Debug.LogError("[LightVolumeAtlaser] Error saving Texture3D: Saving path is null");
            return false;
        }

        try {
            string directoryPath = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath)) {
                Directory.CreateDirectory(directoryPath);
                AssetDatabase.Refresh();
            }
        } catch (System.Exception e) {
            Debug.LogError($"[LightVolumeAtlaser] Error while creating folders '{assetPath}': {e.Message}");
            return false;
        }

        try {
            AssetDatabase.CreateAsset(textureToSave, assetPath);
            EditorUtility.SetDirty(textureToSave);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[LightVolumeAtlaser] 3D Atlas saved at path: '{assetPath}'");
            return true;
        } catch (System.Exception e) {
            Debug.LogError($"[LightVolumeAtlaser] Error saving 3D Atlas at path: '{assetPath}': {e.Message}");
            return false;
        }

    }


    // Check if it's previewed as a prefab, or it's a part of a scene
    bool IsInPrefabAsset(Object obj) {
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabType = PrefabUtility.GetPrefabAssetType(obj);
        var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(obj);
        return prefabStatus == PrefabInstanceStatus.NotAPrefab && prefabType != PrefabAssetType.NotAPrefab && prefabStage == null;
    }
#endif

}