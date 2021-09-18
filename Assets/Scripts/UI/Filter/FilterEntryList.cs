using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class FilterEntryList : DataList<FilterEntryElement, FilterEntryData>, IPointerEnterHandler, IPointerExitHandler {
    [SerializeField] private float _hideInSecs = 1f;
    [SerializeField] private ScrollRect _scroll = default;
    [System.NonSerialized] public UnityEngine.Object LastCaller;
    [System.NonSerialized] public bool IsMouseOut;
    private float accum = 0f;

    private void Awake() {
        OnPopulate += _ => {
            IsMouseOut = false;
            accum = 0f;
        };
    }

    public void ResetScroll() {
        _scroll.verticalNormalizedPosition = 1f;
    }

    private void Update() {
        if (IsMouseOut) {
            accum += Time.unscaledDeltaTime;
            if (accum >= _hideInSecs) {
                accum = 0f;
                gameObject.SetActive(false);
            }
        } else {
            accum = 0;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) {
        IsMouseOut = false;
    }

    public void OnPointerExit(PointerEventData eventData) {
        IsMouseOut = true;
    }
}
