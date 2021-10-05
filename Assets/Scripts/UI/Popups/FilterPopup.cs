using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using FilterCallback = System.Action<System.Collections.Generic.List<FilterData>, System.Collections.Generic.List<ToggleFilterData>>;

public class FilterPopup : Popup {
    class PopupData {
        public List<FilterData> Filters;
        public List<ToggleFilterData> Toggles;
        public FilterCallback ApplyCallback;
    }

    [SerializeField] private Button _applyButton = default;
    [SerializeField] private Button _clearButton = default;
    [SerializeField] private Button _closeButton = default;
    [SerializeField] private FilterDataList _filterList = default;
    [SerializeField] private FilterEntryList _filterEntriesList = default;
    [SerializeField] private ToggleList _toggleList = default;
    private List<FilterData> _filters;
    private List<ToggleFilterData> _toggles;
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
                    foreach (var element in filter.Elements) {
                        element.State = FilterState.None;
                    }
                }
                _filterList.Populate(_filters);
            }
            if (_toggles != null) {
                foreach (var toggle in _toggles) {
                    toggle.IsOn = false;
                }
                _toggleList.Populate(_toggles);
            }
            _filterEntriesList.gameObject.SetActive(false);
        });
    }

    public void Populate(
        List<FilterData> filters,
        List<ToggleFilterData> toggles,
        FilterCallback applyCallback
    ) {
        ApplyCallback = applyCallback;

        if (filters != null) {
            _filters = new List<FilterData>(filters.Count);
            foreach (var filter in filters) {
                filter.List = _filterEntriesList;
                _filters.Add(filter.Clone());
            }
            _filterList.Populate(_filters.ToList());
        }

        if (toggles != null) {
            _toggles = new List<ToggleFilterData>(toggles.Count);
            foreach (var toggle in toggles) {
                _toggles.Add(toggle.Clone() as ToggleFilterData);
            }
            _toggleList.Populate(_toggles.ToList());
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
