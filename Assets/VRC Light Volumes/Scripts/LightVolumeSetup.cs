using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

public class LightVolumeSetup : SingletonEditor<LightVolumeSetup> {

    public LightVolume[] LightVolumes = new LightVolume[0];
    public float[] LightVolumesWeights = new float[0];
    [Header("Baking")]
    [Tooltip("Bakery usually gives better results and works faster.")]
#if BAKERY_INCLUDED
    public Baking BakingMode = Baking.Bakery;
#else
    public Baking BakingMode = Baking.UnityLightmapper;
#endif
    [Tooltip("Removes baked noise in Light Volumes but may slightly reduce sharpness. Recommended to keep it enabled.")]
    public bool Denoise = true;
    [Tooltip("Automatically fixes Bakery's \"burned\" light probes after a scene bake. But decreases their contrast slightly.")] 
    public bool FixLightProbesL1 = true;
    [Header("Visuals")]
    [Tooltip("Size in meters of the overlapping regions between Light Volumes for smooth blending.")]
    [Range(0, 1)] public float EdgeSmoothing = 0.25f;
    [Tooltip("When enabled, areas outside Light Volumes fall back to light probes. Otherwise, the Light Volume with the smallest weight is used as fallback. It also improves performance.")]
    public bool LightProbesBlending = true;
    [Tooltip("Disables smooth blending with areas outside Light Volumes. Use it if your entire scene's play area is covered by Light Volumes. It also improves performance.")]
    public bool SharpBounds = true;
    [Tooltip("Automatically updates a volume's position, rotation, and scale in Play mode using an Udon script. Use only if you have movable volumes in your scene.")]
    public bool AutoUpdateVolumes = false;

    public Texture3D LightVolumeAtlas;
    [SerializeField] public List<LightVolumeData> LightVolumeDataList = new List<LightVolumeData>();

    public bool IsBakeryMode => BakingMode == Baking.Bakery; // Just a shortcut

    private LightVolumeManager _udonLightVolumeManager;
    private Baking _bakingModePrev;

    // Sets shader variables tthrough Udon Component
    public void UpdateVolumes() {
        if (_udonLightVolumeManager == null) _udonLightVolumeManager = GetComponent<LightVolumeManager>();
        if (_udonLightVolumeManager == null) return;
            _udonLightVolumeManager.UpdateVolumes();
    }

    protected override void OnInstanceCreated() {
        if(!TryGetComponent(out _udonLightVolumeManager)) {
            _udonLightVolumeManager = gameObject.AddComponent<LightVolumeManager>();
        }
    }

#if UNITY_EDITOR

#if BAKERY_INCLUDED
    private bool _subscribedToBakery = false;
#endif
    private bool _subscribedToUnityLightmapper = false;

    // Subscribing to OnBaked events
    private void OnEnable() {
#if BAKERY_INCLUDED
        if (!Application.isPlaying && !_subscribedToBakery) {
            ftRenderLightmap.OnFinishedFullRender += OnBakeryFinishedRender;
            _subscribedToBakery = true;
        }
#endif
        if (!Application.isPlaying && !_subscribedToUnityLightmapper) {
            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted += OnAdditionalProbesCompleted;
            Lightmapping.bakeStarted += OnUnityBakingStarted;
            _subscribedToUnityLightmapper = true;
        }
    }
    
    // Unsubscribing fron OnBaked events
    private void OnDisable() {
#if BAKERY_INCLUDED
        if (!Application.isPlaying && _subscribedToBakery) {
            ftRenderLightmap.OnFinishedFullRender -= OnBakeryFinishedRender;
            _subscribedToBakery = false;
        }
#endif
        if (!Application.isPlaying && _subscribedToUnityLightmapper) {
            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted -= OnAdditionalProbesCompleted;
            Lightmapping.bakeStarted -= OnUnityBakingStarted;
            _subscribedToUnityLightmapper = false;
            
        }
    }

    // On Bakery baked
#if BAKERY_INCLUDED
    private void OnBakeryFinishedRender(object sender, EventArgs e) {
        if (BakingMode != Baking.Bakery) return;
        LightVolume[] volumes = FindObjectsByType<LightVolume>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < volumes.Length; i++) {
            if (volumes[i].Bake) {
                volumes[i].BakedRotation = volumes[i].GetRotation();
            }
        }
        if (FixLightProbesL1) FixLightProbes();
    }
#endif

    // On Unity Lightmapper baked
    private void OnAdditionalProbesCompleted() {
        if (BakingMode != Baking.UnityLightmapper) return;
        for (int i = 0; i < LightVolumes.Length; i++) {
            if (LightVolumes[i].Bake) {
                LightVolumes[i].Save3DTextures(i);
                LightVolumes[i].RemoveAdditionalProbes(i);
                LightVolumes[i].BakedRotation = LightVolumes[i].GetRotation();
            }
        }
        Debug.Log($"[LightVolumeSetup] Additional probes baking finished! Generating 3D Atlas...");
        GenerateAtlas();
        Debug.Log($"[LightVolumeSetup] Generating 3D Atlas finished!");
    }

    // On Unity Lightmapper started baking
    private void OnUnityBakingStarted() {
        if (BakingMode != Baking.UnityLightmapper) return;
        for (int i = 0; i < LightVolumes.Length; i++) {
            if (LightVolumes[i].Bake) {
                Debug.Log($"[LightVolumeSetup] Adding additional probes to bake with Light Volume \"{LightVolumes[i].gameObject.name}\" using Unity Lightmapper. Group {i}");
                LightVolumes[i].SetAdditionalProbes(i);
            }
        }
    }

    private void Update() {
        // Resetup required game objects and components for light volumes in new baking mode
        if(_bakingModePrev != BakingMode) {
            _bakingModePrev = BakingMode;
            var volumes = FindObjectsOfType<LightVolume>();
            for (int i = 0; i < volumes.Length; i++) {
                volumes[i].SetupDependencies();
            }
        }
    }

    // Generates atlas and setups udon script
    public void GenerateAtlas() {

        if (LightVolumes.Length == 0) return;

        Texture3D[] textures = new Texture3D[LightVolumes.Length * 3];

        for (int i = 0; i < LightVolumes.Length; i++) {
            if (LightVolumes[i] == null) {
                Debug.LogError("[LightVolumeSetup] One of the light volumes is not setuped!");
                return;
            }
            if (LightVolumes[i].Texture0 == null || LightVolumes[i].Texture1 == null || LightVolumes[i].Texture2 == null) {
                Debug.LogError("[LightVolumeSetup] One of the light volumes is not baked!");
                return;
            }
            textures[i * 3] = LightVolumes[i].Texture0;
            textures[i * 3 + 1] = LightVolumes[i].Texture1;
            textures[i * 3 + 2] = LightVolumes[i].Texture2;
        }

        var atlas = Texture3DAtlasGenerator.CreateAtlas(textures);

        LightVolumeAtlas = atlas.Texture;

        LightVolumeDataList.Clear();

        for (int i = 0; i < LightVolumes.Length; i++) {

            int i3 = i * 3;

            LightVolumeDataList.Add(new LightVolumeData(
                i < LightVolumesWeights.Length ? LightVolumesWeights[i] : 0,
                Vector4.zero,
                Matrix4x4.identity,
                atlas.BoundsUvwMin[i3],
                atlas.BoundsUvwMin[i3 + 1],
                atlas.BoundsUvwMin[i3 + 2],
                atlas.BoundsUvwMax[i3],
                atlas.BoundsUvwMax[i3 + 1],
                atlas.BoundsUvwMax[i3 + 2],
                Quaternion.identity,
                null,
                false
            ));

        }

        LVUtils.SaveTexture3DAsAsset(atlas.Texture, $"Assets/VRC Light Volumes/Textures3D/{SceneManager.GetActiveScene().name}_LightVolumeAtlas.asset");
        SetupUdonBehaviour();

    }

    // Setups udon script
    public void SetupUdonBehaviour() {

        if (LVUtils.IsInPrefabAsset(this)) return;
        if (_udonLightVolumeManager == null) _udonLightVolumeManager = GetComponent<LightVolumeManager>();
        if (_udonLightVolumeManager == null) return;

        if(LightVolumesWeights == null || LightVolumesWeights.Length != LightVolumes.Length) {
            LightVolumesWeights = new float[LightVolumes.Length];
        }

        var v05 = new Vector3(0.5f, 0.5f, 0.5f);

        // Update Weights because can be desynced
        for (int i = 0; i < LightVolumeDataList.Count; i++) {

            if (LightVolumes.Length <= i || !LightVolumes[i].gameObject.TryGetComponent(out LightVolume lightVolume)) continue;

            // Volume data
            var pos = lightVolume.GetPosition();
            var rot = lightVolume.GetRotation();
            var scl = lightVolume.GetScale();

            // Inversed World Matrix for Free rotation
            Matrix4x4 invMatrix = Matrix4x4.TRS(pos, rot, scl).inverse;
            Vector4 localEdgeSmooth = new Vector4(scl.x, scl.y, scl.z, 0) / EdgeSmoothing;
            Quaternion invBakedlRotation = Quaternion.Inverse(lightVolume.BakedRotation);

            LightVolumeDataList[i] = new LightVolumeData(
                i < LightVolumesWeights.Length ? LightVolumesWeights[i] : 0,
                localEdgeSmooth,
                invMatrix,
                LightVolumeDataList[i].UvwMin[0],
                LightVolumeDataList[i].UvwMin[1],
                LightVolumeDataList[i].UvwMin[2],
                LightVolumeDataList[i].UvwMax[0],
                LightVolumeDataList[i].UvwMax[1],
                LightVolumeDataList[i].UvwMax[2],
                invBakedlRotation,
                lightVolume.transform,
                lightVolume.IsAdditive
            );

        }
        
        var sortedData = LightVolumeDataSorter.SortData(LightVolumeDataList);
        
        LightVolumeDataSorter.GetData(sortedData, out Vector4[] invLocalEdgeSmooth, out Matrix4x4[] invWorldMatrix, out Vector4[] boundsUvwMin, out Vector4[] boundsUvwMax, out Quaternion[] invRotation, out Transform[] volumeTransforms, out float[] isAdditive);

        _udonLightVolumeManager.InvLocalEdgeSmooth = invLocalEdgeSmooth;
        _udonLightVolumeManager.BoundsUvwMin = boundsUvwMin;
        _udonLightVolumeManager.BoundsUvwMax = boundsUvwMax;
        _udonLightVolumeManager.InvWorldMatrix = invWorldMatrix;
        _udonLightVolumeManager.LightVolumeAtlas = LightVolumeAtlas;
        _udonLightVolumeManager.InvBakedRotations = invRotation;
        _udonLightVolumeManager.VolumesTransforms = volumeTransforms;
        _udonLightVolumeManager.IsAdditive = isAdditive;
        _udonLightVolumeManager.AutoUpdateVolumes = AutoUpdateVolumes;
        _udonLightVolumeManager.SharpBounds = SharpBounds;
        _udonLightVolumeManager.LightProbesBlending = LightProbesBlending;

        UpdateVolumes();

    }

    // Fixes light probes baked with Bakery L1
    private static void FixLightProbes() {

        var probes = LightmapSettings.lightProbes;
        if (probes == null || probes.count == 0) {
            Debug.LogWarning("[LightVolumeSetup] No Light Probes found to fix.");
            return;
        }

        var shs = probes.bakedProbes;
        for (int i = 0; i < shs.Length; ++i) {
            shs[i] = LVUtils.LinearizeSH(shs[i]);
        }

        probes.bakedProbes = shs;
        EditorUtility.SetDirty(probes);
        EditorSceneManager.MarkAllScenesDirty();

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();

        Debug.Log($"[LightVolumeSetup] {shs.Length} Light Probes fixed!");

    }

#endif



    public enum Baking {
        UnityLightmapper,
        Bakery
    }

}