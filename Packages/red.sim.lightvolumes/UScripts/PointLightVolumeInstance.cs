
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
        public float Range = 5f;
        [ColorUsage(showAlpha: false)] public Color Color;
        public float Intensity = 1f;
        public float Angle = 1f;
        public float Falloff = 1f;
        public int FalloffLUT = -1;

        // Sets range data which is actually an inverted squared range
        public void SetRange(float range) {
            Range = 1 / (range * range);
        }

        // Sets angle data which is actually a Cos(angleRad / 2) 
        public void SetAngle(float angleDeg) {
            Angle = Mathf.Cos(angleDeg * Mathf.Deg2Rad * 0.5f);
        }

        // Sets LUT ID instead of falloff which is actually a negative ID value
        public void SetFalloffLUT(int falloffLUT) {
            Falloff = -falloffLUT;
        }

        // Sets both angle and falloff because angle required to determine falloff anyway
        public void SetAngleFalloff(float angleDeg, float falloff) {
            float outerAngle = angleDeg * Mathf.Deg2Rad * 0.5f;
            Angle = Mathf.Cos(outerAngle);
            Falloff = 1 / (Mathf.Cos(outerAngle * (1.0f - Mathf.Clamp01(falloff))) - Angle);
        }

    }

}