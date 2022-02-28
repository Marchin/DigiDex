using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using System.Threading;

public class EntryZoomPopup : Popup {
    public class PopupData {
        public IDataEntry Entry;
    }

    private const float MinZoom = 1f;
    private const float MinDeltaSqDist = 0.1f;
    [SerializeField] private Image _image = default;
    [SerializeField] private Button _closeButton = default;
    [SerializeField] private GameObject _loadingWheel = default;
    [SerializeField] private RectTransform _bounds = default;
    [SerializeField] private PointerDetector _imagePointerDetector = default;
    [SerializeField] private float _maxZoom = default;
    [SerializeField] private float _zoomSpeed = default;
    [SerializeField] private float _pinchZoomScaling = default;
    [SerializeField] private float _closeButtonOnDelay = default;
    [SerializeField] private float _closeButtonOffDelay = default;
    [SerializeField] private float _tapInterval = default;
    [SerializeField] private float _doubleTapZoom = default;
    private IDataEntry _entry;
    private bool _wasMouseDown;
    private Vector2 _lastMousePos;
    private Vector2? _initMousePos;
    private Vector2? _lastFingerDiff;
    private OperationBySubscription.Subscription _performanceHandle;
    private bool _draggingImage;
    private bool _isHidding;
    private bool _isShowing;
    private CancellationTokenSource _cts;

    private void Awake() {
        _closeButton.onClick.AddListener(() => _ = PopupManager.Instance.Back());
    }

    public async void Populate(IDataEntry entry) {
        _entry = entry;

        var rect = (_image.transform as RectTransform);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;

        CancelCloseButtonTransition();
        _closeButton.gameObject.SetActive(true);

        _loadingWheel.gameObject.SetActive(true);
        _image.sprite = await Addressables.LoadAssetAsync<Sprite>(_entry.Sprite);
        if (_image.sprite != null) {

            if (Vertical) {
                rect.sizeDelta = new Vector2(
                    _bounds.rect.width,
                    _bounds.rect.width * ((float)_image.sprite.rect.height / (float)_image.sprite.rect.width));
            } else {
                rect.sizeDelta = new Vector2(
                    _bounds.rect.height * ((float)_image.sprite.rect.width / (float)_image.sprite.rect.height),
                    _bounds.rect.height);
            }
        }
        _loadingWheel.gameObject.SetActive(false);
    }

    private void OnDisable() {
        CancelCloseButtonTransition();
    }

    private void Update() {
        bool isMouseDown = Input.GetMouseButton(0) && (_wasMouseDown || _imagePointerDetector.IsPointerIn);

        if (!isMouseDown) {
            _initMousePos = null;
            _draggingImage = false;
        }

        if (!isMouseDown && !_imagePointerDetector.IsPointerIn && !_closeButton.gameObject.activeSelf) {
            ShowCloseButton();
            _wasMouseDown = false;
            return; 
        }

        float scaleIncrease = 0f;
        Vector2 delta = Vector2.zero;
        RectTransform rect = _image.transform as RectTransform;

        Vector2 mousePos = Input.mousePosition;

        if (_wasMouseDown && isMouseDown && !_initMousePos.HasValue) {
            _initMousePos = mousePos;
        }

        if ((Input.touchCount <= 1) && !_lastFingerDiff.HasValue && isMouseDown && _wasMouseDown) {
            delta = (mousePos - _lastMousePos);
        }

        Vector2? zoomPoint = null;
        if (Input.touchCount == 2) {
            Vector2 fingerDiff = Input.touches[0].position - Input.touches[1].position;
            if (_lastFingerDiff.HasValue) {
                scaleIncrease = _pinchZoomScaling * (fingerDiff.sqrMagnitude - _lastFingerDiff.Value.sqrMagnitude);
                zoomPoint = Input.touches[1].position + (fingerDiff * 0.5f);
            } else {
                zoomPoint = null;
            }
            _lastFingerDiff = fingerDiff;
        } else {
            scaleIncrease = Input.mouseScrollDelta.y * _zoomSpeed;
            zoomPoint = (scaleIncrease != 0f) ? Input.mousePosition : (Vector2?)null;
            _lastFingerDiff = null;
        }


        if (zoomPoint.HasValue &&
            (((scaleIncrease < 0f) && (MinZoom < _image.transform.localScale.x)) ||
            ((scaleIncrease > 0f) && (_image.transform.localScale.x < _maxZoom)))
        ) {
            Vector2 boundsCenter = _bounds.transform.position;
            delta = (zoomPoint.Value - boundsCenter) * -scaleIncrease;
        }

        _image.transform.localScale = Vector3.one * 
            Mathf.Clamp(_image.transform.localScale.x + scaleIncrease,
                MinZoom,
                _maxZoom);

        float deltaDistSq = delta.sqrMagnitude;

        if ((deltaDistSq > MinDeltaSqDist)) { 
            rect.anchoredPosition += delta;
            AdjustImage();
        }

        if (!_draggingImage && _initMousePos.HasValue) {
            Vector2 dragDelta = mousePos - _initMousePos.Value;
            if (dragDelta.sqrMagnitude >= 200f) {
                _draggingImage = true;
            }
        }

        bool triggerDoubleTap = isMouseDown && !_wasMouseDown;

        _wasMouseDown = isMouseDown;
        _lastMousePos = mousePos;

        bool showCloseButton = (deltaDistSq < MinDeltaSqDist) && 
            (scaleIncrease == 0f) && 
            (!isMouseDown) && 
            (Input.touchCount == 0);
        
        if (showCloseButton != _closeButton.gameObject.activeSelf) {
            if (showCloseButton) {
                if (!_isShowing) {
                    ShowCloseButton();
                }
            } else {
                if (!_isHidding) {
                    HideCloseButton(withDelay: (scaleIncrease == 0f));
                }
            }
        } else if (showCloseButton && _isHidding) {
            CancelCloseButtonTransition();
        }

        if (triggerDoubleTap) {
            TrackDoubleTap();
        }

        if (isMouseDown || (Input.touchCount > 0)) {
            _performanceHandle = PerformanceManager.Instance.HighPerformance.Subscribe();
        } else {
            _performanceHandle?.Finish();
        }
    }

    private void AdjustImage() {
        RectTransform rect = _image.transform as RectTransform;
        
        Vector2 pos = rect.anchoredPosition;
        float halfHeight = 0.5f * rect.rect.height;
        float halfWidth = 0.5f * rect.rect.width;
        float xMax = pos.x + halfWidth * _image.transform.localScale.x;
        float xMin = pos.x - halfWidth * _image.transform.localScale.x;
        float yMax = pos.y + halfHeight * _image.transform.localScale.y;
        float yMin = pos.y - halfHeight * _image.transform.localScale.y;

        if (Vertical || ((rect.rect.width * _image.transform.localScale.x) > _bounds.rect.width)) {
            if ((xMin > _bounds.rect.xMin)) {
                pos.x += _bounds.rect.xMin - xMin;
            }
            if ((xMax < _bounds.rect.xMax)) {
                pos.x += _bounds.rect.xMax - xMax;
            }
        } else {
            pos.x = 0f;
        } 

        if (!Vertical || ((rect.rect.height * _image.transform.localScale.y) > _bounds.rect.height)) {
            if ((yMin > _bounds.rect.yMin)) {
                pos.y += _bounds.rect.yMin - yMin;
            }
            if ((yMax < _bounds.rect.yMax)) {
                pos.y += _bounds.rect.yMax - yMax;
            }
        } else {
            pos.y = 0f;
        } 

        rect.anchoredPosition = pos;
    }

    private async void TrackDoubleTap() {
        int interval = (int)(1000f * _tapInterval);
        var trigger = UniTask.WaitUntil(() => !_wasMouseDown || _draggingImage);
        var timeOut = UniTask.Delay(interval);

        await UniTask.WhenAny(trigger, timeOut);

        if (_wasMouseDown || _draggingImage) {
            return;
        }
        
        trigger = UniTask.WaitUntil(() => _wasMouseDown);
        timeOut = UniTask.Delay(interval);

        await UniTask.WhenAny(trigger, timeOut);
        
        if (!_wasMouseDown) {
            return;
        }

        
        trigger = UniTask.WaitUntil(() => !_wasMouseDown || _draggingImage);
        timeOut = UniTask.Delay(interval);

        await UniTask.WhenAny(trigger, timeOut);

        if (_wasMouseDown || _draggingImage) {
            return;
        }

        if (_image.transform.localScale.x == MinZoom) {
            _image.transform.localScale = Vector3.one * _doubleTapZoom;
            _image.transform.localPosition = _doubleTapZoom * 
                -(Input.mousePosition - 0.5f * new Vector3(Screen.width, Screen.height));
        } else {
            _image.transform.localScale = Vector3.one;
            _image.transform.localPosition = Vector3.zero;
        }
    
        AdjustImage();
    }

    private async void ShowCloseButton() {
        if (_isShowing) {
            return;
        }
        _cts = new CancellationTokenSource();

        _isShowing = true;
        bool isCanceled = await UniTask.Delay((int)(_closeButtonOnDelay * 1000f), cancellationToken: _cts.Token)
            .SuppressCancellationThrow();
        _isShowing = false;

        if (!isCanceled) {
            _closeButton.gameObject.SetActive(true);
            _cts.Dispose();
            _cts = null;
        }
    }

    private async void HideCloseButton(bool withDelay) {
        if (_isHidding) {
            return;
        }

        if (!withDelay) {
            _closeButton.gameObject.SetActive(false);
            return;
        }
        
        _cts = new CancellationTokenSource();
        _isHidding = true;
        bool isCanceled = await UniTask.Delay((int)(_closeButtonOffDelay * 1000f), cancellationToken: _cts.Token)
            .SuppressCancellationThrow();
        _isHidding = false;

        if (!isCanceled) {
            _closeButton.gameObject.SetActive(false);
            _cts.Dispose();
            _cts = null;
        }
    }

    private void CancelCloseButtonTransition() {
        if (_cts != null) {
            if (!_cts.IsCancellationRequested) {
                _cts.Cancel();
                _cts.Dispose();
            }
            _cts = null;
        }
    }

    public override object GetRestorationData() {
        PopupData data = new PopupData { Entry = _entry };

        return data;
    }

    public override void Restore(object data) {
        if (data is PopupData popupData) {
            Populate(popupData.Entry);
        }
    }
}
