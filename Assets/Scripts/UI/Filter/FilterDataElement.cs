using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections.Generic;

public class FilterData {
    public string Name;
    public List<FilterEntryData> Elements;
    public FilterEntryList List;

    public FilterData Clone() {
        FilterData newFilter = new FilterData();

        newFilter.Name = Name;
        newFilter.Elements = new List<FilterEntryData>(Elements.Count);
        for (int iElement = 0; iElement < Elements.Count; ++iElement) {
            newFilter.Elements.Add(Elements[iElement].Clone());
        }
        newFilter.List = List;

        return newFilter;
    }
}

public class FilterDataElement : MonoBehaviour, IDataUIElement<FilterData>, IPointerEnterHandler, IPointerExitHandler {
    [SerializeField] private Button _button = default;
    [SerializeField] private TextMeshProUGUI _label = default;
    private FilterData _filterData;

    private void Awake() {
        _button.onClick.AddListener(() => {
            if (_filterData == null || _filterData.Elements == null || _filterData.List == null) {
                return;
            }

            _filterData.List.LastCaller = this;
            if (_filterData.List.LastCaller == this && _filterData.List.gameObject.activeSelf) {
                _filterData.List.gameObject.SetActive(false);
            } else {
                RectTransform rectTransform = transform as RectTransform;
                RectTransform scrollRectTransform = _filterData.List.transform as RectTransform;
                scrollRectTransform.sizeDelta = new Vector2(rectTransform.rect.width, scrollRectTransform.rect.height);
                if (rectTransform.position.y < (0.5f * Screen.height)) {
                    scrollRectTransform.pivot = new Vector2(0.5f, 0f);
                    Vector2 topCenter = new Vector2(
                        rectTransform.rect.center.x,
                        rectTransform.rect.yMax
                    );
                    scrollRectTransform.position = rectTransform.TransformPoint(topCenter);
                } else {
                    scrollRectTransform.pivot = new Vector2(0.5f, 1f);
                    Vector2 bottomCenter = new Vector2(
                        rectTransform.rect.center.x,
                        rectTransform.rect.yMin
                    );
                    scrollRectTransform.position = rectTransform.TransformPoint(bottomCenter);
                }

                _filterData.List.Populate(_filterData.Elements);
                _filterData.List.gameObject.SetActive(true);
                _filterData.List.ResetScroll();
            }
        });
    }

    public void Populate(FilterData data) {
        _filterData = data;
        RefreshLabel();

        for (int iElement = 0; iElement < _filterData.Elements.Count; ++iElement) {
            _filterData.Elements[iElement].OnStateChange = _ => { RefreshLabel(); };
        }
    }

    public void RefreshLabel() {
        _label.text = _filterData.Name;
        
        if (_filterData != null && _filterData.Elements != null) {
            int activeFilters = _filterData.Elements.Where(e => e.State != FilterState.None).Count();
            if (activeFilters > 0) {
                _label.text += $" ({activeFilters})";
            }
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData) {
        if (_filterData.List.LastCaller == this) {
            _filterData.List.IsMouseOut = false;
        }
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (_filterData.List.LastCaller == this) {
            _filterData.List.IsMouseOut = true;
        }
    }
}
