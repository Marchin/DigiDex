using UnityEngine;

public class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviourSingleton<T> {
    private static MonoBehaviourSingleton<T> _instance = null;
    public static T Instance {
        get {
            if (UnityUtils.EditorClosing) {
                return (T)_instance;
            }
            if (_instance == null) {
                _instance = FindObjectOfType<MonoBehaviourSingleton<T>>();
            }
            if (_instance == null) {
                GameObject managerParentGO = UnityUtils.GetOrGenerateRootGO("Managers");
                _instance = new GameObject(typeof(T).Name).AddComponent<T>();
                _instance.transform.SetParent(managerParentGO.transform);
            }

            return (T)_instance;
        }
    }

    protected virtual void Initialize() {

    }

    private void Awake() {
        if (_instance != null) {
            Destroy(this.gameObject);
        }

        _instance = this;

        Initialize();
    }
}