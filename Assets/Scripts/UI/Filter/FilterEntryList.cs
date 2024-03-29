using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class FilterEntryList : DataList<FilterEntryElement, FilterEntryData>, IPointerEnterHandler, IPointerExitHandler {
    [SerializeField] private float _hideInSecs = 2f;
    [SerializeField] private RectTransform _safeArea = default;
    [SerializeField] private RectTransform _togglesRect = default;
    [SerializeField] private GameObject _listBackground = default;
    [SerializeField] private Toggle _anyToggle = default;
    [SerializeField] private Toggle _allToggle = default;
    [System.NonSerialized] public UnityEngine.Object LastCaller;
    [System.NonSerialized] public bool IsMouseOut;
    public GameObject ListBackground => _listBackground;
    public Toggle AnyToggle => _anyToggle;
    public Toggle AllToggle => _allToggle;
    private float accum = 0f;
    private bool _wasScrolling;

    private void Awake() {
        OnPopulate += _ => {
            IsMouseOut = false;
            accum = 0f;
        };
    }

    private void OnEnable() {
        _listBackground.gameObject.SetActive(false);
    }

    public void ResetScroll() {
        _scroll.verticalNormalizedPosition = 1f;
    }

    private void Update() {
        if (IsMouseOut && !Application.isMobilePlatform) {
            accum += Time.unscaledDeltaTime;
            if (accum >= _hideInSecs) {
                accum = 0f;
                gameObject.SetActive(false);
            }
        } else {
            accum = 0;
        }
        
        bool isScrolling = _scroll.velocity.sqrMagnitude > 0f;
        if (isScrolling != _wasScrolling) {
            foreach (var element in Elements) {
                element.IsScrollContentOn = !isScrolling;
            }
        }
        _wasScrolling = isScrolling;
    }

    public void AdjustPosition(RectTransform rectTransform) {
        RectTransform scrollRectTransform = transform as RectTransform;
        if (rectTransform.position.y < (0.5f * Screen.height)) {
            scrollRectTransform.pivot = new Vector2(0.5f, 0f);
            Vector2 topCenter = new Vector2(rectTransform.rect.center.x, 
                rectTransform.rect.yMax);
            scrollRectTransform.position = rectTransform.TransformPoint(topCenter);
            float yMax = _safeArea.rect.yMax + _safeArea.position.y;
            scrollRectTransform.sizeDelta = new Vector2(
                rectTransform.rect.width, 
                Mathf.Min(_scroll.content.rect.height + _togglesRect.rect.height,
                    yMax - scrollRectTransform.position.y));
        } else {
            scrollRectTransform.pivot = new Vector2(0.5f, 1f);
            Vector2 bottomCenter = new Vector2(rectTransform.rect.center.x, 
                rectTransform.rect.yMin);
            scrollRectTransform.position = rectTransform.TransformPoint(bottomCenter);
            float yMin = _safeArea.rect.yMin + _safeArea.position.y;
            scrollRectTransform.sizeDelta = new Vector2(
                rectTransform.rect.width, 
                Mathf.Min(_scroll.content.rect.height + _togglesRect.rect.height, 
                    scrollRectTransform.position.y - yMin));
        }
        CalculateSizes();
    }

    public void OnPointerEnter(PointerEventData eventData) {
        IsMouseOut = false;
    }

    public void OnPointerExit(PointerEventData eventData) {
        IsMouseOut = true;
    }
}
