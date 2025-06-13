using UnityEngine;

namespace VRCLightVolumes {

    [ExecuteAlways]
    public class PointLightVolume : MonoBehaviour {

        [Tooltip("Defines whether this point light volume can be moved in runtime. Disabling this option slightly improves performance.")]
        public bool Dynamic = false;
        [Tooltip("Enables baked shadows for this light. This setting is only available for static lights, which cannot move. You must re-bake your volumes after changing this setting. This incurs some runtime VRAM and performance overhead.")]
        public bool BakedShadows = false;
        [Tooltip("Point light is the most performant type. Area light is the heaviest and best suited for dynamic, movable sources. For static lighting, it's recommended to bake regular additive light volumes instead.")]
        public LightType Type = LightType.PointLight;
        [Tooltip("Radius in meters beyond which point and spot lights are culled. Fewer overlapping lights result in better performance.")]
        [Min(0.0001f)] public float Range = 5f;
        [Tooltip("Multiplies the point light volume�s color by this value.")]
        [ColorUsage(showAlpha: false)] public Color Color = Color.white;
        [Tooltip("Brightness of the point light volume.")]
        public float Intensity = 1f;
        [Tooltip("Parametric uses settings to compute light falloff. LUT uses a texture: X - cone falloff, Y - attenuation (Y only for point lights). Cookie projects a texture for spot lights. Cubemap projects a cubemap for point lights.")]
        public LightShape Shape = LightShape.Parametric;
        [Tooltip("Angle of a spotlight cone in degrees.")]
        [Range(0.1f, 360)] public float Angle = 60f;
        [Tooltip("Cone falloff.")]
        [Range(0.001f, 1)] public float Falloff = 1f;
        [Tooltip("X - cone falloff, Y - attenuation. No compression and RGBA Float or RGBA Half format is recommended.")]
        public Texture2D FalloffLUT = null;
        [Tooltip("Projects a square texture for spot lights.")]
        public Texture2D Cookie = null;
        [Tooltip("Projects a cubemap for point lights.")]
        public Cubemap Cubemap = null;
        [Tooltip("Shows overdrawing range gizmo. Less point light volumes intersections - more performance!")]
        public bool DebugRange = false;

        public int CustomID = -1;

        public PointLightVolumeInstance PointLightVolumeInstance;
        public LightVolumeSetup LightVolumeSetup;

        private Texture2D _falloffLUTPrev = null;
        private Texture2D _cookiePrev = null;
        private Cubemap _cubemapPrev = null;
        private LightShape _shapePrev = LightShape.Parametric;
        private LightType _typePrev = LightType.PointLight;

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

        // Returns currently used custom texture depending on the light parameters
        public Texture GetCustomTexture() {
            if (Shape == LightShape.Parametric || Type == LightType.AreaLight) {
                return null;
            } else if (Type == LightType.PointLight) {
                if(Shape == LightShape.LUT) {
                    return FalloffLUT;
                } else if (Shape == LightShape.Custom) {
                    return Cubemap;
                }
            } else if (Type == LightType.SpotLight) {
                if (Shape == LightShape.LUT) {
                    return FalloffLUT;
                } else if (Shape == LightShape.Custom) {
                    return Cookie;
                }
            }
            return null;
        }

        private void Update() {
            SetupDependencies();
#if UNITY_EDITOR
            if (_falloffLUTPrev != FalloffLUT || _cookiePrev != Cookie || _cubemapPrev != Cubemap || _shapePrev != Shape || _typePrev != Type) {
                _falloffLUTPrev = FalloffLUT;
                _cookiePrev = Cookie;
                _cubemapPrev = Cubemap;
                _shapePrev = Shape;
                _typePrev = Type;
                LightVolumeSetup.GenerateCustomTexturesArray();
            }
#endif
        }

        public void SyncUdonScript() {
            SetupDependencies();
            PointLightVolumeInstance.IsDynamic = Dynamic;
            PointLightVolumeInstance.SetColor(Color, Intensity);

            if(Type == LightType.PointLight) { // Point light
                PointLightVolumeInstance.SetRange(Range);
                if (Shape == LightShape.Custom && Cubemap != null) {
                    PointLightVolumeInstance.SetCustomTexture(CustomID); // Use Custom Cubemap Texture
                } else if (Shape == LightShape.LUT && FalloffLUT != null) {
                    PointLightVolumeInstance.SetLut(CustomID); // Use LUT
                } else {
                    PointLightVolumeInstance.SetParametric(); // Use this light in parametric mode
                }
                PointLightVolumeInstance.SetPointLight(); // Use it as Point Light
            } else if (Type == LightType.SpotLight) { // Spot Light
                PointLightVolumeInstance.SetRange(Range);
                if (Shape == LightShape.Custom && Cookie != null) {
                    PointLightVolumeInstance.SetCustomTexture(CustomID); // Use Cookie Texture
                } else if (Shape == LightShape.LUT && FalloffLUT != null) {
                    PointLightVolumeInstance.SetLut(CustomID); // Use LUT
                } else {
                    PointLightVolumeInstance.SetParametric(); // Use this light in parametric mode
                }
                PointLightVolumeInstance.SetSpotLight(Angle, Falloff); // Don't use custom tex
            } else if (Type == LightType.AreaLight) { // Area light
                PointLightVolumeInstance.SetAreaLight();
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
                FalloffLUT = null;
                Cookie = null;
                Cubemap = null;
                LightVolumeSetup.GenerateCustomTexturesArray();
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
            LUT,
            Custom
        }

        public enum LightType {
            PointLight,
            SpotLight,
            AreaLight,
        }

    }

}