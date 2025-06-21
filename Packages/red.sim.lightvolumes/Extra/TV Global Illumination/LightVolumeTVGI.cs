using UnityEngine;

#if UDONSHARP
using VRC.SDKBase;
using UdonSharp;
using VRC.SDK3.Rendering;
using VRC.Udon.Common.Interfaces;
#endif


namespace VRCLightVolumes {
#if UDONSHARP
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LightVolumeTVGI : UdonSharpBehaviour
#else
    public class LightVolumeTVGI : MonoBehaviour
#endif
    {
        public Texture TargetRenderTexture;
        public bool AntiFlickering = true;
        [Space]
        public float LightVolumeIntensity = 1;
        public LightVolumeInstance[] TargetLightVolumes;
        [Space]
        public float PointLightVolumeIntensity = 1;
        public PointLightVolumeInstance[] TargetPointLightVolumes;
        
#if UDONSHARP
        private Color32[] _pixels;
#else
        private Unity.Collections.NativeArray<Color32> _pixels;
#endif
        private Color _prevColor;
        private float _timePrev;
        private RenderTexture _downsampledTex;

#if UDONSHARP

        private void Start() {
            _timePrev = Time.time;
            _prevColor = Color.black;
            _downsampledTex = new RenderTexture(64, 32, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            _downsampledTex.useMipMap = true;
            _downsampledTex.autoGenerateMips = true;
            _downsampledTex.Create();
        }

        void Update() {
            VRCGraphics.Blit(TargetRenderTexture, _downsampledTex);
            VRCAsyncGPUReadback.Request(_downsampledTex, _downsampledTex.mipmapCount - 1, (IUdonEventReceiver)this);
        }

        public override void OnAsyncGpuReadbackComplete(VRCAsyncGPUReadbackRequest request) {
            _pixels = new Color32[1];
            if (request.TryGetData(_pixels)) {
                SetColor();
            }
        }

#else
        void Update() {
            Graphics.Blit(TargetRenderTexture, _downsampledTex);
            UnityEngine.Rendering.AsyncGPUReadback.Request(_downsampledTex, _downsampledTex.mipmapCount - 1, OnAsyncGpuReadbackComplete);
        }

        public void OnAsyncGpuReadbackComplete(UnityEngine.Rendering.AsyncGPUReadbackRequest request) {
            var _pixels = request.GetData<Color32>();
            SetColor();
            _pixels.Dispose();
        }
#endif

        private void SetColor() {

            // Custom delta time for the async stuff 
            float dTime = Time.time - _timePrev;
            _timePrev = Time.time;

            Color color = _pixels[0]; // Current color

            if (AntiFlickering) {
                float diff = ColorDifference(color, _prevColor); // Difference between prev and current color
                float smoothing = dTime / Mathf.Lerp(0.25f, 1e-05f, Mathf.Pow(diff * 1.5f, 0.1f)); // Smoothing speed depends on the color difference
                _prevColor = Color.Lerp(_prevColor, color, smoothing); // Actually smoothing colors
            } else {
                _prevColor = color;
            }

            // Applying all colors
            for (int i = 0; i < TargetLightVolumes.Length; i++) {
                TargetLightVolumes[i].Color = _prevColor * LightVolumeIntensity;
            }

            for (int i = 0; i < TargetPointLightVolumes.Length; i++) {
                TargetPointLightVolumes[i].SetColor(_prevColor, PointLightVolumeIntensity);
            }

        }

        private float ColorDifference(Color colorA, Color colorB) {
            float rmean = (colorA.r + colorB.r) * 0.5f;
            float r = colorA.r - colorB.r;
            float g = colorA.g - colorB.g;
            float b = colorA.b - colorB.b;
            return Mathf.Sqrt((2f + rmean) * r * r + 4f * g * g + (3f - rmean) * b * b) / 3;
        }

    }
}