using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LightVolumeManager : UdonSharpBehaviour {

    [Tooltip("Combined Texture3D containing all Light Volumes' textures.")]
    public Texture3D LightVolumeAtlas;
    [Tooltip("When enabled, areas outside Light Volumes fall back to light probes. Otherwise, the Light Volume with the smallest weight is used as fallback. It also improves performance.")]
    public bool LightProbesBlending = true;
    [Tooltip("Disables smooth blending with areas outside Light Volumes. Use it if your entire scene's play area is covered by Light Volumes. It also improves performance.")]
    public bool SharpBounds = true;
    [Tooltip("Automatically updates a volume's position, rotation, and scale in Play mode using an Udon script. Use only if you have movable volumes in your scene.")]
    public bool AutoUpdateVolumes = false;
    [Tooltip("Limits the maximum number of additive volumes that can affect a single pixel. If you have many dynamic additive volumes that may overlap, it's good practice to limit overdraw to maintain performance.")]
    public int AdditiveMaxOverdraw = 4;
    [Tooltip("All Light Volume instances sorted in decreasing order by weight. You can enable or disable volumes game objects at runtime. Manually disabling unnecessary volumes improves performance.")]
    public LightVolumeInstance[] LightVolumeInstances = new LightVolumeInstance[0];
    private bool _isInitialized = false;

    // Actually enabled Volumes
    private int _enabledCount = 0;
    private int[] _enabledIDs = new int[256];
    private Vector4[] _invLocalEdgeSmooth = new Vector4[0];
    private Matrix4x4[] _invWorldMatrix = new Matrix4x4[0];
    private Vector4[] _boundsUvwMin = new Vector4[0];
    private Vector4[] _boundsUvwMax = new Vector4[0];
    private float[] _isRotated = new float[0];
    private Vector4[] _relativeRotations = new Vector4[0];
    private Vector4[] _colors = new Vector4[0];
    private int _additiveCount = 0;

    // Initializing gloabal shader arrays if needed 
    private void TryInitialize() {
        if (_isInitialized) return;
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeInvLocalEdgeSmooth"), new Vector4[256]);
        VRCShader.SetGlobalMatrixArray(VRCShader.PropertyToID("_UdonLightVolumeInvWorldMatrix"), new Matrix4x4[256]);
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeRotation"), new Vector4[256]);
        VRCShader.SetGlobalFloatArray(VRCShader.PropertyToID("_UdonLightVolumeIsRotated"), new float[256]);
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeUvwMin"), new Vector4[756]);
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeUvwMax"), new Vector4[756]);
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeColor"), new Vector4[256]);
        _isInitialized = true;
    }

    private void Update() {
        if(!AutoUpdateVolumes) return;
        UpdateVolumes();
    }

    // Recalculates dynamic volumes
    private void UpdateDynamicVolumes() {

        // Searching for enabled volumes
        _enabledCount = 0;
        _additiveCount = 0;
        for (int i = 0; i < LightVolumeInstances.Length; i++) {
            if (LightVolumeInstances[i] != null && LightVolumeInstances[i].gameObject.activeInHierarchy) {
#if UNITY_EDITOR
                LightVolumeInstances[i].UpdateRotation();
#else
                if (LightVolumeInstances[i].IsDynamic) LightVolumeInstances[i].UpdateRotation();
#endif
                if (LightVolumeInstances[i].IsAdditive) _additiveCount++;
                _enabledIDs[_enabledCount] = i;
                _enabledCount++;
            }
        }

        // Initializing required arrays
        _invLocalEdgeSmooth = new Vector4[_enabledCount];
        _invWorldMatrix     = new Matrix4x4[_enabledCount];
        _isRotated          = new float[_enabledCount];
        _colors             = new Vector4[_enabledCount];
        _relativeRotations  = new Vector4[_enabledCount];
        _boundsUvwMin       = new Vector4[_enabledCount * 3];
        _boundsUvwMax       = new Vector4[_enabledCount * 3];

        // Filling arrays with enabled volumes
        for (int i = 0; i < _enabledCount; i++) {
            
            int enabledId = _enabledIDs[i];
            int i3 = i * 3;
            int i31 = i3 + 1;
            int i32 = i3 + 2;

            _invLocalEdgeSmooth[i] = LightVolumeInstances[enabledId].InvLocalEdgeSmoothing;
            _invWorldMatrix[i]     = LightVolumeInstances[enabledId].InvWorldMatrix;
            _isRotated[i]          = LightVolumeInstances[enabledId].IsRotated ? 1 : 0;
            _relativeRotations[i]  = LightVolumeInstances[enabledId].RelativeRotation;
            _colors[i]             = LightVolumeInstances[enabledId].Color;

            _boundsUvwMin[i3]      = LightVolumeInstances[enabledId].BoundsUvwMin0;
            _boundsUvwMin[i31]     = LightVolumeInstances[enabledId].BoundsUvwMin1;
            _boundsUvwMin[i32]     = LightVolumeInstances[enabledId].BoundsUvwMin2;
            
            _boundsUvwMax[i3]      = LightVolumeInstances[enabledId].BoundsUvwMax0;
            _boundsUvwMax[i31]     = LightVolumeInstances[enabledId].BoundsUvwMax1;
            _boundsUvwMax[i32]     = LightVolumeInstances[enabledId].BoundsUvwMax2;
            
        }

    }

    private void Start() {
        TryInitialize();
        UpdateVolumes();
    }

    public void UpdateVolumes() {

#if UNITY_EDITOR
        // Only need to check initialization here in editor
        TryInitialize();
#endif

        UpdateDynamicVolumes(); // Update dynamic volumes

        if (LightVolumeAtlas == null || _enabledCount == 0) {
            VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeEnabled"), 0);
            return;
        }

        // 3D texture and it's parameters
        VRCShader.SetGlobalTexture(VRCShader.PropertyToID("_UdonLightVolume"), LightVolumeAtlas);

        // Defines if Light Probes Blending enabled in scene
        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeProbesBlend"), LightProbesBlending ? 1 : 0);
        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeSharpBounds"), SharpBounds ? 1 : 0);

        // All light volumes Extra Data
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeInvLocalEdgeSmooth"), _invLocalEdgeSmooth);
        VRCShader.SetGlobalMatrixArray(VRCShader.PropertyToID("_UdonLightVolumeInvWorldMatrix"), _invWorldMatrix);

        // All light volumes UVW
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeUvwMin"), _boundsUvwMin);
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeUvwMax"), _boundsUvwMax);

        // All light volumes count
        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeCount"), _enabledCount);
        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeAdditiveCount"), _additiveCount);
        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeAdditiveMaxOverdraw"), Mathf.Min(Mathf.Max(AdditiveMaxOverdraw, 0), _additiveCount));

        // Defines if Light Volumes enabled in scene
        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeEnabled"), 1);

        // Volume's relative rotation
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeRotation"), _relativeRotations);
        VRCShader.SetGlobalFloatArray(VRCShader.PropertyToID("_UdonLightVolumeIsRotated"), _isRotated);

        // Volume's color correction
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeColor"), _colors);

    }
}