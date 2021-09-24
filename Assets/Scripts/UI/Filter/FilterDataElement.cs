using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using System.Collections.Generic;

public class FilterData {
    public string Name;
    public List<FilterEntryData> Elements;
    public FilterEntryList List;
    private Func<IDataObject, List<int>> _getFilteringComponent;

    public FilterData(string name, Func<IDataObject, List<int>> getFilteringComponent) {
        Name = name;
        _getFilteringComponent = getFilteringComponent;
    }

    public FilterData Clone() {
        FilterData newFilter = new FilterData(Name, _getFilteringComponent);

        newFilter.List = this.List;
        newFilter.Elements = new List<FilterEntryData>(Elements.Count);
        for (int iElement = 0; iElement < Elements.Count; ++iElement) {
            newFilter.Elements.Add(Elements[iElement].Clone());
        }

        return newFilter;
    }

    public List<T> Apply<T>(List<T> list) where T : IDataObject {
        List<int> requiredLevels = this.Elements
            .Where(e => e.State == FilterState.Required)
            .Select(e => this.Elements.IndexOf(e))
            .ToList();
        List<int> excludedLevels = this.Elements
            .Where(e => e.State == FilterState.Excluded)
            .Select(e => this.Elements.IndexOf(e))
            .ToList();

        var filteredList = list
            .Where(element => {
                List<int> filteringComponent = _getFilteringComponent?.Invoke(element);

                return (requiredLevels.Except(filteringComponent).Count() == 0) &&
                    !excludedLevels.Any(index => filteringComponent.Contains(index));
            }).ToList();

        return filteredList;
    }
}

public class ToggleFilterData : ToggleData {
    public Func<IEnumerable<IDataObject>, bool, IEnumerable<IDataObject>> _filterAction;

    public ToggleFilterData(string name, Func<IEnumerable<IDataObject>, bool, IEnumerable<IDataObject>> filterAction) : base(name) {
        _filterAction = filterAction;
    }

    public override object Clone() {
        return this.MemberwiseClone();
    }

    public IEnumerable<T> Apply<T>(IEnumerable<T> list) where T : IDataObject {
        // TODO: See if we can do something better with the casting
        var filteredList = _filterAction?.Invoke(list.Cast<IDataObject>(), IsOn).Cast<T>();

        return filteredList;
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
