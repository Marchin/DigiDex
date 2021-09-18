using UnityEngine;

public class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviourSingleton<T> {
    private static MonoBehaviourSingleton<T> instance = null;

    public static T Instance {
        get {
            if (instance == null) {
                instance = FindObjectOfType<MonoBehaviourSingleton<T>>();
            }
            if (instance == null) {
                GameObject managerParentGO = UnityUtils.GetOrGenerateRootGO("Managers");
                instance = new GameObject(typeof(T).Name).AddComponent<T>();
                instance.transform.SetParent(managerParentGO.transform);
            }

            return (T)instance;
        }
    }

    protected virtual void Initialize() {

    }

    private void Awake() {
        if (instance != null) {
            Destroy(this.gameObject);
        }

        instance = this;

        Initialize();
    }
}