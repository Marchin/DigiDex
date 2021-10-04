using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using FilterCallback = System.Action<System.Collections.Generic.Dictionary<string, FilterData>, System.Collections.Generic.Dictionary<string, ToggleFilterData>>;

public class FilterPopup : Popup {
    class PopupData {
        public Dictionary<string, FilterData> Filters;
        public Dictionary<string, ToggleFilterData> Toggles;
        public FilterCallback ApplyCallback;
    }

    [SerializeField] private Button _applyButton = default;
    [SerializeField] private Button _clearButton = default;
    [SerializeField] private Button _closeButton = default;
    [SerializeField] private FilterDataList _filterList = default;
    [SerializeField] private FilterEntryList _filterEntriesList = default;
    [SerializeField] private ToggleList _toggleList = default;
    private Dictionary<string, FilterData> _filters;
    private Dictionary<string, ToggleFilterData> _toggles;
    private FilterCallback ApplyCallback;

    private void Awake() {
        _closeButton.onClick.AddListener(PopupManager.Instance.Back);
        _applyButton.onClick.AddListener(() => {
            ApplyCallback?.Invoke(_filters, _toggles);
            PopupManager.Instance.Back();
        });
        _clearButton.onClick.AddListener(() => {
            if (_filters != null) {
                foreach (var filter in _filters) {
                    foreach (var element in filter.Value.Elements) {
                        element.State = FilterState.None;
                    }
                }
                _filterList.Populate(_filters.Values);
            }
            if (_toggles != null) {
                foreach (var toggle in _toggles) {
                    toggle.Value.IsOn = false;
                }
                _toggleList.Populate(_toggles.Values);
            }
            _filterEntriesList.gameObject.SetActive(false);
        });
    }

    public void Populate(
        Dictionary<string, FilterData> filters,
        Dictionary<string, ToggleFilterData> toggles,
        FilterCallback applyCallback
    ) {
        ApplyCallback = applyCallback;

        if (filters != null) {
            _filters = new Dictionary<string, FilterData>(filters.Count);
            foreach (var filter in filters) {
                filter.Value.List = _filterEntriesList;
                _filters.Add(filter.Key, filter.Value.Clone());
            }
            _filterList.Populate(_filters.Values.ToList());
        }

        if (toggles != null) {
            _toggles = new Dictionary<string, ToggleFilterData>(toggles.Count);
            foreach (var toggle in toggles) {
                _toggles.Add(toggle.Key, toggle.Value.Clone() as ToggleFilterData);
            }
            _toggleList.Populate(_toggles.Values.ToList());
        }

        _filterEntriesList.gameObject.SetActive(false);
        gameObject.SetActive(true);
    }

    public override object GetRestorationData() {
        PopupData data = new PopupData {
            Filters = _filters,
            Toggles = _toggles,
            ApplyCallback = this.ApplyCallback
        };  

        return data;
    }

    public override void Restore(object data) {
        if (data is PopupData popupData) {
            Populate(
                popupData.Filters, 
                popupData.Toggles, 
                popupData.ApplyCallback);
        }
    }
}
