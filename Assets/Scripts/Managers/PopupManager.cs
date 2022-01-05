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
    public bool Vertical;
    public object Data;
}

public class PopupManager : MonoBehaviourSingleton<PopupManager> {
    private List<Popup> _stack = new List<Popup>();
    private List<AsyncOperationHandle<GameObject>> _handles = new List<AsyncOperationHandle<GameObject>>();
    private GameObject _parentCanvas;
    private CanvasScaler _canvasScaler;
    private List<CanvasScaler> _registeredCanvasScalers = new List<CanvasScaler>();
    public event Action OnStackChange;
    public event Action OnWindowResize;
    public event Action OnRotation;
    private List<PopupRestorationData> _restorationData = new List<PopupRestorationData>();
    private bool _loadingPopup;
    public bool ClosingPopup { get; private set; }
    public bool IsScreenOnPortrait => (Screen.height > Screen.width);
    private ScreenOrientation _lastDeviceOrientation;
    private Vector2 _lastScreenSize;
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
        RegisterCanvasScalerForRotationScaling(_canvasScaler);
        RefreshReferenceResolution();
        _lastDeviceOrientation = Screen.orientation;
        PopupLoop();
    }

    private void RemovePopup(int index = 0) {
        _stack.RemoveAt(index);
        Addressables.ReleaseInstance(_handles[index]);
        _handles.RemoveAt(index);
    }

    private void RefreshReferenceResolution() {
        foreach (var canvasScaler in _registeredCanvasScalers) {
            if (IsScreenOnPortrait != (canvasScaler.referenceResolution.y > canvasScaler.referenceResolution.x)) {
                canvasScaler.referenceResolution = new Vector2(
                    canvasScaler.referenceResolution.y,
                    canvasScaler.referenceResolution.x
                );
                canvasScaler.matchWidthOrHeight = IsScreenOnPortrait ? 0f : 1f;
            }
        }
    }

    public async UniTask<T> GetOrLoadPopup<T>(bool restore = true, bool track = true) where T : Popup {
        var lockHandle = ApplicationManager.Instance.LockScreen();
        T popup = null;
        _loadingPopup = true;
        if (track && ActivePopup != null) {
            var activePopup = ActivePopup;
            PopupRestorationData restorationData = new PopupRestorationData {
                PopupType = activePopup.GetType(),
                IsFullScreen = activePopup.FullScreen,
                Vertical = activePopup.Vertical,
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
                (popup.transform as RectTransform)?.AdjustToSafeZone();
                _stack.Insert(0, popup);
            }
        }

        OnStackChange?.Invoke();
        _loadingPopup = false;

        lockHandle.Complete();

        return popup;
    }

    private async void RefreshScaler() {
        RefreshReferenceResolution();
        if (_loadingPopup || ActivePopup == null || _stack.Count <= 0) return;

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

            lastVisiblePopup = Mathf.Min(_stack.Count - 1, lastVisiblePopup);
            int counter = lastVisiblePopup;
            Popup popup = _stack[lastVisiblePopup];
            while (counter >= 0) {
                PopupRestorationData restorationData = new PopupRestorationData {
                    PopupType = popup.GetType(),
                    IsFullScreen = popup.FullScreen,
                    Vertical = popup.Vertical,
                    Data = popup.GetRestorationData()
                };
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

    private async void PopupLoop() {
        while (true) {
            RefreshScaler();
            if (Screen.orientation != _lastDeviceOrientation) {
                foreach (var popup in _stack) {
                    (popup.transform as RectTransform).AdjustToSafeZone();
                }

                OnRotation?.Invoke();
                _lastDeviceOrientation = Screen.orientation;
            }
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            if (screenSize != _lastScreenSize) {
                OnWindowResize?.Invoke();
                _lastScreenSize = screenSize;
            }
            await UniTask.Delay(500);
        }
    }

    public async UniTask Back() {
        if (ClosingPopup) {
            Debug.LogWarning("A popup is already being closed");
            return;
        }

        if (ActivePopup != null) {
            ClosingPopup = true;
            int startingIndex = -1;
            if (_restorationData.Count > 0) {
                startingIndex = 0;

                while ((startingIndex + 1) < _restorationData.Count && !_restorationData[startingIndex].IsFullScreen) {
                    ++startingIndex;
                }
                while (startingIndex > 0 && _restorationData[startingIndex].Vertical == IsScreenOnPortrait) {
                    --startingIndex;
                }
            }

            if (startingIndex >= 0) {
                var handle = ApplicationManager.Instance.DisplayLoadingScreen();
                while (startingIndex >= 0) {
                    PopupRestorationData restorationData = _restorationData[startingIndex];

                    if (restorationData.Data != null) {
                        await RestorePopup(restorationData);
                    } else {
                        CloseActivePopup();
                    }
                    
                    if (startingIndex == 0) {
                        _restorationData.RemoveAt(startingIndex);
                    } else {
                        _restorationData[startingIndex].Data = null;
                    }
                    --startingIndex;
                }
                handle.Complete();
            } else {
                CloseActivePopup();
            }

            ClosingPopup = false;
            OnStackChange?.Invoke();
        } else {
            UnityUtils.Quit();
        }
    }

    public async void ClearStackUntilPopup<T>() where T : Popup {
        if (ActivePopup == null || ActivePopup.GetType() == typeof(T)) {
            return;
        }

        while ((_restorationData.Count > 0) && _restorationData[0].PopupType != typeof(T)) {
            _restorationData.RemoveAt(0);
        }

        if (_restorationData.Count > 0) {
            Debug.Assert(_restorationData[0].PopupType == typeof(T), "Something went wrong while clearing stack");
            while ((ActivePopup != null) && (ActivePopup.GetType() != typeof(T))) {
                CloseActivePopup();
            }

            await RestorePopup(_restorationData[0]);
        }

        OnStackChange?.Invoke();
    }
    
    private void CloseActivePopup() {
        foreach (var popup in _stack) {
            if (popup.gameObject.activeSelf) {
                popup.OnClose();
                popup.gameObject.SetActive(false);
                break;
            }
        }
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

    public void RegisterCanvasScalerForRotationScaling(CanvasScaler canvasScaler) {
        if (canvasScaler != null && !_registeredCanvasScalers.Contains(canvasScaler)) {
            _registeredCanvasScalers.Add(canvasScaler);
            RefreshReferenceResolution();
        }
    }

    public void UnregisterCanvasScalerForRotationScaling(CanvasScaler canvasScaler) {
        if (canvasScaler != null && _registeredCanvasScalers.Contains(canvasScaler)) {
            _registeredCanvasScalers.Remove(canvasScaler);
        }
    }
}
