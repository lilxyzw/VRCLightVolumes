using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public abstract class SingletonEditor<T> : MonoBehaviour where T : SingletonEditor<T> {
    private static T _instance;

    public static T Instance {
        get {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                if (_instance == null) {
                    _instance = FindObjectOfType<T>();
                    if (_instance == null) {
                        var go = new GameObject(typeof(T).Name);
                        _instance = go.AddComponent<T>();
                        _instance.OnInstanceCreated();
                        Undo.RegisterCreatedObjectUndo(go, $"Create {typeof(T).Name}");
                    }
                }
                return _instance;
            }
#endif
            if (_instance == null)
                _instance = FindObjectOfType<T>();

            return _instance;
        }
    }

    protected virtual void OnInstanceCreated() {

    }

    protected virtual void Awake() {
        if (_instance == null)
            _instance = (T)this;
        else if (_instance != this) {
            Debug.LogError($"There is only one instance of {typeof(T).Name} allowed in scene!");
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                EditorApplication.delayCall += () =>{
                    if (!this) return;
                    DestroyImmediate(this);
                };
                return;
            }
#endif
            Destroy(this);
        }
    }
}