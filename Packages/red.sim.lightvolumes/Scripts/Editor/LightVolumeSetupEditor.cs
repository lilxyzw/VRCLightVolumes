using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

namespace VRCLightVolumes {
    [CustomEditor(typeof(LightVolumeSetup))]
    public class LightVolumeSetupEditor : Editor {

        private SerializedProperty volumesProp;
        private SerializedProperty weightsProp;
        private ReorderableList reorderableList;

        private LightVolumeSetup _lightVolumeSetup;

        private bool _isMultipleInstancesError = false;

        private void OnEnable() {

            int managersCount = FindObjectsByType<LightVolumeManager>(FindObjectsSortMode.None).Length;
            _isMultipleInstancesError = managersCount > 1;

            _lightVolumeSetup = (LightVolumeSetup)target;

            volumesProp = serializedObject.FindProperty("LightVolumes");
            weightsProp = serializedObject.FindProperty("LightVolumesWeights");

            reorderableList = new ReorderableList(
                serializedObject,
                volumesProp,
                true, // draggable
                true, // displayHeader
                false, // displayAddButton
                false  // displayRemoveButton
            );

            // Drawing header
            reorderableList.drawHeaderCallback = (Rect rect) => {

                float totalWidth = rect.width;
                float availableWidth = totalWidth - 15f - 4f;
                float weightWidth = 42f;
                float space = 5f;
                float volumeWidth = availableWidth - weightWidth - space;
                float xOffset = rect.x + 15f;

                var headerCountStyle = new GUIStyle(EditorStyles.numberField) {
                    alignment = TextAnchor.MiddleCenter,
                    contentOffset = Vector2.zero,
                    fixedHeight = EditorGUIUtility.singleLineHeight - 3,
                    margin = new RectOffset(0, 0, 0, 0),
                    padding = new RectOffset(0, 0, 0, 0),
                    fontSize = 11
                };
                headerCountStyle.normal.textColor = EditorStyles.label.normal.textColor;

                Rect volumeHeaderRect = new Rect(xOffset, rect.y, volumeWidth, EditorGUIUtility.singleLineHeight);
                Rect weightHeaderRect = new Rect(xOffset + volumeWidth + space, rect.y, weightWidth, EditorGUIUtility.singleLineHeight);
                Rect fieldRect = new Rect(xOffset + 96, rect.y + 2, 32, rect.height);

                EditorGUI.LabelField(volumeHeaderRect, "Light Volumes");
                EditorGUI.LabelField(weightHeaderRect, "Weight");

                EventType eventType = Event.current.type;
                if (eventType == EventType.DragUpdated || eventType == EventType.DragPerform) {
                    if (!rect.Contains(Event.current.mousePosition)) return;
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (eventType == EventType.DragPerform) {
                        DragAndDrop.AcceptDrag();
                        for (int i = 0; i < DragAndDrop.objectReferences.Length; i++) {
                            GameObject go = (GameObject)DragAndDrop.objectReferences[i];
                            if (go.TryGetComponent(out LightVolume volume)) {
                                int newIndex = volumesProp.arraySize;
                                if (newIndex == 256) break;
                                volumesProp.arraySize++;
                                weightsProp.arraySize = volumesProp.arraySize;
                                volumesProp.GetArrayElementAtIndex(newIndex).objectReferenceValue = volume;
                                weightsProp.GetArrayElementAtIndex(newIndex).floatValue = 0;
                            }
                        }
                        Event.current.Use();
                    }
                }

                _lightVolumeSetup.SyncUdonScript();

            };

            // Drawing each element
            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {

                if (index < 0 || index >= volumesProp.arraySize || index >= weightsProp.arraySize) return;

                SerializedProperty volumeElement = volumesProp.GetArrayElementAtIndex(index);
                SerializedProperty weightElement = weightsProp.GetArrayElementAtIndex(index);

                rect.y += 2; // Top padding
                float totalWidth = rect.width;
                float weightWidth = 45f; // Weight width
                float space = 5f;        // Spacing

                Rect iconRect = new Rect(rect.x, rect.y, totalWidth - weightWidth - space, EditorGUIUtility.singleLineHeight);
                Rect volumeRect = new Rect(rect.x + 24, rect.y, totalWidth - weightWidth - space, EditorGUIUtility.singleLineHeight);
                Rect weightRect = new Rect(rect.x + totalWidth - weightWidth, rect.y, weightWidth, EditorGUIUtility.singleLineHeight);

                if (volumeElement.objectReferenceValue != null && volumeElement.objectReferenceValue.GetType() == typeof(LightVolume)) {
                    var volume = (LightVolume)volumeElement.objectReferenceValue;
                    GUIContent icon = volume.Additive ? EditorGUIUtility.IconContent("d_Spotlight Icon") : EditorGUIUtility.IconContent("d_PreMatLight1@2x");
                    icon.tooltip = volume.Additive ? "Additive Volume" : "Regular Volume";
                    EditorGUI.LabelField(iconRect, icon);
                }

                EditorGUI.LabelField(volumeRect, volumeElement.objectReferenceValue != null ? volumeElement.objectReferenceValue.name : "None");
                EditorGUI.PropertyField(weightRect, weightElement, GUIContent.none);

            };

            // On Moving element around
            reorderableList.onReorderCallbackWithDetails = (ReorderableList list, int oldIndex, int newIndex) => {
                if (oldIndex >= 0 && oldIndex < weightsProp.arraySize && newIndex >= 0 && newIndex < weightsProp.arraySize) {
                    weightsProp.MoveArrayElement(oldIndex, newIndex);
                } else if (weightsProp.arraySize != volumesProp.arraySize) {
                    // In case of a sync error
                    weightsProp.arraySize = volumesProp.arraySize;
                    EditorUtility.SetDirty(target);
                }
                _lightVolumeSetup.SyncUdonScript();
            };

            // On Click on element
            reorderableList.onSelectCallback = (ReorderableList list) => {
                SerializedProperty volumeElement = volumesProp.GetArrayElementAtIndex(list.index);
                LightVolume volume = volumeElement.objectReferenceValue as LightVolume;
                if (volume != null) EditorGUIUtility.PingObject(volume.gameObject);
            };

        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            if (volumesProp.arraySize != weightsProp.arraySize) {
                weightsProp.arraySize = volumesProp.arraySize;
                for (int i = 0; i < volumesProp.arraySize; i++) {
                    if (i >= weightsProp.arraySize - (volumesProp.arraySize - weightsProp.arraySize)) {
                        SerializedProperty weightElement = weightsProp.GetArrayElementAtIndex(i);
                        weightElement.floatValue = i;
                    }
                }
                EditorUtility.SetDirty(target);
            }

            GUILayout.Space(10);

            if (LVUtils.IsInPrefabAsset(_lightVolumeSetup)) {
                EditorGUILayout.HelpBox("This component is part of a prefab asset (not in the scene). Please, use one that is placed on your scene!", MessageType.Warning);
                GUILayout.Space(10);
            } else if (_isMultipleInstancesError) {
                EditorGUILayout.HelpBox("Multiple Light Volume Managers detected in the scene. Please ensure only one is active to avoid unexpected behavior!", MessageType.Error);
                GUILayout.Space(10);
            }

            int vCount = 0;
            if (_lightVolumeSetup.LightVolumeManager != null && _lightVolumeSetup.LightVolumeManager.LightVolumeAtlas != null) {
                var tex = _lightVolumeSetup.LightVolumeManager.LightVolumeAtlas;
                vCount = tex.width * tex.height * tex.depth;
            }

            GUILayout.Label($"Atlas size in VRAM: {SizeInVRAM(vCount)} MB");
            GUILayout.Label($"Atlas size in bundle: {SizeInBundle(vCount)} MB (Approximately)");

            GUILayout.Space(10);

            reorderableList.DoLayoutList();

            GUILayout.Space(-15);

            List<string> hiddenFields = new List<string>() { "m_Script", "LightVolumes", "LightVolumesWeights", "LightVolumeAtlas", "LightVolumeDataList", "LightVolumeManager", "_bakingModePrev" };
            if (_lightVolumeSetup.BakingMode != LightVolumeSetup.Baking.Bakery) {
                hiddenFields.Add("FixLightProbesL1");
            }

            DrawPropertiesExcluding(serializedObject, hiddenFields.ToArray());

            GUILayout.Space(5);

            if (GUILayout.Button("Pack Light Volumes")) {
                _lightVolumeSetup.GenerateAtlas();
            }

            serializedObject.ApplyModifiedProperties();

        }

        // Real size in VRAM
        string SizeInVRAM(int vCount) {
            float mb = vCount * 8 / (float)(1024 * 1024);
            return mb.ToString("0.00");
        }

        // Approximate size in Asset bundle
        string SizeInBundle(int vCount) {
            float mb = vCount * 8 * 0.315f / (float)(1024 * 1024);
            return mb.ToString("0.00");
        }

    }
}