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
        [Range(0, 127)] public int Delay = 0;
        public bool SmoothingEnabled = true;
        [Range(0, 1)] public float Smoothing = 0.25f;
        [Space]
        public bool OverrideColor = false;
        [ColorUsage(showAlpha: false)] public Color Color = Color.white;
        [Space]
        public LightVolumeInstance[] TargetLightVolumes;
        public PointLightVolumeInstance[] TargetPointLightVolumes;
        public Renderer[] TargetMeshRenderers;
        public float MaterialsIntensity = 2;

        private int _emissionColorID;
        private MaterialPropertyBlock _block;

        private Color _prevColor;

        private void InitIDs() {
            _emissionColorID = VRCShader.PropertyToID("_EmissionColor");
        }

        private void Start() {
            _block = new MaterialPropertyBlock();
            InitIDs();
            _prevColor = Color.black;
        }

        private void Update() {

            int band = (int)AudioBand;
            Color color;
            if (OverrideColor) {
                color = Vector4.Scale(AudioLink.GetDataAtPixel(Delay, band), Color);
            } else {
                color = Vector4.Scale(AudioLink.GetDataAtPixel(Delay, band), AudioLink.GetDataAtPixel(band, 23));
            }

            if (SmoothingEnabled) {
                float diff = ColorDifference(color, _prevColor); // Difference between prev and current color
                float smoothing = Time.deltaTime / Mathf.Lerp(Mathf.Lerp(0.25f, 1f, Smoothing), Mathf.Lerp(1e-05f, 0.1f, Smoothing), Mathf.Pow(diff * 1.5f, 0.1f)); // Smoothing speed depends on the color difference
                _prevColor = Color.Lerp(_prevColor, color, smoothing); // Actually smoothing colors
            } else {
                _prevColor = color;
            }

            for (int i = 0; i < TargetLightVolumes.Length; i++) {
                TargetLightVolumes[i].Color = _prevColor;
            }
            for (int i = 0; i < TargetPointLightVolumes.Length; i++) {
                TargetPointLightVolumes[i].Color = _prevColor;
            }
            _block.SetColor(_emissionColorID, _prevColor * MaterialsIntensity);
            for (int i = 0; i < TargetMeshRenderers.Length; i++) {
                TargetMeshRenderers[i].SetPropertyBlock(_block);
            }

        }

        private float ColorDifference(Color colorA, Color colorB) {
            float rmean = (colorA.r + colorB.r) * 0.5f;
            float r = colorA.r - colorB.r;
            float g = colorA.g - colorB.g;
            float b = colorA.b - colorB.b;
            return Mathf.Sqrt((2f + rmean) * r * r + 4f * g * g + (3f - rmean) * b * b) / 3;
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