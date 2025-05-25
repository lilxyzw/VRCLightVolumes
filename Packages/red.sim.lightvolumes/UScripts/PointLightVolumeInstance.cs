
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
        [Min(0.0001f)] public float Range = 5f;
        [ColorUsage(showAlpha: false)] public Color Color;
        public float Intensity = 1f;
        public float Angle = 1f;
        public float ConeFalloff = 1f;
        private void OnDrawGizmos() {
            Gizmos.color = new Color(1,1,0,0.25f);
            Gizmos.DrawWireSphere(transform.position, Range);
        }

    }

}