using UnityEngine;

#if UDONSHARP
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
        public LightVolumeInstance[] TargetLightVolumes;

        private Color32[] _pixels;


#if UDONSHARP
        void Update() {
            VRCAsyncGPUReadback.Request(TargetRenderTexture, TargetRenderTexture.mipmapCount - 1, (IUdonEventReceiver)this);
        }

        public override void OnAsyncGpuReadbackComplete(VRCAsyncGPUReadbackRequest request) {
            _pixels = new Color32[1];
            if (request.TryGetData(_pixels)) {
                for (int i = 0; i < TargetLightVolumes.Length; i++) {
                    TargetLightVolumes[i].Color = _pixels[0];
                }
            }
        }

#else
        void Update() {
            UnityEngine.Rendering.AsyncGPUReadback.Request(TargetRenderTexture, TargetRenderTexture.mipmapCount - 1, OnAsyncGpuReadbackComplete);
        }

        public void OnAsyncGpuReadbackComplete(UnityEngine.Rendering.AsyncGPUReadbackRequest request) {
            var data = request.GetData<Color32>();
            for (int i = 0; i < TargetLightVolumes.Length; i++) {
                TargetLightVolumes[i].Color = _pixels[0];
            }
            data.Dispose();
        }
#endif

    }
}