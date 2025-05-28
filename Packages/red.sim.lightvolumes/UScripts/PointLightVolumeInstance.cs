
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
        public bool IsDynamic = false;
        public float Range = 5f;
        [ColorUsage(showAlpha: false)] public Color Color;
        public float Intensity = 1f;
        public float Angle = 1f;
        public float Falloff = 1f;

        public Vector4 PositionData;
        public Vector4 ColorData;
        public Vector4 DirectionData;

        // Sets range data which is actually an inverted squared range
        public void SetRange(float range) {
            Range = 1 / (range * range);
        }

        // Sets angle data which is actually a Cos(angleRad / 2) 
        public void SetAngle(float angleDeg) {
            Angle = Mathf.Cos(angleDeg * Mathf.Deg2Rad * 0.5f);
        }

        // Sets LUT or Cubemap ID instead of falloff which is actually a negative ID value
        public void SetCustomID(int id) {
            Falloff = -id;
        }

        // Sets both angle and falloff because angle required to determine falloff anyway
        public void SetAngleFalloff(float angleDeg, float falloff) {
            float outerAngle = angleDeg * Mathf.Deg2Rad * 0.5f;
            Angle = Mathf.Cos(outerAngle);
            Falloff = 1 / (Mathf.Cos(outerAngle * (1.0f - Mathf.Clamp01(falloff))) - Angle);
        }

        // Updates data required for shader
        public void UpdateData() {

            PositionData = transform.position;
            PositionData.w = Range;

            ColorData = Color * Intensity;

            if (Angle < 1 || Falloff > 0) { // If Spot Light or a point light with no cubemap
                DirectionData = transform.forward;
                DirectionData.w = Falloff;
                ColorData.w = Angle;
            } else { // If Point Light
                Quaternion rot = Quaternion.Inverse(transform.rotation);
                // A hack to save memory!
                if (rot.w < 0) { // If w component is negative. Then negate the quaternion to make it possible to store w
                    DirectionData = new Vector4(-rot.x, -rot.y, -rot.z, Falloff); // Negate! It's the same rotation actually!
                    ColorData.w = -rot.w + 1; // Storing w component (+1) of quaternion in angle to save mamory
                } else { // If w component is positive
                    DirectionData = new Vector4(rot.x, rot.y, rot.z, Falloff);
                    ColorData.w = rot.w + 1; // Storing w component (+1) of quaternion in angle to save mamory
                }
            }

        }

    }

}