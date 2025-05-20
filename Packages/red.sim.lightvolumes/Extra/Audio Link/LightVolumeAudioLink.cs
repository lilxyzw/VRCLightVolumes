#if AUDIOLINK

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
        
        public LightVolumeInstance TargetLightVolume;
        public AudioLink.AudioLink AudioLink;
        public AudioLinkBand AudioBand = AudioLinkBand.Bass;
        public float VolumeInrtensity = 1;
        public float MaterialsInrtensity = 2;
        public Renderer[] MeshRenderers;

        private int _emissionColorID;
        private MaterialPropertyBlock _block;

        private void InitIDs() {
            _emissionColorID = VRCShader.PropertyToID("_EmissionColor");
        }

        private void Reset() {
            TargetLightVolume = GetComponent<LightVolumeInstance>();
        }

        private void Start() {
            _block = new MaterialPropertyBlock();
            InitIDs();
        }

        private void Update() {
            int band = (int)AudioBand;
            Color color = Vector4.Scale(AudioLink.GetDataAtPixel(15, 28 + band), AudioLink.GetDataAtPixel(band, 23));
            TargetLightVolume.Color = color * VolumeInrtensity;
            _block.SetColor(_emissionColorID, color * MaterialsInrtensity);
            for (int i = 0; i < MeshRenderers.Length; i++) {
                MeshRenderers[i].SetPropertyBlock(_block);
            }
        }

    }

    public enum AudioLinkBand {
        Bass = 0,
        LowMid = 1,
        HighMid = 2,
        Treble = 3
    }

}

#endif