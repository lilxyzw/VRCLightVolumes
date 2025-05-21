using UnityEngine;

#if UDONSHARP
using UdonSharp;
using VRC.SDKBase;
#else
using VRCShader = UnityEngine.Shader;
#endif

namespace VRCLightVolumes {

#if UDONSHARP
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LightVolumeAudioLink : UdonSharpBehaviour
#else
    public class LightVolumeAudioLink : MonoBehaviour
#endif
    {
#if AUDIOLINK

        public AudioLink.AudioLink AudioLink;
        public AudioLinkBand AudioBand = AudioLinkBand.Bass;
        public float VolumeInrtensity = 1;
        public float MaterialsInrtensity = 2;
        public LightVolumeInstance[] TargetLightVolumes;
        public Renderer[] TargetMeshRenderers;

        private int _emissionColorID;
        private MaterialPropertyBlock _block;

        private void InitIDs() {
            _emissionColorID = VRCShader.PropertyToID("_EmissionColor");
        }

        private void Start() {
            _block = new MaterialPropertyBlock();
            InitIDs();
        }

        private void Update() {
            int band = (int)AudioBand;
            Color color = Vector4.Scale(AudioLink.GetDataAtPixel(15, 28 + band), AudioLink.GetDataAtPixel(band, 23));
            Color volumeColor = color * VolumeInrtensity;
            for (int i = 0; i < TargetLightVolumes.Length; i++) {
                TargetLightVolumes[i].Color = volumeColor;
            }
            _block.SetColor(_emissionColorID, color * MaterialsInrtensity);
            for (int i = 0; i < TargetMeshRenderers.Length; i++) {
                TargetMeshRenderers[i].SetPropertyBlock(_block);
            }
        }
#endif
    }

    public enum AudioLinkBand {
        Bass = 0,
        LowMid = 1,
        HighMid = 2,
        Treble = 3
    }

}