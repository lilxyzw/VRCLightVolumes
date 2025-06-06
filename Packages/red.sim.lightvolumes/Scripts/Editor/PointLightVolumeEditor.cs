using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace VRCLightVolumes {

    [CanEditMultipleObjects]
    [CustomEditor(typeof(PointLightVolume))]
    public class PointLightVolumeEditor : Editor {

        PointLightVolume PointLightVolume;

        const int TEX_SIZE = 128;
        Texture2D lutTexture;

        private void OnEnable() {
            PointLightVolume = (PointLightVolume)target;
        }

        public override void OnInspectorGUI() {

            serializedObject.Update();

            List<string> hiddenFields = new List<string> { "m_Script", "CustomID", "PointLightVolumeInstance", "LightVolumeSetup" };

            if(PointLightVolume.Type == PointLightVolume.LightType.PointLight) {
                hiddenFields.Add("Angle");
                hiddenFields.Add("Falloff");
            }

            if (PointLightVolume.Shape == PointLightVolume.LightShape.Parametric) {
                hiddenFields.Add("FalloffLUT");
                hiddenFields.Add("Cubemap");
                hiddenFields.Add("Cookie");
            } else if (PointLightVolume.Shape == PointLightVolume.LightShape.Custom) {
                hiddenFields.Add("Falloff");
                if(PointLightVolume.Type == PointLightVolume.LightType.PointLight) {
                    hiddenFields.Add("FalloffLUT");
                    hiddenFields.Add("Cookie");
                } else if (PointLightVolume.Type == PointLightVolume.LightType.SpotLight) {
                    hiddenFields.Add("FalloffLUT");
                    hiddenFields.Add("Cubemap");
                }
            } else if (PointLightVolume.Shape == PointLightVolume.LightShape.LUT) {
                hiddenFields.Add("Falloff");
                hiddenFields.Add("Cubemap");
                hiddenFields.Add("Cookie");
            }

            DrawPropertiesExcluding(serializedObject, hiddenFields.ToArray());

            serializedObject.ApplyModifiedProperties();

        }

        private void DrawVolumeGUI(PointLightVolume pointLightVolume) {

            Transform t = pointLightVolume.transform;
            Vector3 origin = t.position;
            float range = pointLightVolume.Range;

            if (pointLightVolume.Type == PointLightVolume.LightType.PointLight) { // Point Light Visualization

                // Drawing

                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                Handles.color = new Color(1f, 1f, 0f, 0.6f);
                DrawPointLight(origin, range);

                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                Handles.color = new Color(1f, 1f, 0f, 0.15f);
                DrawPointLight(origin, range);

            } else { // Spot Light Visualization

                // Calculating

                Vector3 forward = t.forward;
                Vector3 right = t.right;
                Vector3 up = t.up;

                float spotAngle = Mathf.Clamp(pointLightVolume.Angle, 0f, 360f);
                float halfAngleRad = spotAngle * 0.5f * Mathf.Deg2Rad;
                float radius = Mathf.Abs(range) * Mathf.Sin(halfAngleRad);
                float centerOffset = range * Mathf.Cos(halfAngleRad);
                Vector3 diskCenter = origin + forward * centerOffset;
                Vector3[] dirs = new Vector3[] { right, -right, up, -up };

                // Drawing

                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                Handles.color = new Color(1f, 1f, 0f, 0.6f);
                DrawSpotLight(origin, diskCenter, forward, radius, dirs);

                Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
                Handles.color = new Color(1f, 1f, 0f, 0.15f);
                DrawSpotLight(origin, diskCenter, forward, radius, dirs);

            }

        }

        void OnSceneGUI() {
            foreach (var obj in Selection.gameObjects) {
                var volume = obj.GetComponent<PointLightVolume>();
                if (volume != null) {
                    DrawVolumeGUI(volume);
                }
            }
        }

        // Draws a spotlight visualization using precalculated values
        private void DrawSpotLight(Vector3 origin, Vector3 diskCenter, Vector3 forward, float radius, Vector3[] dirs) {
            Handles.DrawWireDisc(diskCenter, forward, radius);
            foreach (var dir in dirs) {
                Vector3 edge = diskCenter + dir * radius;
                Handles.DrawLine(origin, edge);
            }
        }

        // Draws a pointlight visualization
        private void DrawPointLight(Vector3 center, float radius) {
            Handles.DrawWireArc(center, Vector3.right, Vector3.up, 360, radius);
            Handles.DrawWireArc(center, Vector3.up, Vector3.forward, 360, radius);
            Handles.DrawWireArc(center, Vector3.forward, Vector3.right, 360, radius);
        }

    }
}