using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class LightVolumeManager : UdonSharpBehaviour {

    public Texture3D LightVolume;
    [Range(0, 1f)] public float VolumesBlend = 0.5f;
    [Space]
    public float[] VolumesWeights;
    [Space]
    public Vector3[] BoundsWorldMin;
    public Vector3[] BoundsWorldMax;
    [Space]
    public Vector3[] BoundsUvwMin;
    public Vector3[] BoundsUvwMax;

    private void Start() {
        SetShaderProperties();
    }

    private void OnValidate() {
        SetShaderProperties();
    }

    [ContextMenu("Set Shader Properties")]
    public void SetShaderProperties() {

        if(LightVolume == null) {
            VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeEnabled"), 0);
            return;
        }

        // Initializing max volume count possible
        float[] LightVolumeWeight = new float[256];
        Vector4[] LightVolumeWorldMin = new Vector4[256];
        Vector4[] LightVolumeWorldMax = new Vector4[256];
        Vector4[] LightVolumeUvwMin = new Vector4[768];
        Vector4[] LightVolumeUvwMax = new Vector4[768];

        // Per volumes loop
        for (int i = 0; i < VolumesWeights.Length; i++) {
            LightVolumeWeight[i] = VolumesWeights[i];
            LightVolumeWorldMin[i] = BoundsWorldMin[i];
            LightVolumeWorldMax[i] = BoundsWorldMax[i];
        }

        // Per volumes loop x3
        for (int i = 0; i < BoundsUvwMin.Length; i++) {
            LightVolumeUvwMin[i] = BoundsUvwMin[i];
            LightVolumeUvwMax[i] = BoundsUvwMax[i];
        }

        // 3D texture and it's parameters
        VRCShader.SetGlobalTexture(VRCShader.PropertyToID("_UdonLightVolume"), LightVolume);
        VRCShader.SetGlobalVector(VRCShader.PropertyToID("_UdonLightVolumeTexelSize"), new Vector3(1f / LightVolume.width, 1f / LightVolume.height, 1f / LightVolume.depth));

        // Light volumes blending radius
        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeBlend"), Mathf.Max(VolumesBlend, 0.0001f));

        // All light volumes weights
        VRCShader.SetGlobalFloatArray(VRCShader.PropertyToID("_UdonLightVolumeWeight"), LightVolumeWeight);

        // All light volumes world bounds
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeWorldMin"), LightVolumeWorldMin);
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeWorldMax"), LightVolumeWorldMax);

        // All light volumes UVW
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeUvwMin"), LightVolumeUvwMin);
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeUvwMax"), LightVolumeUvwMax);

        // All light volumes count
        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeCount"), VolumesWeights.Length);

        // Defines if Light Volumes enabled in scene
        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeEnabled"), 1);

    }
}