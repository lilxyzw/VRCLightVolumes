using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

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
            true, // displayAddButton
            true  // displayRemoveButton
        );

        // Drawing header
        reorderableList.drawHeaderCallback = (Rect rect) => {

            float totalWidth = rect.width;
            float availableWidth = totalWidth - 15f - 4f;
            float weightWidth = 60f;
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

            EditorGUI.BeginChangeCheck();
            int newSize = Mathf.Min(EditorGUI.DelayedIntField(fieldRect, reorderableList.serializedProperty.arraySize, headerCountStyle), 256);
            if (EditorGUI.EndChangeCheck()) {
                newSize = Mathf.Max(0, newSize);
                reorderableList.serializedProperty.arraySize = newSize;
                reorderableList.index = Mathf.Clamp(reorderableList.index, 0, newSize - 1);
            }
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
            float weightWidth = 60f; // Weight width
            float space = 5f;        // Spacing

            Rect volumeRect = new Rect(rect.x, rect.y, totalWidth - weightWidth - space, EditorGUIUtility.singleLineHeight);
            Rect weightRect = new Rect(rect.x + totalWidth - weightWidth, rect.y, weightWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(volumeRect, volumeElement, GUIContent.none);
            EditorGUI.PropertyField(weightRect, weightElement, GUIContent.none);
        };

        // On Adding element
        reorderableList.onAddCallback = (ReorderableList list) => {
            int index = list.serializedProperty.arraySize;
            if (index == 256) return;
            list.serializedProperty.arraySize++;
            weightsProp.arraySize = list.serializedProperty.arraySize;
            list.serializedProperty.GetArrayElementAtIndex(index).objectReferenceValue = null;
            weightsProp.GetArrayElementAtIndex(index).floatValue = 0;
            list.index = index;
            _lightVolumeSetup.SyncUdonScript();
        };

        // On Removing element
        reorderableList.onRemoveCallback = (ReorderableList list) => {
            int indexToRemove = list.index;
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            if (indexToRemove >= 0 && indexToRemove < weightsProp.arraySize) {
                weightsProp.DeleteArrayElementAtIndex(indexToRemove);
            } else if (weightsProp.arraySize > volumesProp.arraySize) {
                weightsProp.DeleteArrayElementAtIndex(weightsProp.arraySize - 1);
            }
            if (list.index >= list.serializedProperty.arraySize - 1) {
                list.index = list.serializedProperty.arraySize - 1;
            }
            _lightVolumeSetup.SyncUdonScript();
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

        // On Drag and Drop in element
        reorderableList.drawElementBackgroundCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            EventType eventType = Event.current.type;

            if (eventType == EventType.Repaint) {
                ReorderableList.defaultBehaviours.DrawElementBackground(rect, index, isActive, isFocused, true);
            }

            if (eventType == EventType.DragUpdated || eventType == EventType.DragPerform) {
                if (!rect.Contains(Event.current.mousePosition)) return;
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (eventType == EventType.DragPerform) {
                    DragAndDrop.AcceptDrag();
                    if (DragAndDrop.objectReferences.Length > 0) {
                        GameObject go = (GameObject)DragAndDrop.objectReferences[0];
                        if(go.TryGetComponent(out LightVolume volume)) {
                            if(index == -1) {
                                for (int i = 0; i < DragAndDrop.objectReferences.Length; i++) {
                                    GameObject obj = (GameObject)DragAndDrop.objectReferences[i];
                                    if (obj.TryGetComponent(out LightVolume v)) {
                                        int newIndex = volumesProp.arraySize;
                                        if (newIndex == 256) break;
                                        volumesProp.arraySize++;
                                        weightsProp.arraySize = volumesProp.arraySize;
                                        volumesProp.GetArrayElementAtIndex(newIndex).objectReferenceValue = v;
                                        weightsProp.GetArrayElementAtIndex(newIndex).floatValue = 0;
                                    }
                                }
                            } else {
                                volumesProp.GetArrayElementAtIndex(index).objectReferenceValue = volume;
                            }
                        }
                    }
                    Event.current.Use();
                }
            }
            _lightVolumeSetup.SyncUdonScript();
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

        reorderableList.DoLayoutList();

        List<string> hiddenFields = new List<string>() { "m_Script", "LightVolumes", "LightVolumesWeights", "LightVolumeAtlas", "LightVolumeDataList", "LightVolumeManager", "_bakingModePrev" };
        if (_lightVolumeSetup.BakingMode != LightVolumeSetup.Baking.Bakery) {
            hiddenFields.Add("FixLightProbesL1");
        }

        DrawPropertiesExcluding(serializedObject, hiddenFields.ToArray());

        GUILayout.Space(5);

        if (GUILayout.Button("Pack Light Volumes")){
            _lightVolumeSetup.GenerateAtlas();
        }

        serializedObject.ApplyModifiedProperties();

    }

}