using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using FilterCallback = System.Action<System.Collections.Generic.List<FilterData>, System.Collections.Generic.List<ToggleActionData>>;

public class FilterPopup : Popup {
    class PopupData {
        public List<FilterData> Filters;
        public List<ToggleActionData> Toggles;
        public FilterCallback ApplyCallback;
    }

    [SerializeField] private Button _applyButton = default;
    [SerializeField] private Button _clearButton = default;
    [SerializeField] private Button _closeButton = default;
    [SerializeField] private FilterDataList _filterList = default;
    [SerializeField] private FilterEntryList _filterEntriesList = default;
    [SerializeField] private ToggleList _toggleList = default;
    private List<FilterData> _filters;
    private List<ToggleActionData> _toggles;
    private FilterCallback ApplyCallback;

    private void Awake() {
        _closeButton.onClick.AddListener(() => _ = PopupManager.Instance.Back());
        _applyButton.onClick.AddListener(() => {
            ApplyCallback?.Invoke(_filters, _toggles);
            _ = PopupManager.Instance.Back();
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
                List<ToggleData> togglesData = new List<ToggleData>(_toggles.Count);
                foreach (var toggle in _toggles) {
                    toggle.IsOn = false;
                    togglesData.Add(toggle);
                }
                _toggleList.Populate(togglesData);
            }
            _filterEntriesList.gameObject.SetActive(false);
        });
    }

    public void Populate(
        List<FilterData> filters,
        List<ToggleActionData> toggles,
        FilterCallback applyCallback
    ) {
        ApplyCallback = applyCallback;

        if (filters != null) {
            _filters = new List<FilterData>(filters.Count);
            foreach (var filter in filters) {
                filter.List = _filterEntriesList;
                _filters.Add(filter.Clone());
            }
            _filterList.Populate(_filters);
        }

        if (toggles != null) {
            _toggles = new List<ToggleActionData>(toggles.Count);
            List<ToggleData> togglesData = new List<ToggleData>(_toggles.Count);
            foreach (var toggle in toggles) {
                var toggleData = toggle.Clone() as ToggleActionData;
                _toggles.Add(toggleData);
                togglesData.Add(toggleData);
            }
            _toggleList.Populate(togglesData);
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
