using System;
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
    public Popup ActivePopup {
        get {
            foreach (var popup in _stack) {
                if (popup.gameObject.activeSelf) {
                    return popup;
                }
            }

            return null;
        }
    }
    public event Action OnStackChange;
    
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
                _stack[0].OnClose();
                _stack.RemoveAt(0);
                Addressables.ReleaseInstance(_handles[0]);
                _handles.RemoveAt(0);
            }
        }

        if (popup == null) {
            var handle = Addressables.InstantiateAsync(typeof(T).Name, _parentCanvas.transform);
            _handles.Insert(0, handle);
            popup = (await handle).GetComponent<T>();
            var rect = (popup.transform as RectTransform);
            rect.offsetMin = Screen.safeArea.min;
            rect.offsetMax = new Vector2(
                Screen.safeArea.xMax - Screen.width,
                Screen.height - Screen.safeArea.yMax
            );
            _stack.Insert(0, popup);
        }

        OnStackChange?.Invoke();

        return popup;
    }

    public void Back() {
        if (_stack.Count > 0) {
            foreach (var popup in _stack) {
                if (popup.gameObject.activeSelf) {
                    popup.OnClose();
                    popup.gameObject.SetActive(false);
                    break;
                }
            }
            OnStackChange?.Invoke();
        }
    }
}
