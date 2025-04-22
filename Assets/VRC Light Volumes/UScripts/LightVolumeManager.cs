using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class LightVolumeManager : UdonSharpBehaviour {

    public Texture3D LightVolumeAtlas;
    public bool LightProbesBlending = false;
    public bool SharpBounds = false;
    [Space]
    public int[] DynamicVolumesIDs;
    public LightVolumeUdon[] DynamicVolumes;
    [Space]
    public Vector4[] InvLocalEdgeSmooth;
    public Matrix4x4[] InvWorldMatrix;
    [Space]
    public Vector4[] BoundsUvwMin;
    public Vector4[] BoundsUvwMax;

    private bool _isInitialized = false;

    // Initializing gloabal shader arrays if needed 
    private void TryInitialize() {
        if (_isInitialized) return;
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeInvLocalEdgeSmooth"), new Vector4[256]);
        VRCShader.SetGlobalMatrixArray(VRCShader.PropertyToID("_UdonLightVolumeInvWorldMatrix"), new Matrix4x4[256]);
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeUvwMin"), new Vector4[756]);
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeUvwMax"), new Vector4[756]);
        _isInitialized = true;
    }

    private void Update() {
        if (DynamicVolumesIDs.Length == 0) return; // Update nothing if no dynamic volumes 
    }

    private void Start() {
        TryInitialize();
        SetShaderVariables();
    }

    private void OnValidate() {
        SetShaderVariables();
    }

    public void SetShaderVariables() {

#if UNITY_EDITOR
        // Only need to check initialization here in editor
        TryInitialize();
#endif

        if (LightVolumeAtlas == null || InvLocalEdgeSmooth.Length == 0 || BoundsUvwMin.Length == 0 || BoundsUvwMax.Length == 0) {
            VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeEnabled"), 0);
            return;
        }

        // 3D texture and it's parameters
        VRCShader.SetGlobalTexture(VRCShader.PropertyToID("_UdonLightVolume"), LightVolumeAtlas);

        // Defines if Light Probes Blending enabled in scene
        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeProbesBlend"), LightProbesBlending ? 1 : 0);
        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeSharpBounds"), SharpBounds ? 1 : 0);

        // All light volumes Extra Data
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeInvLocalEdgeSmooth"), InvLocalEdgeSmooth);
        VRCShader.SetGlobalMatrixArray(VRCShader.PropertyToID("_UdonLightVolumeInvWorldMatrix"), InvWorldMatrix);

        // All light volumes UVW
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeUvwMin"), BoundsUvwMin);
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeUvwMax"), BoundsUvwMax);

        // All light volumes count
        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeCount"), InvLocalEdgeSmooth.Length);

        // Defines if Light Volumes enabled in scene
        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeEnabled"), 1);

    }
}