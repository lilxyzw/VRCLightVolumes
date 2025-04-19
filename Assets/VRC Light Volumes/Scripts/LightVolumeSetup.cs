using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[RequireComponent(typeof(LightVolumeManager))]
public class LightVolumeSetup : SingletonEditor<LightVolumeSetup> {

    public LightVolume[] LightVolumes;
    public float[] LightVolumesWeights;
    public Baking BakingMode;

    public int StochasticIterations = 5000;
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

        var atlas = Texture3DAtlasGenerator.CreateAtlasStochastic(textures, StochasticIterations);

        LightVolumeAtlas = atlas.Texture;

        LightVolumeDataList.Clear();

        var v05 = new Vector3(0.5f, 0.5f, 0.5f); 

        for (int i = 0; i < LightVolumes.Length; i++) {
            
            int i3 = i * 3;

            // Volume data
            var pos = LightVolumes[i].GetPosition();
            var rot = LightVolumes[i].GetRotation();
            var scl = LightVolumes[i].GetScale();

            float rotationMode = (int)LightVolumes[i].RotationType;

            Matrix4x4 invWorldMatrix = Matrix4x4.identity;
            Vector4 dataA = Vector4.zero;
            Vector4 dataB = Vector4.zero;
            
            if (rotationMode == 0) {
                // Fixed rotation: A - WorldMin B - WorldMax
                dataA = LVUtils.TransformPoint(-v05, pos, rot, scl);
                dataB = LVUtils.TransformPoint( v05, pos, rot, scl);
            } else if (rotationMode == 1) {
                // Y Axis Rotation: A - BoundsCenter | SinY   B - InvBoundsSize | CosY
                float eulerY = rot.eulerAngles.y;
                dataA = new Vector4(pos.x, pos.y, pos.z, Mathf.Sin(eulerY * Mathf.Deg2Rad));
                dataB = new Vector4(1 / scl.x, 1 / scl.y, 1 / scl.z, Mathf.Cos(eulerY * Mathf.Deg2Rad));
            } else {
                // Inversed World Matrix for Free rotation
                invWorldMatrix = LightVolumes[i].GetMatrixTRS().inverse;
            }

            LightVolumeDataList.Add(new LightVolumeData(
                i < LightVolumesWeights.Length ? LightVolumesWeights[i] : 0,
                rotationMode,
                dataA,
                dataB,
                atlas.BoundsUvwMin[i3],
                atlas.BoundsUvwMin[i3 + 1],
                atlas.BoundsUvwMin[i3 + 2],
                atlas.BoundsUvwMax[i3],
                atlas.BoundsUvwMax[i3 + 1],
                atlas.BoundsUvwMax[i3 + 2],
                invWorldMatrix
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

        // Update Weights because can be desynced
        for (int i = 0; i < LightVolumeDataList.Count; i++) {
            LightVolumeDataList[i] = new LightVolumeData(
                i < LightVolumesWeights.Length ? LightVolumesWeights[i] : 0,
                LightVolumeDataList[i].RotationMode,
                LightVolumeDataList[i].DataA,
                LightVolumeDataList[i].DataB,
                LightVolumeDataList[i].UvwMin[0],
                LightVolumeDataList[i].UvwMin[1],
                LightVolumeDataList[i].UvwMin[2],
                LightVolumeDataList[i].UvwMax[0],
                LightVolumeDataList[i].UvwMax[1],
                LightVolumeDataList[i].UvwMax[2],
                LightVolumeDataList[i].InvWorldMatrix
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