using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.Collections.Generic;

#if UNITY_EDITOR
using System.IO;
#endif

[ExecuteAlways]
public class AtlasCreatorExample : MonoBehaviour {

    public BakeryVolume[] BakeryVolumes;
    public float[] BakeryVolumesWeights;

    public int stochasticIterations = 5000;

    [SerializeField] private Texture3D generatedAtlas;
    [SerializeField] private Vector3[] boundsMin;
    [SerializeField] private Vector3[] boundsMax;
    [SerializeField] private Texture3DAtlasGenerator.Atlas3D Atlas;
    [SerializeField] private Vector3[] boundsWMin;
    [SerializeField] private Vector3[] boundsWMax;
    [SerializeField] private bool IsEnabled;
#if UNITY_EDITOR
    [ContextMenu("Generate 3D Atlas")]
    private void GenerateAtlas() {

        Texture3D[] Textures = new Texture3D[BakeryVolumes.Length * 3];

        for (int i = 0; i < BakeryVolumes.Length; i ++) {
            if(BakeryVolumes[i].bakedTexture0 == null || BakeryVolumes[i].bakedTexture1 == null || BakeryVolumes[i].bakedTexture2 == null) {
                Debug.LogError("One of the bakery volumes is not baked!");
                return;
            }
            Textures[i * 3]     = BakeryVolumes[i].bakedTexture0;
            Textures[i * 3 + 1] = BakeryVolumes[i].bakedTexture1;
            Textures[i * 3 + 2] = BakeryVolumes[i].bakedTexture2;
        }

        Atlas = Texture3DAtlasGenerator.CreateAtlasStochastic(Textures, stochasticIterations);
        generatedAtlas = Atlas.Texture;

        boundsMin = Atlas.BoundsUvwMin;
        boundsMax = Atlas.BoundsUvwMax;

        boundsWMin = new Vector3[BakeryVolumes.Length];
        boundsWMax = new Vector3[BakeryVolumes.Length];

        for (int i = 0; i < BakeryVolumes.Length; i++) {
            boundsWMin[i] = BakeryVolumes[i].bounds.min;
            boundsWMax[i] = BakeryVolumes[i].bounds.max;
        }

        SaveTexture3DAsAsset(generatedAtlas, "Assets/BakeryLightmaps/Atlas3D.asset");


    }
#endif

    [ContextMenu("SetSaderVars")]
    private void SetVars() {
        SetShaderVariables(BakeryVolumes);
    }

    private void Update() {
        SetShaderVariables(BakeryVolumes);
    }

    private void SetShaderVariables(BakeryVolume[] volumes) {

        Shader.SetKeyword(GlobalKeyword.Create("LightVolumesEnabled"), true);

        float[] LightVolumeWeight = new float[256];
        Vector4[] LightVolumeWorldMin = new Vector4[256];
        Vector4[] LightVolumeWorldMax = new Vector4[256];
        Vector4[] LightVolumeUvwMin = new Vector4[768];
        Vector4[] LightVolumeUvwMax = new Vector4[768];

        for (int i = 0; i < volumes.Length; i++) {

            // Weight
            LightVolumeWeight[i] = BakeryVolumesWeights.Length > 0 ? BakeryVolumesWeights[Mathf.Clamp(i, 0, BakeryVolumesWeights.Length)] : 0;
            
            // World bounds
            LightVolumeWorldMin[i] = boundsWMin[i];
            LightVolumeWorldMax[i] = boundsWMax[i];

            // UVW bounds
            LightVolumeUvwMin[i * 3] = boundsMin[i * 3];
            LightVolumeUvwMax[i * 3] = boundsMax[i * 3];
            LightVolumeUvwMin[i * 3 + 1] = boundsMin[i * 3 + 1];
            LightVolumeUvwMax[i * 3 + 1] = boundsMax[i * 3 + 1];
            LightVolumeUvwMin[i * 3 + 2] = boundsMin[i * 3 + 2];
            LightVolumeUvwMax[i * 3 + 2] = boundsMax[i * 3 + 2];

        }

        Vector3 size = new Vector3(generatedAtlas.width, generatedAtlas.height, generatedAtlas.depth);
        Shader.SetGlobalVector("_UdonLightVolumeTexelSize", new Vector3(1f / size.x, 1f / size.y, 1f / size.z));


        Shader.SetGlobalFloat("_UdonLightVolumeCount", volumes.Length);
        Shader.SetGlobalTexture("_UdonLightVolume", generatedAtlas);

        Shader.SetGlobalFloatArray("_UdonLightVolumeWeight", LightVolumeWeight);
        Shader.SetGlobalVectorArray("_UdonLightVolumeWorldMin", LightVolumeWorldMin);
        Shader.SetGlobalVectorArray("_UdonLightVolumeWorldMax", LightVolumeWorldMax);

        Shader.SetGlobalVectorArray("_UdonLightVolumeUvwMin", LightVolumeUvwMin);
        Shader.SetGlobalVectorArray("_UdonLightVolumeUvwMax", LightVolumeUvwMax);

        Shader.SetGlobalFloat(Shader.PropertyToID("_UdonLightVolumeEnabled"), IsEnabled ? 1 : 0);

    }
#if UNITY_EDITOR
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
#endif

}