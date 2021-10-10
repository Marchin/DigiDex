using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

public static class UnityUtils {

    public static bool EditorClosing { get; private set; }

    public static void Quit() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    public static void RegisterToEditorStateChange() {
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChange;
    }
#endif

#if UNITY_EDITOR
    public static void OnPlayModeStateChange(UnityEditor.PlayModeStateChange state) {
        EditorClosing = (state == UnityEditor.PlayModeStateChange.ExitingPlayMode);
    }
#endif 

    public static bool FloatsAreEqual(float a, float b, float epsilon = 0.001f) {
        bool result = (a > b - epsilon) && (a < b + epsilon);

        return result;
    }

    public static GameObject GetOrGenerateRootGO(string goName) {
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        GameObject queriedGO = rootObjects.FirstOrDefault(x => string.Compare(x.name, goName) == 0);
        if (queriedGO == null) {
            queriedGO = new GameObject(goName);
        }
        return queriedGO;
    }

    public static T GetOrAddComponent<T>(this GameObject go) where T : Component {
        T component = go.GetComponent<T>();

        if (component == null) {
            component = go.AddComponent<T>();
        }

        return component;
    }

    public static int Repeat(int n, int count) {
        int result = n % count;
        if (result < 0) {
            result = count + result;
        }
        return result;
    }

    public static void SnapTo(this ScrollRect scroll, RectTransform target) {
        Canvas.ForceUpdateCanvases();
        scroll.content.anchoredPosition = 
            (Vector2)scroll.transform.InverseTransformPoint(scroll.content.position) - 
                (Vector2)scroll.transform.InverseTransformPoint(target.position);
    }

    public static AsyncOperationHandle<Sprite> LoadSprite(
        this Image image, 
        AssetReferenceAtlasedSprite spriteRef,
        CancellationToken cancellationToken = default
    ) {
        if (spriteRef.RuntimeKeyIsValid()) {
            var handle = Addressables.LoadAssetAsync<Sprite>(spriteRef);
            handle.WithCancellation(cancellationToken).ContinueWith(sprite => {
                image.sprite = sprite;
                image.gameObject.SetActive(sprite != null);
            }).SuppressCancellationThrow();

            return handle;
        } else {
            return default;
        }
    }
}
