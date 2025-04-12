using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class LightVolumeManager : UdonSharpBehaviour {

    public Texture3D LightVolume;
    [Range(0, 1f)] public float VolumesBlend = 0.5f;
    [Space]
    public Vector4[] BoundsWorldMin;
    public Vector4[] BoundsWorldMax;
    [Space]
    public Vector4[] BoundsUvwMin;
    public Vector4[] BoundsUvwMax;

    // Buffered
    private int _lightVolumeEnabledID;
    private int _lightVolumeID;
    private int _lightVolumeBlendID;
    private int _lightVolumeWorldMinID;
    private int _lightVolumeWorldMaxID;
    private int _lightVolumeUvwMinID;
    private int _lightVolumeUvwMaxID;
    private int _lightVolumeCountID;

    private bool _isInitialized = false;

    // Initializing IDs and 
    private void InitializeShaderVariables() {

        if (!_isInitialized) return;

        _lightVolumeID = VRCShader.PropertyToID("_UdonLightVolume");
        _lightVolumeBlendID = VRCShader.PropertyToID("_UdonLightVolumeBlend");
        _lightVolumeWorldMinID = VRCShader.PropertyToID("_UdonLightVolumeWorldMin");
        _lightVolumeWorldMaxID = VRCShader.PropertyToID("_UdonLightVolumeWorldMax");
        _lightVolumeUvwMinID = VRCShader.PropertyToID("_UdonLightVolumeUvwMin");
        _lightVolumeUvwMaxID = VRCShader.PropertyToID("_UdonLightVolumeUvwMax");
        _lightVolumeCountID = VRCShader.PropertyToID("_UdonLightVolumeCount");
        _lightVolumeEnabledID = VRCShader.PropertyToID("_UdonLightVolumeEnabled");

        VRCShader.SetGlobalVectorArray(_lightVolumeWorldMinID, new Vector4[256]);
        VRCShader.SetGlobalVectorArray(_lightVolumeWorldMaxID, new Vector4[256]);
        VRCShader.SetGlobalVectorArray(_lightVolumeUvwMinID, new Vector4[756]);
        VRCShader.SetGlobalVectorArray(_lightVolumeUvwMaxID, new Vector4[756]);

        _isInitialized = true;

    }

    private void Awake() {
        InitializeShaderVariables();
    }

    private void Start() {
        SetShaderVariables();
    }

    private void OnValidate() {
        SetShaderVariables();
    }

    [ContextMenu("Set Shader Variables")]
    public void SetShaderVariables() {

        InitializeShaderVariables();

        if (LightVolume == null || BoundsWorldMin.Length == 0 || BoundsWorldMax.Length == 0 || BoundsUvwMin.Length == 0 || BoundsUvwMax.Length == 0) {
            VRCShader.SetGlobalFloat(_lightVolumeEnabledID, 0);
            return;
        }

        // 3D texture and it's parameters
        VRCShader.SetGlobalTexture(_lightVolumeID, LightVolume);

        // Light volumes blending radius
        VRCShader.SetGlobalFloat(_lightVolumeBlendID, Mathf.Max(VolumesBlend, 0.0001f));

        // All light volumes world bounds
        VRCShader.SetGlobalVectorArray(_lightVolumeWorldMinID, BoundsWorldMin);
        VRCShader.SetGlobalVectorArray(_lightVolumeWorldMaxID, BoundsWorldMax);

        // All light volumes UVW
        VRCShader.SetGlobalVectorArray(_lightVolumeUvwMinID, BoundsUvwMin);
        VRCShader.SetGlobalVectorArray(_lightVolumeUvwMaxID, BoundsUvwMax);

        // All light volumes count
        VRCShader.SetGlobalFloat(_lightVolumeCountID, BoundsWorldMin.Length);

        // Defines if Light Volumes enabled in scene
        VRCShader.SetGlobalFloat(_lightVolumeEnabledID, 1);

    }
}