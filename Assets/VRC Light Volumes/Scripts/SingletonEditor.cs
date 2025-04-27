using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DefaultExecutionOrder(-10000)]
public abstract class SingletonEditor<T> : MonoBehaviour where T : SingletonEditor<T> {
    private static T _instance;

    public static T Instance {
        get {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                if (!_instance || !_instance)
                    _instance = FindObjectOfType<T>() ?? CreateTempInstance();
                return _instance;
            }
#endif
            if (!_instance || !_instance)
                _instance = FindObjectOfType<T>();
            return _instance;
        }
    }

    protected virtual void Awake() {
        if (_instance == null || !_instance)
        {
            _instance = (T)this;
            return;
        }

        if (_instance != this)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying &&
                (_instance.hideFlags & HideFlags.DontSave) != 0) {
                var temp = _instance;
                _instance = (T)this;
                DestroyImmediate(temp);
                return;
            }

            if (!Application.isPlaying)
            {
                EditorApplication.delayCall += () => {
                    if (this) DestroyImmediate(this);
                };
                return;
            }
#endif
            Destroy(this);
        }
    }

    protected virtual void OnDestroy() {
        if (_instance == this) _instance = null;
    }

    protected virtual void OnInstanceCreated() { }


#if UNITY_EDITOR
    private static T CreateTempInstance() {
        var go = new GameObject($"{typeof(T).Name}_Temp") {
            hideFlags = HideFlags.DontSave
        };
        var inst = go.AddComponent<T>();
        inst.OnInstanceCreated();
        return inst;
    }
#endif
}
