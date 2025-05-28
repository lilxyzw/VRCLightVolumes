using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRCLightVolumes {

    [ExecuteAlways]
    public class PointLightVolume : MonoBehaviour {
        
        public bool Dynamic = false;
        public LightType Type = LightType.PointLight;
        [Min(0.0001f)] public float Range = 5f;
        [ColorUsage(showAlpha: false)] public Color Color = Color.white;
        public float Intensity = 1f;
        public LightShape Shape = LightShape.Parametric;
        [Range(0.1f, 360)] public float Angle = 60f;
        [Range(0.001f, 1)] public float Falloff = 1f;
        public Texture2D FalloffLUT = null;
        public Cubemap Cubemap = null;

        public int CustomID = -1;

        public PointLightVolumeInstance PointLightVolumeInstance;
        public LightVolumeSetup LightVolumeSetup;

        private Texture2D _falloffLUTPrev = null;
        private Cubemap _cubemapPrev = null;
        private LightShape _shapePrev = LightShape.Parametric;

        // Looks for LightVolumeSetup and LightVolumeInstance udon script and setups them if needed
        public void SetupDependencies() {
            if (PointLightVolumeInstance == null && !TryGetComponent(out PointLightVolumeInstance)) {
                PointLightVolumeInstance = gameObject.AddComponent<PointLightVolumeInstance>();
            }
            if (LightVolumeSetup == null) {
                LightVolumeSetup = FindObjectOfType<LightVolumeSetup>();
                if (LightVolumeSetup == null) {
                    var go = new GameObject("Light Volume Manager");
                    LightVolumeSetup = go.AddComponent<LightVolumeSetup>();
                    LightVolumeSetup.SyncUdonScript();
                }
            }
        }

        private void Update() {
            SetupDependencies();
            if (_falloffLUTPrev != FalloffLUT) {
                _falloffLUTPrev = FalloffLUT;
                LightVolumeSetup.GenerateLUTArray();
            }
            if (_cubemapPrev != Cubemap) {
                _cubemapPrev = Cubemap;
                LightVolumeSetup.GenerateCubemapArray();
            }
            if(_shapePrev != Shape) {
                _shapePrev = Shape;
                LightVolumeSetup.GenerateLUTArray();
                LightVolumeSetup.GenerateCubemapArray();
            }
        }

        public void SyncUdonScript() {
            SetupDependencies();
            PointLightVolumeInstance.IsDynamic = Dynamic;
            PointLightVolumeInstance.Color = Color;
            PointLightVolumeInstance.Intensity = Intensity;
            PointLightVolumeInstance.SetRange(Range);

            if(Type == LightType.PointLight) { // Point light
                PointLightVolumeInstance.SetAngle(0); // Use it as Point Light
                if (Shape == LightShape.Custom && Cubemap != null) PointLightVolumeInstance.SetCustomID(CustomID); // Use LUT
                else PointLightVolumeInstance.SetCustomID(-1); // Don't use custom tex
            } else { // Spot Light
                PointLightVolumeInstance.SetAngleFalloff(Angle, Falloff); // Don't use custom tex
                if (Shape == LightShape.Custom && FalloffLUT != null) PointLightVolumeInstance.SetCustomID(CustomID); // Use LUT
            }

            LVUtils.MarkDirty(PointLightVolumeInstance);
        }

        private void Reset() {
            SetupDependencies();
            SyncUdonScript();
            LightVolumeSetup.RefreshVolumesList();
            LightVolumeSetup.SyncUdonScript();
        }

        private void OnEnable() {
            SetupDependencies();
            LightVolumeSetup.RefreshVolumesList();
            LightVolumeSetup.SyncUdonScript();
        }

        private void OnDisable() {
            if (LightVolumeSetup != null) {
                LightVolumeSetup.RefreshVolumesList();
                LightVolumeSetup.SyncUdonScript();
            }
        }

        private void OnDestroy() {
            if (LightVolumeSetup != null) {
                LightVolumeSetup.RefreshVolumesList();
                LightVolumeSetup.SyncUdonScript();
            }
        }

        private void OnValidate() {
            SyncUdonScript();
        }

        // Delete self in play mode
        private void Start() {
            if (Application.isPlaying) Destroy(this);
        }

        public enum LightShape {
            Parametric,
            Custom
        }

        public enum LightType {
            PointLight,
            SpotLight
        }

    }

}