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

    private void Start() {
        SetShaderVariables();
    }

    private void OnValidate() {
        SetShaderVariables();
    }

    [ContextMenu("Set Shader Variables")]
    public void SetShaderVariables() {

        if(LightVolume == null || BoundsWorldMin.Length == 0 || BoundsWorldMax.Length == 0 || BoundsUvwMin.Length == 0 || BoundsUvwMax.Length == 0) {
            VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeEnabled"), 0);
            return;
        }

        // 3D texture and it's parameters
        VRCShader.SetGlobalTexture(VRCShader.PropertyToID("_UdonLightVolume"), LightVolume);

        // Light volumes blending radius
        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeBlend"), Mathf.Max(VolumesBlend, 0.0001f));

        // All light volumes world bounds
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeWorldMin"), BoundsWorldMin);
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeWorldMax"), BoundsWorldMax);

        // All light volumes UVW
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeUvwMin"), BoundsUvwMin);
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeUvwMax"), BoundsUvwMax);

        // All light volumes count
        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeCount"), BoundsWorldMin.Length);

        // Defines if Light Volumes enabled in scene
        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeEnabled"), 1);

    }
}