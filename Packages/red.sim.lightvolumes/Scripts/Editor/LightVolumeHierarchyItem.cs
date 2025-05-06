using UnityEditor;
using UnityEngine;

namespace VRCLightVolumes {
    public static class HierarchyMenu {

        [MenuItem("GameObject/Light Volume", false, 9999)]
        private static void CreateCustomGo(MenuCommand cmd) {

            var go = new GameObject(GetUniqueName("Light Volume"));

            LightVolume volume = go.AddComponent<LightVolume>();

            GameObjectUtility.SetParentAndAlign(go, cmd.context as GameObject);

            volume.Reset();

            Undo.RegisterCreatedObjectUndo(go, $"Create new Light Volume");

            Selection.activeObject = go;

        }

        private static string GetUniqueName(string baseName) {
            if (GameObject.Find(baseName) == null)
                return baseName;

            int idx = 1;
            string candidate;
            do {
                candidate = $"{baseName} ({idx++})";
            } while (GameObject.Find(candidate) != null);

            return candidate;
        }

    }
}