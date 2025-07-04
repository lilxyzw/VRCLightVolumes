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
        [Tooltip("Reference to your Audio Link manager that should control Light Volumes")]
        public AudioLink.AudioLink AudioLink;
        [Tooltip("Defines which audio band will be used to control Light Volumes. Four bands available: Bass, Low Mid, High Mid, Treble")]
        public AudioLinkBand AudioBand = AudioLinkBand.Bass;
        [Tooltip("Defines how many samples back in history we're getting data from. Can be a value from 0 to 127. Zero means no delay at all")]
        [Range(0, 127)] public int Delay = 0;
        [Tooltip("Enables smoothing algorithm that tries to smooth out flickering that can usually be a problem")]
        public bool SmoothingEnabled = true;
        [Tooltip("Value from 0 to 1 that defines how much smoothing should be applied. Zero usually applies just a little bit of smoothing. One smoothes out almost all the fast blinks and makes intensity changing very slow")]
        [Range(0, 1)] public float Smoothing = 0.25f;
        [Space]
        [Tooltip("Auto uses Theme Colors 0, 1, 2, 3 for Bass, LowMid, HighMid, Treble. Override Color allows you to set the static color value")]
        public AudioLinkColor ColorMode = AudioLinkColor.Auto;
        [Tooltip("Color that will be used when Override Color is enabled")]
        [ColorUsage(showAlpha: false)] public Color Color = Color.white;
        [Space]
        [Tooltip("List of the Light Volumes that should be affected by AudioLink")]
        public LightVolumeInstance[] TargetLightVolumes;
        [Tooltip("List of the Point Light Volumes that should be affected by AudioLink")]
        public PointLightVolumeInstance[] TargetPointLightVolumes;
        [Tooltip("List of the Mesh Renderers that has materials that should change color based on AudioLink")]
        public Renderer[] TargetMeshRenderers;
        [Tooltip("Brightness multiplier of the materials that should change color based on AudioLink. Intensity for Light Volumes and Point Light Volumes should be setup in their components")]
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
            if (AudioLink != null) {
                AudioLink.EnableReadback();
            }
        }

        private void Update() {

            int band = (int)AudioBand;
            Color color;

            if (ColorMode == AudioLinkColor.OverrideColor) {
                color = Vector4.Scale(AudioLink.GetDataAtPixel(Delay, band), Color);
            } else if(ColorMode == AudioLinkColor.Auto) {
                color = Vector4.Scale(AudioLink.GetDataAtPixel(Delay, band), AudioLink.GetDataAtPixel(band, 23));
            } else {
                color = Vector4.Scale(AudioLink.GetDataAtPixel(Delay, band), AudioLink.GetDataAtPixel((int)ColorMode, 23));
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
                TargetPointLightVolumes[i].IsRangeDirty = true;
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

        private void OnValidate() {
            if (AudioLink != null) {
                AudioLink.EnableReadback();
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

    public enum AudioLinkColor {
        Auto = -1,
        ThemeColor0 = 0,
        ThemeColor1 = 1,
        ThemeColor2 = 2,
        ThemeColor3 = 3,
        OverrideColor = 4
    }

}