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

public class PopupRestorationData {
    public Type PopupType;
    public bool IsFullScreen;
    public object Data;
}

public class PopupManager : MonoBehaviourSingleton<PopupManager> {
    private List<Popup> _stack = new List<Popup>();
    private List<AsyncOperationHandle<GameObject>> _handles = new List<AsyncOperationHandle<GameObject>>();
    private GameObject _parentCanvas;
    private CanvasScaler _canvasScaler;
    public event Action OnStackChange;
    private List<PopupRestorationData> _restorationData = new List<PopupRestorationData>();
    private bool _loadingPopup;
    public bool IsScreenOnPortrait => (Screen.height > Screen.width);
    private ScreenOrientation _lastDeviceOrientation;
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
        RefreshReferenceResolution();
        _canvasScaler.matchWidthOrHeight = IsScreenOnPortrait ? 0f : 1f;
        _lastDeviceOrientation = Screen.orientation;
    }

    private void RemovePopup(int index = 0) {
        _stack.RemoveAt(index);
        Addressables.ReleaseInstance(_handles[index]);
        _handles.RemoveAt(index);
    }

    private void RefreshReferenceResolution() {
        if (IsScreenOnPortrait != (_canvasScaler.referenceResolution.y > _canvasScaler.referenceResolution.x)) {
            _canvasScaler.referenceResolution = new Vector2(
                _canvasScaler.referenceResolution.y,
                _canvasScaler.referenceResolution.x
            );
            _canvasScaler.matchWidthOrHeight = IsScreenOnPortrait ? 0f : 1f;
        }
    }

    public async UniTask<T> GetOrLoadPopup<T>(bool restore = false, bool track = true) where T : Popup {
        T popup = null;
        _loadingPopup = true;
        if (track && ActivePopup != null) {
            var activePopup = ActivePopup;
            PopupRestorationData restorationData = new PopupRestorationData {
                PopupType = activePopup.GetType(),
                IsFullScreen = activePopup.FullScreen,
                Data = restore ? activePopup.GetRestorationData() : null
            };
            _restorationData.Insert(0, restorationData);
        }

        while (_stack.Count > 0 && !_stack[0].gameObject.activeSelf) {
            if (_stack[0].GetType() == typeof(T)) {
                popup = _stack[0] as T;
                popup.gameObject.SetActive(true);
            } else {
                _stack[0].OnClose();
                RemovePopup();
            }
        }

        bool inPortait = Screen.height > Screen.width;
        if (popup == null) {
            int popupIndex = _stack.FindIndex(0, _stack.Count, popup => popup.GetType() == typeof(T));

            if (popupIndex >= 0) {
                Popup aux = _stack[popupIndex];
                if (inPortait != aux.Vertical) {
                    RemovePopup(popupIndex);
                    popupIndex = -1;
                }
            }

            if (popupIndex >= 0) {
                while (popupIndex > 0) {
                    _stack[0].OnClose();
                    RemovePopup();
                    --popupIndex;
                }
                Debug.Assert(_stack[0] is T, "The found popup type doesn't correspond with the request");
                popup = _stack[0] as T;
                popup.gameObject.SetActive(true);
            } else {
                string popupName = typeof(T).Name;
                if (inPortait) {
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
                AdjustToSafeZone(popup.transform as RectTransform);
                _stack.Insert(0, popup);
            }
        }

        OnStackChange?.Invoke();
        _loadingPopup = false;

        return popup;
    }

    private void AdjustToSafeZone(RectTransform rect) {
        rect.offsetMin = Screen.safeArea.min;
        rect.offsetMax = new Vector2(
            Screen.safeArea.xMax - Screen.width,
            Screen.safeArea.yMax - Screen.height
        );
    }
    
    private async void RefreshScaler() {
        RefreshReferenceResolution();
        if (_loadingPopup || ActivePopup == null) return;

        if (ActivePopup.Vertical != IsScreenOnPortrait) {
            var handle = ApplicationManager.Instance.DisplayLoadingScreen();
            
            // Cleaning up inactive popup facilitates the algorithm and at this point their orientation probably don't match
            while (!_stack[0].gameObject.activeSelf) {
                RemovePopup();
            }

            int lastVisiblePopup = 0;
            while (lastVisiblePopup < _stack.Count) {
                if (_stack[lastVisiblePopup].FullScreen) {
                    break;
                } else {
                    ++lastVisiblePopup;
                }
            }

            int counter = lastVisiblePopup;
            Popup popup = _stack[lastVisiblePopup];
            while (counter >= 0) {
                PopupRestorationData restorationData = null;
                restorationData.PopupType = popup.GetType();
                restorationData.IsFullScreen = popup.FullScreen;
                restorationData.Data = popup.GetRestorationData();
                RemovePopup(lastVisiblePopup);
                await RestorePopup(restorationData);
                popup = _stack[lastVisiblePopup];
                --counter;
            }

            handle.Complete();
        }
    }

    public T GetLoadedPopupOfType<T>() where T : Popup {
        T popup = _stack.Find(p => p.GetType() == typeof(T)) as T;

        return popup;
    }

    private void Update() {
        RefreshScaler();

        if (Screen.orientation != _lastDeviceOrientation) {
            foreach (var popup in _stack) {
                AdjustToSafeZone(popup.transform as RectTransform);
            }

            _lastDeviceOrientation = Screen.orientation;
        }
    }

    public async void Back() {
        if (ActivePopup != null) {
            int startingIndex = -1;
            if (_restorationData.Count > 0) {
                startingIndex = 0;

                while ((startingIndex + 1) < _restorationData.Count && !_restorationData[startingIndex].IsFullScreen) {
                    ++startingIndex;
                }
            }

            var handle = ApplicationManager.Instance.DisplayLoadingScreen();
            while (startingIndex >= 0) {
                PopupRestorationData restorationData = _restorationData[startingIndex];

                if (restorationData.Data != null) {
                    await RestorePopup(restorationData);
                } else {
                    foreach (var popup in _stack) {
                        if (popup.gameObject.activeSelf) {
                            popup.OnClose();
                            popup.gameObject.SetActive(false);
                            break;
                        }
                    }
                }
                
                if (startingIndex == 0) {
                    _restorationData.RemoveAt(startingIndex);
                } else {
                    _restorationData[startingIndex].Data = null;
                }
                --startingIndex;
            }
            handle.Complete();

            OnStackChange?.Invoke();
        } else {
            UnityUtils.Quit();
        }
    }

    public async void ClearStackUntilPopup<T>() where T : Popup {
        if (ActivePopup == null || ActivePopup.GetType() == typeof(T)) {
            return;
        }

        while (ActivePopup != null && ActivePopup.GetType() != typeof(T)) {
            PopupRestorationData restorationData = null;

            if (_restorationData.Count > 0) {
                restorationData = _restorationData[0];
                _restorationData.RemoveAt(0);
            }

            if (restorationData.PopupType == typeof(T) && restorationData.Data != null) {
                await RestorePopup(restorationData);
            } else {
                foreach (var popup in _stack) {
                    if (popup.gameObject.activeSelf) {
                        popup.OnClose();
                        popup.gameObject.SetActive(false);
                        break;
                    }
                }
            }
        }

        OnStackChange?.Invoke();
    }

    private async UniTask RestorePopup(PopupRestorationData restorationData) {
        MethodInfo method = this.GetType().GetMethod(nameof(GetOrLoadPopup));
        MethodInfo generic = method.MakeGenericMethod(restorationData.PopupType);
        var task = generic.Invoke(this, new object[] { false, false });
        var awaiter = task.GetType().GetMethod("GetAwaiter").Invoke(task, null);
        await UniTask.WaitUntil(() =>
            (awaiter.GetType().GetProperty("IsCompleted").GetValue(awaiter) as bool?) ?? true);
        var result = awaiter.GetType().GetMethod("GetResult");
        Popup popup = result.Invoke(awaiter, null) as Popup;
        popup.Restore(restorationData.Data);
    }
}
