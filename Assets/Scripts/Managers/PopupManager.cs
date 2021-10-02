using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;

public class PopupManager : MonoBehaviourSingleton<PopupManager> {
    private List<Popup> _stack = new List<Popup>();
    private List<AsyncOperationHandle<GameObject>> _handles = new List<AsyncOperationHandle<GameObject>>();
    private GameObject _parentCanvas;
    private CanvasScaler _canvasScaler;
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
    private List<(Type, object)> _restorationData = new List<(Type, object)>();
    
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
        _canvasScaler = _parentCanvas.GetOrAddComponent<CanvasScaler>();
    }

    public async UniTask<T> GetOrLoadPopup<T>(bool restore = false, bool track = true) where T : Popup {
        T popup = null;
        
        if (track && ActivePopup != null) {
            _restorationData.Insert(0, (ActivePopup.GetType(), restore ? ActivePopup.GetRestorationData() : null));
        }

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
            int popupIndex = _stack.FindIndex(0, _stack.Count, popup => popup.GetType() == typeof(T));

            if (popupIndex >= 0) {
                while (popupIndex > 0) {
                    _stack[0].OnClose();
                    _stack.RemoveAt(0);
                    Addressables.ReleaseInstance(_handles[0]);
                    _handles.RemoveAt(0);
                    --popupIndex;
                }
                Debug.Assert(_stack[0] is T, "The found popup type doesn't correspond with the request");
                popup = _stack[0] as T;
                popup.gameObject.SetActive(true);
            } else {
                bool isVertical = Screen.height > Screen.width;
                string popupName = typeof(T).Name;
                if (isVertical) {
                    string verticalPopupName = popupName + Popup.VerticalSufix;
                    bool verticalExists = Addressables.ResourceLocators
                        .Where(r => r.Keys.Any(k => verticalPopupName.Equals(k as string))).Any();
                    if (verticalExists) {
                        popupName = verticalPopupName;
                    }
                }
                var handle = Addressables.InstantiateAsync(popupName, _parentCanvas.transform);
                _handles.Insert(0, handle);
                popup = (await handle).GetComponent<T>();
                var rect = (popup.transform as RectTransform);
                rect.offsetMin = Screen.safeArea.min;
                rect.offsetMax = new Vector2(
                    Screen.safeArea.xMax - Screen.width,
                    Screen.safeArea.yMax - Screen.height
                );
                _stack.Insert(0, popup);
            }
        }

        RefreshScaler();
        OnStackChange?.Invoke();

        return popup;
    }
    
    private void RefreshScaler() {
        if (ActivePopup.Vertical != (_canvasScaler.referenceResolution.y > _canvasScaler.referenceResolution.x)) {
            _canvasScaler.referenceResolution = new Vector2(
                _canvasScaler.referenceResolution.y,
                _canvasScaler.referenceResolution.x
            );
            _canvasScaler.matchWidthOrHeight = ActivePopup.Vertical ? 0f : 1f;
        }
    }

    public async void Back() {
        if (_stack.Count > 0) {
            (Type, object) restorationData = _restorationData[0];
            _restorationData.RemoveAt(0);

            RefreshScaler();

            if (restorationData.Item2 != null) {
                MethodInfo method = this.GetType().GetMethod(nameof(GetOrLoadPopup));
                MethodInfo generic = method.MakeGenericMethod(restorationData.Item1);
                var task = generic.Invoke(this, new object[] { false, false });
                var awaiter = task.GetType().GetMethod("GetAwaiter").Invoke(task, null);
                await UniTask.WaitUntil(() => (awaiter.GetType().GetProperty("IsCompleted").GetValue(awaiter) as bool?) ?? true);
                var result = awaiter.GetType().GetMethod("GetResult");
                Popup popup = result.Invoke(awaiter, null) as Popup;
                popup.Restore(restorationData.Item2);
            } else {
                foreach (var popup in _stack) {
                    if (popup.gameObject.activeSelf) {
                        popup.OnClose();
                        popup.gameObject.SetActive(false);
                        break;
                    }
                }
            }

            OnStackChange?.Invoke();
        }
    }
}
