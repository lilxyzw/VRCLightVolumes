using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class LightVolumeManager : UdonSharpBehaviour {

    public bool DynamicUpdate;
    public Texture3D LightVolumeAtlas;
    public bool LightProbesBlending = false;
    public bool SharpBounds = false;
    [Space]
    public Transform[] VolumesTransforms;
    public Quaternion[] InvBakedRotations;
    [Space]
    public Vector4[] InvLocalEdgeSmooth;
    public Matrix4x4[] InvWorldMatrix;
    [Space]
    public Vector4[] BoundsUvwMin;
    public Vector4[] BoundsUvwMax;
    [Space]
    public float[] IsAdditive;

    public Vector4[] _rotations;
    public float[] _needsToRotate;

    private bool _isInitialized = false;

    // Initializing gloabal shader arrays if needed 
    private void TryInitialize() {
        if (_isInitialized) return;
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeInvLocalEdgeSmooth"), new Vector4[256]);
        VRCShader.SetGlobalMatrixArray(VRCShader.PropertyToID("_UdonLightVolumeInvWorldMatrix"), new Matrix4x4[256]);
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeRotation"), new Vector4[256]);
        VRCShader.SetGlobalFloatArray(VRCShader.PropertyToID("_UdonLightVolumeNeedsRotation"), new float[256]);
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeUvwMin"), new Vector4[756]);
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeUvwMax"), new Vector4[756]);
        VRCShader.SetGlobalFloatArray(VRCShader.PropertyToID("_UdonLightVolumeAdditive"), new float[256]);
        _isInitialized = true;
    }

    private void Update() {
        if(!DynamicUpdate) return;
        SetShaderVariables();
    }

    private void UpdateRotations() {
        _rotations = new Vector4[VolumesTransforms.Length];
        _needsToRotate = new float[VolumesTransforms.Length];
        for (int i = 0; i < VolumesTransforms.Length; i++) {
            InvWorldMatrix[i] = Matrix4x4.TRS(VolumesTransforms[i].position, VolumesTransforms[i].rotation, VolumesTransforms[i].lossyScale).inverse;
            Quaternion rot = VolumesTransforms[i].rotation * InvBakedRotations[i];
            _needsToRotate[i] = rot == Quaternion.identity ? 0 : 1;
            _rotations[i] = new Vector4(rot.x, rot.y, rot.z, rot.w);
        }
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

        UpdateRotations();

        if (LightVolumeAtlas == null || InvLocalEdgeSmooth.Length == 0 || BoundsUvwMin.Length == 0 || BoundsUvwMax.Length == 0 || _rotations.Length == 0 || _needsToRotate.Length == 0) {
            VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeEnabled"), 0);
            return;
        }

        // 3D texture and it's parameters
        VRCShader.SetGlobalTexture(VRCShader.PropertyToID("_UdonLightVolume"), LightVolumeAtlas);

        // Defines if Light Probes Blending enabled in scene
        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeProbesBlend"), LightProbesBlending ? 1 : 0);
        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeSharpBounds"), SharpBounds ? 1 : 0);
        VRCShader.SetGlobalFloatArray(VRCShader.PropertyToID("_UdonLightVolumeAdditive"), IsAdditive);

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

        // Volume's relative rotation
        
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeRotation"), _rotations);
        VRCShader.SetGlobalFloatArray(VRCShader.PropertyToID("_UdonLightVolumeNeedsRotation"), _needsToRotate);

    }
}