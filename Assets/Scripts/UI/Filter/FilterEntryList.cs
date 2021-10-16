using UnityEngine;
using UnityEngine.EventSystems;

public class FilterEntryList : DataList<FilterEntryElement, FilterEntryData>, IPointerEnterHandler, IPointerExitHandler {
    [SerializeField] private float _hideInSecs = 2f;
    [System.NonSerialized] public UnityEngine.Object LastCaller;
    [System.NonSerialized] public bool IsMouseOut;
    private float accum = 0f;
    private bool _wasScrolling;
    public float OriginalScrollHeight { get; private set; }

    private void Awake() {
        RectTransform scrollRectTransform = transform as RectTransform;
        OriginalScrollHeight = scrollRectTransform.rect.height;
        OnPopulate += _ => {
            IsMouseOut = false;
            accum = 0f;
        };
    }

    public void ResetScroll() {
        _scroll.verticalNormalizedPosition = 1f;
    }

    private void Update() {
        if (false && IsMouseOut && !Application.isMobilePlatform) {
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

    public void OnPointerEnter(PointerEventData eventData) {
        IsMouseOut = false;
    }

    public void OnPointerExit(PointerEventData eventData) {
        IsMouseOut = true;
    }
}
