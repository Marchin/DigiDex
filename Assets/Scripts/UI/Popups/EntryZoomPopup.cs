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
    [SerializeField] private Image _image = default;
    [SerializeField] private Button _closeButton = default;
    [SerializeField] private GameObject _loadingWheel = default;
    [SerializeField] private RectTransform _bounds = default;
    [SerializeField] private PointerDetector _imagePointerDetector = default;
    [SerializeField] private float _maxZoom = default;
    [SerializeField] private float _zoomSpeed = default;
    [SerializeField] private float _pinchZoomScaling = default;
    [SerializeField] private float _closeButtonOnDelay = default;
    private IDataEntry _entry;
    private bool _wasMouseDown;
    private Vector2 _lastMousePos;
    private Vector2? _lastFingerDiff;
    private OperationBySubscription.Subscription _performanceHandle;
    private bool _draggingImage;
    private CancellationTokenSource _cts;

    private void Awake() {
        _closeButton.onClick.AddListener(() => _ = PopupManager.Instance.Back());
    }

    public async void Populate(IDataEntry entry) {
        _entry = entry;

        var rect = (_image.transform as RectTransform);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;

        CancelShowCloseButton();
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
        CancelShowCloseButton();
    }

    private void Update() {
        bool isMouseDown = Input.GetMouseButton(0) && (_wasMouseDown || _imagePointerDetector.IsPointerIn);

        if (!isMouseDown && !_imagePointerDetector.IsPointerIn) {
            ShowCloseButton();
            _wasMouseDown = false;
            return; 
        }

        _draggingImage = isMouseDown;
        float scaleIncrease = 0f;
        Vector2 delta = Vector2.zero;
        RectTransform rect = _image.transform as RectTransform;

        Vector2 mousePos = Input.mousePosition;

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

        if ((delta != Vector2.zero)) { 
            rect.anchoredPosition += delta;
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

        _wasMouseDown = isMouseDown;
        _lastMousePos = mousePos;

        bool showCloseButton = (delta == Vector2.zero) && 
            (scaleIncrease == 0f) && 
            !isMouseDown && 
            (Input.touchCount == 0);
        
        if (showCloseButton) {
            ShowCloseButton();
        } else {
            CancelShowCloseButton();
            _closeButton.gameObject.SetActive(false);
        }

        if (isMouseDown || (Input.touchCount > 0)) {
            _performanceHandle = PerformanceManager.Instance.HighPerformance.Subscribe();
        } else {
            _performanceHandle?.Finish();
        }
    }

    private async void ShowCloseButton() {
        if (_cts == null) {
            _cts = new CancellationTokenSource();
        } else {
            return;
        }

        bool isCanceled = await UniTask.Delay((int)(_closeButtonOnDelay * 1000f), cancellationToken: _cts.Token)
            .SuppressCancellationThrow();

        if (!isCanceled) {
            _closeButton.gameObject.SetActive(true);
            _cts.Dispose();
            _cts = null;
        }
    }

    private void CancelShowCloseButton() {
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
