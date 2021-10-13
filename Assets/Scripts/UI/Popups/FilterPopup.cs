using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using FilterCallback = System.Action<System.Collections.Generic.IEnumerable<FilterData>, System.Collections.Generic.IEnumerable<ToggleActionData>>;

public class FilterPopup : Popup {
    class PopupData {
        public IEnumerable<FilterData> Filters;
        public IEnumerable<ToggleActionData> Toggles;
        public FilterCallback ApplyCallback;
    }

    [SerializeField] private Button _applyButton = default;
    [SerializeField] private Button _clearButton = default;
    [SerializeField] private Button _closeButton = default;
    [SerializeField] private FilterDataList _filterList = default;
    [SerializeField] private FilterEntryList _filterEntriesList = default;
    [SerializeField] private ToggleList _toggleList = default;
    private IEnumerable<FilterData> _filters;
    private IEnumerable<ToggleActionData> _toggles;
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
        IEnumerable<FilterData> filters,
        IEnumerable<ToggleActionData> toggles,
        FilterCallback applyCallback
    ) {
        ApplyCallback = applyCallback;

        if (filters != null) {
            _filters = new List<FilterData>(filters.Count());
            foreach (var filter in filters) {
                filter.List = _filterEntriesList;
                _filters = _filters.Append(filter.Clone());
            }
            _filterList.Populate(_filters);
        }

        if (toggles != null) {
            _toggles = new List<ToggleActionData>(toggles.Count());
            foreach (var toggle in toggles) {
                _toggles = _toggles.Append(toggle.Clone() as ToggleActionData);
            }
            _toggleList.Populate(_toggles);
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
