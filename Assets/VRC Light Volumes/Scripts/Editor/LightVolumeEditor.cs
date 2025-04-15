using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.IMGUI.Controls;

[CustomEditor(typeof(LightVolume))]
public class LightVolumeEditor : Editor {

    private bool _isEditMode = false;
    private Tool _savedTool;
    private Tool _previousTool;

    private void OnEnable() {
        _previousTool = Tools.current;
    }

    public override void OnInspectorGUI() {

        GUIContent content = EditorGUIUtility.IconContent("EditCollider");
        content.text = " Edit Bounds";

        GUILayout.Space(10);

        GUIStyle toggleStyle = new GUIStyle(GUI.skin.button);
        toggleStyle.imagePosition = ImagePosition.ImageLeft;

        if (_isEditMode && _previousTool != Tools.current) {
            _previousTool = _savedTool;
            Tools.current = _savedTool;
            _isEditMode = false;
            Tools.hidden = false;
            SceneView.RepaintAll();
        }

        bool newIsEditMode = GUILayout.Toggle(_isEditMode, content, toggleStyle);

        if (newIsEditMode != _isEditMode) {

            if (newIsEditMode) {
                _savedTool = Tools.current;
                Tools.current = Tool.None;
                _previousTool = Tool.None;
            } else {
                Tools.current = _savedTool;
                _previousTool = _savedTool;
            }

            _isEditMode = newIsEditMode;
            Tools.hidden = false;
            SceneView.RepaintAll();
        }

        GUILayout.Space(10);

        string[] hiddenFields = new string[] { "m_Script" };
        DrawPropertiesExcluding(serializedObject, hiddenFields);

    }

    private void OnSceneGUI() {

        if (!_isEditMode) return;

        LightVolume volume = (LightVolume)target;
        Transform transform = volume.transform;

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
                    worldDirection = transform.rotation * (isPositive ? Vector3.right : Vector3.left);
                    worldUpDirection = transform.rotation * Vector3.up;
                    Handles.color = colorX;
                    break;
                case 1: // Y
                    worldDirection = transform.rotation * (isPositive ? Vector3.up : Vector3.down);
                    worldUpDirection = transform.rotation * Vector3.right;
                    Handles.color = colorY;
                    break;
                case 2: // Z
                    worldDirection = transform.rotation * (isPositive ? Vector3.forward : Vector3.back);
                    worldUpDirection = transform.rotation * Vector3.up;
                    Handles.color = colorZ;
                    break;
            }

            // Handle parameters
            Vector3 handlePos = transform.position + worldDirection * transform.lossyScale[axisIndex] * 0.5f;
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
                Vector3 modifiedScale = transform.lossyScale;
                modifiedScale[axisIndex] += delta;
                transform.position += worldDirection * delta / 2;
                SetLossyScale(transform, modifiedScale);
            }
        }

    }

    // Bring back tools
    void OnDisable() {
        Tools.hidden = false;
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