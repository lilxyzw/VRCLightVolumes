using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LightVolumeManager : UdonSharpBehaviour {

    public Texture3D LightVolumeAtlas;
    public bool LightProbesBlending = true;
    public bool SharpBounds = true;
    public bool AutoUpdateVolumes = false;
    [Space]
    public Transform[] VolumesTransforms = new Transform[0];
    public Quaternion[] InvBakedRotations = new Quaternion[0];
    [Space]
    public Vector4[] InvLocalEdgeSmooth = new Vector4[0];
    public Matrix4x4[] InvWorldMatrix = new Matrix4x4[0];
    [Space]
    public Vector4[] BoundsUvwMin = new Vector4[0];
    public Vector4[] BoundsUvwMax = new Vector4[0];
    [Space]
    public float[] IsAdditive = new float[0];

    private Vector4[] _rotations = new Vector4[0];
    private float[] _needsToRotate = new float[0];

    private bool _isInitialized = false;

    // Actually enabled Volumes
    private int _enabledCount = 0;
    private int[] _enabledIDs = new int[256];
    private Transform[] _volumesTransforms = new Transform[0];
    private Quaternion[] _invBakedRotations = new Quaternion[0];
    private Vector4[] _invLocalEdgeSmooth = new Vector4[0];
    private Matrix4x4[] _invWorldMatrix = new Matrix4x4[0];
    private Vector4[] _boundsUvwMin = new Vector4[0];
    private Vector4[] _boundsUvwMax = new Vector4[0];
    private float[] _isAdditive = new float[0];


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
        if(!AutoUpdateVolumes) return;
        UpdateVolumes();
    }

    private void UpdateEnabledVolumes() {

        // Searching for enabled volumes
        _enabledCount = 0;
        for (int i = 0; i < VolumesTransforms.Length; i++) {
            if (VolumesTransforms[i] != null && VolumesTransforms[i].gameObject.activeInHierarchy) {
                _enabledIDs[_enabledCount] = i;
                _enabledCount++;
            }
        }

        // Filtering disabled volumes
        if (_enabledCount != VolumesTransforms.Length) { // If something is disabled

            _volumesTransforms = new Transform[_enabledCount];
            _invBakedRotations = new Quaternion[_enabledCount];
            _invLocalEdgeSmooth = new Vector4[_enabledCount];
            _invWorldMatrix = new Matrix4x4[_enabledCount];
            _boundsUvwMin = new Vector4[_enabledCount * 3];
            _boundsUvwMax = new Vector4[_enabledCount * 3];
            _isAdditive = new float[_enabledCount];

            for (int i = 0; i < _enabledCount; i++) {
                
                int enabledId = _enabledIDs[i];

                int i3 = i * 3;
                int i31 = i3 + 1;
                int i32 = i3 + 2;

                int e3 = enabledId * 3;
                int e31 = e3 + 1;
                int e32 = e3 + 2;

                _volumesTransforms[i] = VolumesTransforms[enabledId];
                _invBakedRotations[i] = InvBakedRotations[enabledId];
                _invLocalEdgeSmooth[i] = InvLocalEdgeSmooth[enabledId];
                _invWorldMatrix[i] = InvWorldMatrix[enabledId];
                _isAdditive[i] = IsAdditive[enabledId];

                _boundsUvwMin[i3] = BoundsUvwMin[e3];
                _boundsUvwMin[i31] = BoundsUvwMin[e31];
                _boundsUvwMin[i32] = BoundsUvwMin[e32];
                
                _boundsUvwMax[i3] = BoundsUvwMax[e3];
                _boundsUvwMax[i31] = BoundsUvwMax[e31];
                _boundsUvwMax[i32] = BoundsUvwMax[e32];
                
            }

        } else { // Everything is enabled
            _volumesTransforms = VolumesTransforms;
            _invBakedRotations = InvBakedRotations;
            _invLocalEdgeSmooth = InvLocalEdgeSmooth;
            _invWorldMatrix = InvWorldMatrix;
            _boundsUvwMin = BoundsUvwMin;
            _boundsUvwMax = BoundsUvwMax;
            _isAdditive = IsAdditive;
        }

    }

    private void UpdateRotations() {
        _rotations = new Vector4[_enabledCount];
        _needsToRotate = new float[_enabledCount];
        for (int i = 0; i < _enabledCount; i++) {
            InvWorldMatrix[i] = Matrix4x4.TRS(_volumesTransforms[i].position, _volumesTransforms[i].rotation, _volumesTransforms[i].lossyScale).inverse;
            Quaternion rot = _volumesTransforms[i].rotation * _invBakedRotations[i];
            _needsToRotate[i] = rot == Quaternion.identity ? 0 : 1;
            _rotations[i] = new Vector4(rot.x, rot.y, rot.z, rot.w);
        }
    }

    private void Start() {
        TryInitialize();
        UpdateVolumes();
    }

    private void OnValidate() {
        UpdateVolumes();
    }

    public void UpdateVolumes() {

#if UNITY_EDITOR
        // Only need to check initialization here in editor
        TryInitialize();
#endif

        UpdateEnabledVolumes();
        UpdateRotations();

        if (LightVolumeAtlas == null || _enabledCount == 0) {
            VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeEnabled"), 0);
            return;
        }

        // 3D texture and it's parameters
        VRCShader.SetGlobalTexture(VRCShader.PropertyToID("_UdonLightVolume"), LightVolumeAtlas);

        // Defines if Light Probes Blending enabled in scene
        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeProbesBlend"), LightProbesBlending ? 1 : 0);
        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeSharpBounds"), SharpBounds ? 1 : 0);
        VRCShader.SetGlobalFloatArray(VRCShader.PropertyToID("_UdonLightVolumeAdditive"), _isAdditive);

        // All light volumes Extra Data
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeInvLocalEdgeSmooth"), _invLocalEdgeSmooth);
        VRCShader.SetGlobalMatrixArray(VRCShader.PropertyToID("_UdonLightVolumeInvWorldMatrix"), _invWorldMatrix);

        // All light volumes UVW
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeUvwMin"), _boundsUvwMin);
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeUvwMax"), _boundsUvwMax);

        // All light volumes count
        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeCount"), _enabledCount);

        // Defines if Light Volumes enabled in scene
        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeEnabled"), 1);

        // Volume's relative rotation
        
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeRotation"), _rotations);
        VRCShader.SetGlobalFloatArray(VRCShader.PropertyToID("_UdonLightVolumeNeedsRotation"), _needsToRotate);

    }
}