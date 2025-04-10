using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(LightVolumeSetup))]
public class LightVolumeSetupEditor : Editor {

    private SerializedProperty volumesProp;
    private SerializedProperty weightsProp;
    private ReorderableList reorderableList;

    private LightVolumeSetup _lightVolumeSetup;
    private LightVolumeManager _udonLightVolumeManager;

    private bool _isMultipleInstancesError = false;

    private void OnEnable() {

        int managersCount = FindObjectsByType<LightVolumeManager>(FindObjectsSortMode.None).Length;
        _isMultipleInstancesError = managersCount > 1;

        _lightVolumeSetup = (LightVolumeSetup)target;

        volumesProp = serializedObject.FindProperty("BakeryVolumes");
        weightsProp = serializedObject.FindProperty("BakeryVolumesWeights");

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

            Rect volumeHeaderRect = new Rect(xOffset, rect.y, volumeWidth, EditorGUIUtility.singleLineHeight);
            Rect weightHeaderRect = new Rect(xOffset + volumeWidth + space, rect.y, weightWidth, EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(volumeHeaderRect, "Bakery Volume");
            EditorGUI.LabelField(weightHeaderRect, "Weight");

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
            list.serializedProperty.arraySize++;
            weightsProp.arraySize = list.serializedProperty.arraySize;
            list.serializedProperty.GetArrayElementAtIndex(index).objectReferenceValue = null;
            weightsProp.GetArrayElementAtIndex(index).floatValue = 1.0f;
            list.index = index;
            SetupUdonBehaviour();
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
            SetupUdonBehaviour();
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
            SetupUdonBehaviour();
        };

        // On Drag and Drop
        reorderableList.drawElementBackgroundCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            EventType eventType = Event.current.type;
            if (eventType == EventType.DragUpdated || eventType == EventType.DragPerform) {
                if (!rect.Contains(Event.current.mousePosition))
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (eventType == EventType.DragPerform) {
                    DragAndDrop.AcceptDrag();

                    foreach (Object draggedObject in DragAndDrop.objectReferences) {
                        if (draggedObject is BakeryVolume volume) {
                            int newIndex = volumesProp.arraySize;
                            volumesProp.arraySize++;
                            weightsProp.arraySize = volumesProp.arraySize;
                            volumesProp.GetArrayElementAtIndex(newIndex).objectReferenceValue = volume;
                            weightsProp.GetArrayElementAtIndex(newIndex).floatValue = 1.0f;
                        }
                    }
                    Event.current.Use();
                }
            }
            SetupUdonBehaviour();
        };

    }

    private void OnValidate() {
        SetupUdonBehaviour();
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

        if (IsInPrefabAsset(_lightVolumeSetup)) {
            EditorGUILayout.HelpBox("This component is part of a prefab asset (not in the scene). Please, use one that is placed on your scene!", MessageType.Warning);
            GUILayout.Space(10);
        } else if (_isMultipleInstancesError) {
            EditorGUILayout.HelpBox("Multiple Light Volume Managers detected in the scene. Please ensure only one is active to avoid unexpected behavior!", MessageType.Error);
            GUILayout.Space(10);
        }

        reorderableList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();

        string[] hiddenFields = new string[] { "m_Script", "BakeryVolumes", "BakeryVolumesWeights" };
        DrawPropertiesExcluding(serializedObject, hiddenFields);

        if(GUILayout.Button("Pack Light Volumes")){
            GenerateAtlas();
        }
    }

    // Setups udon script
    private void SetupUdonBehaviour() {

        if (IsInPrefabAsset(_lightVolumeSetup)) return;
        if (_udonLightVolumeManager == null) _udonLightVolumeManager = _lightVolumeSetup.GetComponent<LightVolumeManager>();
        if (_udonLightVolumeManager == null) return;

        var bakeryVolumes = _lightVolumeSetup.BakeryVolumes;

        Vector3[] boundsWorldMin = new Vector3[bakeryVolumes.Length];
        Vector3[] boundsWorldMax = new Vector3[bakeryVolumes.Length];

        for (int i = 0; i < bakeryVolumes.Length; i++) {
            if (bakeryVolumes[i] == null) continue;
            boundsWorldMin[i] = bakeryVolumes[i].bounds.min;
            boundsWorldMax[i] = bakeryVolumes[i].bounds.max;
        }

        _udonLightVolumeManager.BoundsWorldMin = boundsWorldMin;
        _udonLightVolumeManager.BoundsWorldMax = boundsWorldMax;

        _udonLightVolumeManager.VolumesWeights = _lightVolumeSetup.BakeryVolumesWeights;

        _lightVolumeSetup.SetShaderVariables();

    }

    // Generates atlas and setups udon script
    private void GenerateAtlas() {

        if (IsInPrefabAsset(_lightVolumeSetup)) return;
        if (_udonLightVolumeManager == null) _udonLightVolumeManager = _lightVolumeSetup.GetComponent<LightVolumeManager>();
        if (_udonLightVolumeManager == null) {
            Debug.LogError("[LightVolumeAtlaser] Udon LightVolumeManager component must be setuped on this game object!");
            return;
        }

        var bakeryVolumes = _lightVolumeSetup.BakeryVolumes;

        if (bakeryVolumes.Length == 0) return;

        Texture3D[] textures = new Texture3D[bakeryVolumes.Length * 3];

        for (int i = 0; i < bakeryVolumes.Length; i++) {
            if (bakeryVolumes[i] == null) {
                Debug.LogError("[LightVolumeAtlaser] One of the bakery volumes is not setuped!");
                return;
            }
            if (bakeryVolumes[i].bakedTexture0 == null || bakeryVolumes[i].bakedTexture1 == null || bakeryVolumes[i].bakedTexture2 == null) {
                Debug.LogError("[LightVolumeAtlaser] One of the bakery volumes is not baked!");
                return;
            }
            textures[i * 3] = bakeryVolumes[i].bakedTexture0;
            textures[i * 3 + 1] = bakeryVolumes[i].bakedTexture1;
            textures[i * 3 + 2] = bakeryVolumes[i].bakedTexture2;
        }

        var atlas = Texture3DAtlasGenerator.CreateAtlasStochastic(textures, _lightVolumeSetup.StochasticIterations);

        _udonLightVolumeManager.LightVolume = atlas.Texture;
        _udonLightVolumeManager.BoundsUvwMin = atlas.BoundsUvwMin;
        _udonLightVolumeManager.BoundsUvwMax = atlas.BoundsUvwMax;

        SaveTexture3DAsAsset(atlas.Texture, "Assets/BakeryLightmaps/Atlas3D.asset");

        SetupUdonBehaviour();

    }

    public static bool SaveTexture3DAsAsset(Texture3D textureToSave, string assetPath) {

        if (textureToSave == null) {
            Debug.LogError("[LightVolumeAtlaser] Error saving Texture3D: texture is null");
            return false;
        }

        if (string.IsNullOrEmpty(assetPath)) {
            Debug.LogError("[LightVolumeAtlaser] Error saving Texture3D: Saving path is null");
            return false;
        }

        try {
            string directoryPath = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath)) {
                Directory.CreateDirectory(directoryPath);
                AssetDatabase.Refresh();
            }
        } catch (System.Exception e) {
            Debug.LogError($"[LightVolumeAtlaser] Error while creating folders '{assetPath}': {e.Message}");
            return false;
        }

        try {
            AssetDatabase.CreateAsset(textureToSave, assetPath);
            EditorUtility.SetDirty(textureToSave);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[LightVolumeAtlaser] 3D Atlas saved at path: '{assetPath}'");
            return true;
        } catch (System.Exception e) {
            Debug.LogError($"[LightVolumeAtlaser] Error saving 3D Atlas at path: '{assetPath}': {e.Message}");
            return false;
        }

    }

    // Check if it's previewed as a prefab, or it's a part of a scene
    bool IsInPrefabAsset(Object obj) {
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        var prefabType = PrefabUtility.GetPrefabAssetType(obj);
        var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(obj);

        return prefabStatus == PrefabInstanceStatus.NotAPrefab &&
               prefabType != PrefabAssetType.NotAPrefab &&
               prefabStage == null;
    }

}