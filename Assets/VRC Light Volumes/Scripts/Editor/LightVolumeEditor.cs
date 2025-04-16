using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(LightVolume))]
public class LightVolumeEditor : Editor {

    private bool _isEditMode = false;
    private Tool _savedTool;
    private Tool _previousTool;

    private void OnEnable() {
        _previousTool = Tools.current;
    }

    public override void OnInspectorGUI() {

        LightVolume volume = (LightVolume)target;

        serializedObject.Update();

        GUIContent editBoundsContent = EditorGUIUtility.IconContent("EditCollider");
        editBoundsContent.text = " Edit Bounds";

        GUIContent previewProbesContent = EditorGUIUtility.IconContent("LightProbeGroup Gizmo");
        previewProbesContent.text = " Preview Probes";

        GUIStyle toggleStyle = new GUIStyle(GUI.skin.button);
        toggleStyle.imagePosition = ImagePosition.ImageLeft;
        toggleStyle.fixedHeight = 20;
        toggleStyle.fixedWidth = 150;

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        bool newIsEditMode = GUILayout.Toggle(_isEditMode, editBoundsContent, toggleStyle);
        GUILayout.Space(10);
        bool newPreviewProbes = GUILayout.Toggle(volume.PreviewProbes, previewProbesContent, toggleStyle);
        if (newPreviewProbes != volume.PreviewProbes) {
            SceneView.RepaintAll();
            volume.PreviewProbes = newPreviewProbes;
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

#if BAKERY_INCLUDED
        if (volume.RotationType == LightVolume.VolumeRotation.Free && volume.BakingMode == LightVolume.Baking.Bakery) {
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("In Bakery baking mode, only Y-axis rotation is allowed in the editor. Free rotation will still work at runtime.", MessageType.Warning);
        }
#else
        if (volume.BakingMode == LightVolume.Baking.Bakery) {
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("To use Bakery mode, please include Bakery into your project!", MessageType.Error);
        }
#endif

        // Clicking on Edit Bounds button
        if (newIsEditMode != _isEditMode) {

            if (newIsEditMode) { 
                // Went to edit mode
                _savedTool = Tools.current;
                _previousTool = Tool.None;
                Tools.current = Tool.None;
            } else { 
                // Went from edit mode
                Tools.current = _savedTool;
                _previousTool = _savedTool;
            }

            _isEditMode = newIsEditMode;
            Tools.hidden = false;
            SceneView.RepaintAll();
        }

        // Clicking on a new tool
        if (_isEditMode && _previousTool != Tools.current) {
            // Went from edit mode 
            _previousTool = Tools.current;
            _isEditMode = false;
            Tools.hidden = false;
            SceneView.RepaintAll();
        }

        List<string> hiddenFields = new List<string> { "m_Script", "PreviewProbes" };

#if BAKERY_INCLUDED
        hiddenFields.Add("BakeryVolume");
#endif

        if(volume.BakingMode != LightVolume.Baking.Bakery) {
            hiddenFields.Add("Denoise");
        }

        if(volume.BakingMode == LightVolume.Baking.DontBake) {
            hiddenFields.Add("AdaptiveResolution");
            hiddenFields.Add("Resolution");
            hiddenFields.Add("VoxelsPerUnit");
            hiddenFields.Add("PreviewProbes");
        } if (volume.AdaptiveResolution) {
            
        } else {
            hiddenFields.Add("VoxelsPerUnit");
        }

        DrawPropertiesExcluding(serializedObject, hiddenFields.ToArray());

        serializedObject.ApplyModifiedProperties();

    }

    private void OnSceneGUI() {

        LightVolume volume = (LightVolume)target;
        Transform transform = volume.transform;

        Handles.matrix = Matrix4x4.TRS(volume.Position, volume.Rotation, volume.Scale);
        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
        Handles.color = Color.white;
        Handles.DrawWireCube(Vector3.zero, Vector3.one);
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
        Handles.color = new Color(1, 1, 1, 0.2f);
        Handles.DrawWireCube(Vector3.zero, Vector3.one);
        Handles.matrix = Matrix4x4.identity;

        if (!_isEditMode) return;

        // Hide tools
        Tools.hidden = true;

        // Axis colors
        Color colorX = Handles.xAxisColor;
        Color colorY = Handles.yAxisColor;
        Color colorZ = Handles.zAxisColor;

        for (int i = 0; i < 6; i++) {

            Vector3 worldDirection = Vector3.zero;
            Vector3 worldUpDirection = Vector3.zero;

            int axisIndex = i / 2;
            bool isPositive = (i % 2 == 0);

            switch (axisIndex) {
                case 0: // X
                    worldDirection = volume.Rotation * (isPositive ? Vector3.right : Vector3.left);
                    worldUpDirection = volume.Rotation * Vector3.up;
                    Handles.color = colorX;
                    break;
                case 1: // Y
                    worldDirection = volume.Rotation * (isPositive ? Vector3.up : Vector3.down);
                    worldUpDirection = volume.Rotation * Vector3.right;
                    Handles.color = colorY;
                    break;
                case 2: // Z
                    worldDirection = volume.Rotation * (isPositive ? Vector3.forward : Vector3.back);
                    worldUpDirection = volume.Rotation * Vector3.up;
                    Handles.color = colorZ;
                    break;
            }

            // Handle parameters
            Vector3 handlePos = volume.Position + worldDirection * volume.Scale[axisIndex] * 0.5f;
            float handleSize = HandleUtility.GetHandleSize(handlePos) * 0.2f;
            Vector3 handleOffset = handleSize * worldDirection * 0.1f / 0.2f;

            EditorGUI.BeginChangeCheck();

            // Drawing Cone handle
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            Vector3 newHandleLocalPos = Handles.Slider(handlePos + handleOffset, worldDirection, handleSize, Handles.ConeHandleCap, 0.25f) - handleOffset;

            // Drawing X-Ray square
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            Handles.DrawSolidRectangleWithOutline(GetPlaneVertices(handlePos, Quaternion.LookRotation(worldDirection, worldUpDirection), handleSize), new Color(1, 1, 1, 0.15f), Color.white);
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Greater;
            Handles.DrawSolidRectangleWithOutline(GetPlaneVertices(handlePos, Quaternion.LookRotation(worldDirection, worldUpDirection), handleSize), Color.clear, new Color(1, 1, 1, 0.25f));

            // Applying position and rotation
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(transform, "Scale Bounds Size");
                float delta = Vector3.Dot(newHandleLocalPos - handlePos, worldDirection);
                Vector3 modifiedScale = volume.Scale;
                modifiedScale[axisIndex] += delta;
                transform.position += worldDirection * delta / 2;
                SetLossyScale(transform, modifiedScale);
                volume.Recalculate();
            }
        }

    }

    // Bring back tools
    void OnDisable() {
        Tools.hidden = false;
        if (_isEditMode) {
            // Went from edit mode
            Tools.current = _savedTool;
            _previousTool = _savedTool;
        }
    }

    // Setting lossy scale to a specified transform
    private void SetLossyScale(Transform transform, Vector3 targetLossyScale, int maxIterations = 20) {
        Vector3 guess = transform.localScale;
        for (int i = 0; i < maxIterations; i++) {
            transform.localScale = guess;
            Vector3 currentLossy = transform.lossyScale;
            Vector3 ratio = new Vector3(
                currentLossy.x != 0 ? targetLossyScale.x / currentLossy.x : 1f,
                currentLossy.y != 0 ? targetLossyScale.y / currentLossy.y : 1f,
                currentLossy.z != 0 ? targetLossyScale.z / currentLossy.z : 1f
            );
            guess = new Vector3(guess.x * ratio.x, guess.y * ratio.y, guess.z * ratio.z);
        }
    }

    // Plane vertices for drawing a square
    Vector3[] GetPlaneVertices(Vector3 center, Quaternion rotation, float size) {
        Vector3 right = rotation * Vector3.right * size;
        Vector3 up = rotation * Vector3.up * size;
        return new Vector3[] { center - right - up, center - right + up, center + right + up, center + right - up };
    }

}