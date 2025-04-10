
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class LightVolumeManager : UdonSharpBehaviour {

    public Texture3D LightVolume;
    public float[] BakeryVolumesWeights;
    [SerializeField] private Vector3[] boundsWorldMin;
    [SerializeField] private Vector3[] boundsWorldMax;
    [SerializeField] private Vector3[] boundsUvwMin;
    [SerializeField] private Vector3[] boundsUvwMax;
    [SerializeField] private float EdgeBlend;

    void Start() {

        float[] LightVolumeWeight = new float[256];
        Vector4[] LightVolumeWorldMin = new Vector4[256];
        Vector4[] LightVolumeWorldMax = new Vector4[256];
        Vector4[] LightVolumeUvwMin = new Vector4[768];
        Vector4[] LightVolumeUvwMax = new Vector4[768];

        for (int i = 0; i < BakeryVolumesWeights.Length; i++) {

            // Weight
            LightVolumeWeight[i] = BakeryVolumesWeights.Length > 0 ? BakeryVolumesWeights[Mathf.Clamp(i, 0, BakeryVolumesWeights.Length)] : 0;

            // World bounds
            LightVolumeWorldMin[i] = boundsWorldMin[i];
            LightVolumeWorldMax[i] = boundsWorldMax[i];

        }

        for (int i = 0; i < boundsUvwMin.Length; i++) {
            LightVolumeUvwMin[i] = boundsUvwMin[i];
            LightVolumeUvwMax[i] = boundsUvwMax[i];
        }

        Vector3 size = new Vector3(LightVolume.width, LightVolume.height, LightVolume.depth);
        VRCShader.SetGlobalVector(VRCShader.PropertyToID("_UdonLightVolumeTexelSize"), new Vector3(1f / size.x, 1f / size.y, 1f / size.z));
        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeBlend"), Mathf.Max(EdgeBlend, 0.0001f));
        VRCShader.SetGlobalFloatArray(VRCShader.PropertyToID("_UdonLightVolumeWeight"), LightVolumeWeight);
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeWorldMin"), LightVolumeWorldMin);
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeWorldMax"), LightVolumeWorldMax);

        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeUvwMin"), LightVolumeUvwMin);
        VRCShader.SetGlobalVectorArray(VRCShader.PropertyToID("_UdonLightVolumeUvwMax"), LightVolumeUvwMax);

        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeCount"), BakeryVolumesWeights.Length);
        VRCShader.SetGlobalTexture(VRCShader.PropertyToID("_UdonLightVolume"), LightVolume);

        VRCShader.SetGlobalFloat(VRCShader.PropertyToID("_UdonLightVolumeEnabled"), 1);

    }
}
