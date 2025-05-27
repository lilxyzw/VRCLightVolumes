using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRCLightVolumes {

    [ExecuteAlways]
    public class PointLightVolume : MonoBehaviour {
        
        public LightType Type = LightType.PointLight;
        [Min(0.0001f)] public float Range = 5f;
        [ColorUsage(showAlpha: false)] public Color Color = Color.white;
        public float Intensity = 1f;
        public LightShape Shape = LightShape.Parametric;
        [Range(0.1f, 360)] public float Angle = 60f;
        [Range(0.001f, 1)] public float Falloff = 0f;
        public Texture2D FalloffLUT;

        public int FalloffLUT_ID = -1;

        public PointLightVolumeInstance PointLightVolumeInstance;
        public LightVolumeSetup LightVolumeSetup;

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

        private void SyncUdonScript() {
            SetupDependencies();
            PointLightVolumeInstance.Color = Color;
            PointLightVolumeInstance.Intensity = Intensity;
            PointLightVolumeInstance.SetRange(Range);

            if(Type == LightType.PointLight) { // Point light
                PointLightVolumeInstance.SetAngle(0); // Use it as Point Light
                if (Shape == LightShape.FalloffLUT && FalloffLUT != null) PointLightVolumeInstance.SetFalloffLUT(FalloffLUT_ID); // Use LUT
                else PointLightVolumeInstance.SetFalloffLUT(-1); // Don't use LUT
            } else { // Spot Light
                PointLightVolumeInstance.SetAngleFalloff(Angle, Falloff); // Don't use LUT
                if (Shape == LightShape.FalloffLUT && FalloffLUT != null) PointLightVolumeInstance.SetFalloffLUT(FalloffLUT_ID); // Use LUT
            }

            LVUtils.MarkDirty(PointLightVolumeInstance);
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
            FalloffLUT
        }

        public enum LightType {
            PointLight,
            SpotLight
        }

    }

}