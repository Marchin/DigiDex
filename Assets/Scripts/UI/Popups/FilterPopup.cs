using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class FilterPopup : Popup {
    [SerializeField] private Button _applyButton = default;
    [SerializeField] private Button _clearButton = default;
    [SerializeField] private Button _closeButton = default;
    [SerializeField] private FilterDataList _filterList = default;
    [SerializeField] private FilterEntryList _filterEntriesList = default;
    private List<FilterData> _filters;
    private Action<List<FilterData>> ApplyCallback;

    private void Awake() {
        _closeButton.onClick.AddListener(() => { PopupManager.Instance.Back(); });
        _applyButton.onClick.AddListener(() => {
            ApplyCallback?.Invoke(_filters);
            gameObject.SetActive(false);
        });
        _clearButton.onClick.AddListener(() => {
            if (_filters != null) {
                for (int iFilter = 0; iFilter < _filters.Count; ++iFilter) {
                    for (int iElement = 0; iElement < _filters[iFilter].Elements.Count; ++iElement) {
                        _filters[iFilter].Elements[iElement].State = FilterState.None;
                    }
                    _filterList.Elements[iFilter].RefreshLabel();
                }
            }
            _filterEntriesList.gameObject.SetActive(false);
        });
    }

    public void Populate(List<FilterData> filters, Action<List<FilterData>> applyCallback) {
        ApplyCallback = applyCallback;
        _filters = new List<FilterData>(filters.Count);
        for (int iFilter = 0; iFilter < filters.Count; ++iFilter) {
            _filters.Add(filters[iFilter].Clone());
            _filters[iFilter].List = _filterEntriesList;
        }
        _filterList.Populate(_filters);

        _filterEntriesList.gameObject.SetActive(false);
        gameObject.SetActive(true);
    }
}
