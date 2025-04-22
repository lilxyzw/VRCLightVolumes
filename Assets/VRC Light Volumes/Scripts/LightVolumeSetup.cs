using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[RequireComponent(typeof(LightVolumeManager))]
public class LightVolumeSetup : SingletonEditor<LightVolumeSetup> {

    public LightVolume[] LightVolumes;
    public float[] LightVolumesWeights;
    public Baking BakingMode;

    public int StochasticIterations = 5000;
    public bool LinearizeSphericalHarmonics = true;
    public Texture3D LightVolumeAtlas;
    [SerializeField] public List<LightVolumeData> LightVolumeDataList = new List<LightVolumeData>();

    public bool IsBakeryMode => BakingMode == Baking.Bakery; // Just a shortcut

    private LightVolumeManager _udonLightVolumeManager;
    private Baking _bakingModePrev;

    public void SetShaderVariables() {
        if (_udonLightVolumeManager == null) _udonLightVolumeManager = GetComponent<LightVolumeManager>();
        if (_udonLightVolumeManager == null) return;
            _udonLightVolumeManager.SetShaderVariables();
    }

#if UNITY_EDITOR

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
                Debug.LogError("[LightVolumeAtlaser] One of the light volumes is not setuped!");
                return;
            }
            if (LightVolumes[i].Texture0 == null || LightVolumes[i].Texture1 == null || LightVolumes[i].Texture2 == null) {
                Debug.LogError("[LightVolumeAtlaser] One of the light volumes is not baked!");
                return;
            }
            textures[i * 3] = LightVolumes[i].Texture0;
            textures[i * 3 + 1] = LightVolumes[i].Texture1;
            textures[i * 3 + 2] = LightVolumes[i].Texture2;
        }

        var atlas = Texture3DAtlasGenerator.CreateAtlasStochastic(textures, StochasticIterations, LinearizeSphericalHarmonics);

        LightVolumeAtlas = atlas.Texture;

        LightVolumeDataList.Clear();

        

        for (int i = 0; i < LightVolumes.Length; i++) {

            int i3 = i * 3;

            LightVolumeDataList.Add(new LightVolumeData(
                i < LightVolumesWeights.Length ? LightVolumesWeights[i] : 0,
                0,
                Vector4.zero,
                Vector4.zero,
                atlas.BoundsUvwMin[i3],
                atlas.BoundsUvwMin[i3 + 1],
                atlas.BoundsUvwMin[i3 + 2],
                atlas.BoundsUvwMax[i3],
                atlas.BoundsUvwMax[i3 + 1],
                atlas.BoundsUvwMax[i3 + 2],
                Matrix4x4.identity
            ));

        }

        LVUtils.SaveTexture3DAsAsset(atlas.Texture, "Assets/BakeryLightmaps/Atlas3D.asset");

        SetupUdonBehaviour();

    }

    // Setups udon script
    [ContextMenu("Setup Udon Behaviour")]
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

            // Volume data
            var pos = LightVolumes[i].GetPosition();
            var rot = LightVolumes[i].GetRotation();
            var scl = LightVolumes[i].GetScale();

            float rotType = (int)LightVolumes[i].RotationType;

            Matrix4x4 invMatrix = Matrix4x4.identity;
            Vector4 datA = Vector4.zero;
            Vector4 datB = Vector4.zero;

            if (rotType == 0) {
                // Fixed rotation: A - WorldMin B - WorldMax
                datA = LVUtils.TransformPoint(-v05, pos, rot, scl);
                datB = LVUtils.TransformPoint(v05, pos, rot, scl);
            } else if (rotType == 1) {
                // Y Axis Rotation: A - BoundsCenter | SinY   B - InvBoundsSize | CosY
                float eulerY = rot.eulerAngles.y;
                datA = new Vector4(pos.x, pos.y, pos.z, Mathf.Sin(eulerY * Mathf.Deg2Rad));
                datB = new Vector4(1 / scl.x, 1 / scl.y, 1 / scl.z, Mathf.Cos(eulerY * Mathf.Deg2Rad));
            } else {
                // Inversed World Matrix for Free rotation
                invMatrix = Matrix4x4.TRS(pos, rot, scl).inverse;
                datB = new Vector4(1 / scl.x, 1 / scl.y, 1 / scl.z, 0);
            }

            LightVolumeDataList[i] = new LightVolumeData(
                i < LightVolumesWeights.Length ? LightVolumesWeights[i] : 0,
                rotType,
                datA,
                datB,
                LightVolumeDataList[i].UvwMin[0],
                LightVolumeDataList[i].UvwMin[1],
                LightVolumeDataList[i].UvwMin[2],
                LightVolumeDataList[i].UvwMax[0],
                LightVolumeDataList[i].UvwMax[1],
                LightVolumeDataList[i].UvwMax[2],
                invMatrix
            );

        }

        float[] rotationMode;
        Vector4[] dataA;
        Vector4[] dataB;
        Vector4[] boundsUvwMin;
        Vector4[] boundsUvwMax;
        Matrix4x4[] invWorldMatrix;

        var sortedData = LightVolumeDataSorter.SortData(LightVolumeDataList);
        LightVolumeDataSorter.GetData(sortedData, out rotationMode, out dataA, out dataB, out boundsUvwMin, out boundsUvwMax, out invWorldMatrix);

        _udonLightVolumeManager.RotationTypes = rotationMode;
        _udonLightVolumeManager.DataA = dataA;
        _udonLightVolumeManager.DataB = dataB;
        _udonLightVolumeManager.BoundsUvwMin = boundsUvwMin;
        _udonLightVolumeManager.BoundsUvwMax = boundsUvwMax;
        _udonLightVolumeManager.InvWorldMatrix = invWorldMatrix;
        _udonLightVolumeManager.LightVolumeAtlas = LightVolumeAtlas;

        SetShaderVariables();

    }

#endif

    public enum Baking {
        UnityLightmapper,
        Bakery
    }

}