
using UnityEngine;

#if UDONSHARP
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;
#endif

namespace VRCLightVolumes {
#if UDONSHARP
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PointLightVolumeInstance : UdonSharpBehaviour
#else
    public class PointLightVolumeInstance : MonoBehaviour
#endif
    {
        [Tooltip("Point light volume color")]
        [ColorUsage(showAlpha: false)] public Color Color;
        [Tooltip("Color multiplies by this value.")]
        public float Intensity = 1;
        [Tooltip("Defines whether this point light volume can be moved in runtime. Disabling this option slightly improves performance.")]
        public bool IsDynamic = false;
        [Tooltip("For point light: XYZ = Position, W = Inverse squared range.\nFor spot light: XYZ = Position, W = Inverse squared range, negated.\nFor area light: XYZ = Position, W = Width.")]
        public Vector4 PositionData;
        [Tooltip("For point light: XYZW = Rotation quaternion.\nFor spot light: XYZ = Direction, W = Cone falloff.\nFor area light: XYZW = Rotation quaternion.")]
        public Vector4 DirectionData;
        [Tooltip("If parametric: Stores 0.\nIf uses custom LUT: Stores LUT ID with positive sign.\nIf uses custom texture: Stores texture ID with negative sign.")]
        public float CustomID;
        [Tooltip("Half-angle of the spotlight cone, in radians.")]
        public float Angle;
        [Tooltip("For point light: Cos of angle (for LUT).\nFor spot light: Cos of outer angle if no custom texture, tan of outer angle otherwise.\nFor area light: 2 + Height.")]
        public float AngleData;
        [Tooltip("Index of the shadowmask channel used by this light. -1 means no shadowmask.")]
        public sbyte ShadowmaskIndex = -1;

        // Checks if it's a spotlight
        public bool IsSpotLight() {
            return PositionData.w < 0;
        }
        
        // Checks if it's a point light
        public bool IsPointLight() {
            return PositionData.w >= 0 && AngleData <= 1.5;
        }

        // Checks if it's an area light
        public bool IsAreaLight() {
            return PositionData.w >= 0 && AngleData > 1.5;
        }

        // Checks if uses custom texture
        public bool IsCustomTexture() {
            return CustomID < 0;
        }

        // Checks if uses LUT
        public bool IsLut() {
            return CustomID > 0;
        }

        // Checks if uses Parametric mode
        public bool IsParametric() {
            return CustomID == 0;
        }

        // Sets range data which is actually an inverted squared range
        public void SetRange(float range) {
            PositionData.w = Mathf.Sign(PositionData.w) / (range * range); // Saving the sign that was here before
        }

        // Sets LUT ID
        public void SetLut(int id) {
            CustomID = id + 1;
            AngleData = Mathf.Cos(Angle);
        }

        // Sets Cubemap or a Cookie ID
        public void SetCustomTexture(int id) {
            CustomID = - id - 1;
            if(IsSpotLight()) { // If it's spotlight
                AngleData = Mathf.Tan(Angle);
            }
        }

        // Sets light into parametric mode
        public void SetParametric() {
            CustomID = 0;
            AngleData = Mathf.Cos(Angle);
        }

        // Sets light into the point light type
        public void SetPointLight() {
            PositionData.w = Mathf.Abs(PositionData.w);
        }

        // Sets light into the spot light type with both angle and falloff because angle required to determine falloff anyway
        public void SetSpotLight(float angleDeg, float falloff) {
            Angle = angleDeg * Mathf.Deg2Rad * 0.5f;
            if (IsCustomTexture()) {
                AngleData = Mathf.Tan(Angle); // Using Custom Tex
            } else {
                AngleData = Mathf.Cos(Angle);
                DirectionData.w = 1 / (Mathf.Cos(Angle * (1.0f - Mathf.Clamp01(falloff))) - AngleData);
            }
            PositionData.w = - Mathf.Abs(PositionData.w);
        }

        // Sets light into the spot light type with angle specified
        public void SetSpotLight(float angleDeg) {
            Angle = angleDeg * Mathf.Deg2Rad * 0.5f;
            if (IsCustomTexture()) {
                AngleData = Mathf.Tan(Angle); // Using Custom Tex
            } else {
                AngleData = Mathf.Cos(Angle);
            }
            PositionData.w = - Mathf.Abs(PositionData.w);
        }
        
        // Sets light into the area light type
        public void SetAreaLight() {
            PositionData.w = Mathf.Max(Mathf.Abs(transform.lossyScale.x), 0.001f);
            AngleData = 2 + Mathf.Max(Mathf.Abs(transform.lossyScale.y), 0.001f); // Add 2 to get out of [-1; 1] codomain of cosine
        }

        // Updates data required for shader
        public void UpdateTransform() {

            Vector3 pos = transform.position;
            PositionData = new Vector4(pos.x, pos.y, pos.z, PositionData.w);

            if (IsAreaLight()) {
                Quaternion rot = transform.rotation;
                DirectionData = new Vector4(rot.x, rot.y, rot.z, rot.w);
                SetAreaLight();
            } else if (IsSpotLight() && !IsCustomTexture()) { // If Spot Light with no cookie
                Vector3 dir = transform.forward;
                DirectionData = new Vector4(dir.x, dir.y, dir.z, DirectionData.w);
            } else if (!IsParametric()) { // If Point Light with a cubemap or a spot light with cookie
                Quaternion rot = Quaternion.Inverse(transform.rotation);
                DirectionData = new Vector4(rot.x, rot.y, rot.z, rot.w);
            }

        }

    }

}