using UnityEngine;

public class LightVolumeSetup : MonoBehaviour {

    public BakeryVolume[] BakeryVolumes;
    public float[] BakeryVolumesWeights;
    public int StochasticIterations = 5000;

    private LightVolumeManager _udonLightVolumeManager;

    public void SetShaderVariables() {
        if (_udonLightVolumeManager == null) _udonLightVolumeManager = GetComponent<LightVolumeManager>();
        if (_udonLightVolumeManager == null) return;
            _udonLightVolumeManager.SetShaderProperties();
    }

}