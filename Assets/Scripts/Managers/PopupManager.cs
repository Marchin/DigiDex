using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class PopupManager : MonoBehaviourSingleton<PopupManager> {
    private List<Popup> _stack = new List<Popup>();
    private List<AsyncOperationHandle<GameObject>> _handles = new List<AsyncOperationHandle<GameObject>>();
    private GameObject _parentCanvas;
    
    private void Awake() {
        SceneManager.activeSceneChanged += (prev, next) => {
            foreach (var handle in _handles) {
                Addressables.ReleaseInstance(handle);
            }
            _handles.Clear();
            _stack.Clear();
        };

        _parentCanvas = UnityUtils.GetOrGenerateRootGO("Popup Canvas");
        _parentCanvas.GetOrAddComponent<Canvas>();
    }

    public async UniTask<T> GetOrLoadPopup<T>() where T : Popup {
        T popup = null; 

        while (_stack.Count > 0 && !_stack[0].gameObject.activeSelf) {
            if (_stack[0].GetType() == typeof(T)) {
                popup = _stack[0] as T;
                popup.gameObject.SetActive(true);
            } else {
                _stack.RemoveAt(0);
                Addressables.ReleaseInstance(_handles[0]);
                _handles.RemoveAt(0);
            }
        }

        if (popup == null) {
            var handle = Addressables.InstantiateAsync(typeof(T).Name, _parentCanvas.transform);
            _handles.Add(handle);
            popup = (await handle).GetComponent<T>();
            _stack.Add(popup);
        }


        return popup;
    }

    public void Back() {
        foreach (var popup in _stack) {
            if (popup.gameObject.activeSelf) {
                popup.gameObject.SetActive(false);
                break;
            }
        }
    }
}
