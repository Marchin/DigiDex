using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using System;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class DatabaseViewPopup : Popup {
    public class PopupData {
        public List<FilterData> Filters;
        public List<ToggleActionData> Toggles;
        public string LastQuery;
        public Database DB;
        public int InspectedIndex;
    }

    [SerializeField] private InputField _searchInput = default;
    [SerializeField] private Button _clearSearch = default;
    [SerializeField] private GameObject _searchIcon = default;
    [SerializeField] private EntryElementList _entryElementList = default;
    [SerializeField] private Button _filterButton = default;
    [SerializeField] private Button _closeButton = default;
    [SerializeField] private GameObject _activeFilterIndicator = default;
    [SerializeField] private GameObject _noEntriesFoundText = default;
    private CancellationTokenSource _entryDataCTS;
    private List<AsyncOperationHandle> _entryDataHandles = new List<AsyncOperationHandle>();
    private List<IDataEntry> _filteredEntries;
    private List<IDataEntry> _currEntries;
    private List<FilterData> _filters;
    private List<ToggleActionData> _toggles;
    private string _lastQuery = "";
    private Database _db;
    private bool _initialized;
    private int _inspectedIndex;
    private int inspectedIndex {
        get => _inspectedIndex;
        set =>_inspectedIndex = UnityUtils.Repeat(value, _currEntries.Count);
    }

    private void Start() {
        _clearSearch.gameObject.SetActive(false);
        _searchIcon.SetActive(true);
        _activeFilterIndicator.gameObject.SetActive(false);

        _filterButton.onClick.AddListener(async () => {
            var popup = await PopupManager.Instance.GetOrLoadPopup<FilterPopup>(restore: false);
            popup.Populate(_filters, _toggles, (filters , toggles) => {
                var dbPopup = PopupManager.Instance.GetLoadedPopupOfType<DatabaseViewPopup>();
                if (dbPopup != null) {
                    dbPopup._filters = filters;
                    dbPopup._toggles = toggles;
                } else {
                    Debug.LogWarning("DatabasePopup not found to apply filter");
                }

                // Filters get applied when the popup stack refreshes
            });
        });

        _searchInput.onValueChanged.AddListener(OnInputChanged);
        _clearSearch.onClick.AddListener(() => _searchInput.text = "");

        _closeButton.onClick.AddListener(() => _ = PopupManager.Instance.Back());

        _initialized = true;
    }

    private void OnInspect(IDataEntry data) {
        inspectedIndex = _currEntries.IndexOf(data);

        PopupManager.Instance.GetOrLoadPopup<EntryViewPopup>(restore: false).ContinueWith(popup => {
            Action prev = null;
            Action next = null;

            if (_currEntries.Count > 1) {
                prev = () => {
                    --inspectedIndex;
                    EntryViewPopup activePopupInstance = PopupManager.Instance.GetLoadedPopupOfType<EntryViewPopup>();
                    activePopupInstance?.Populate(_currEntries[inspectedIndex]);
                };
                next = () => {
                    ++inspectedIndex;
                    EntryViewPopup activePopupInstance = PopupManager.Instance.GetLoadedPopupOfType<EntryViewPopup>();
                    activePopupInstance?.Populate(_currEntries[inspectedIndex]);
                };
            }
            
            popup.Initialize(prev, next);
            popup.Populate(data);
        });
    }

    public void Populate(
        Database database,
        List<FilterData> filters = null,
        List<ToggleActionData> toggles = null,
        string lastQuery = ""
    ) {
        _db = database;
         _db.Entries.Sort((x, y) => x.DisplayName.CompareTo(y.DisplayName));
        _currEntries = new List<IDataEntry>(_db.Entries);
        _filteredEntries = new List<IDataEntry>(_currEntries);
        PopulateList(database.Entries);
        _filters = filters ?? _db.RetrieveFiltersData();
        _toggles = toggles ?? _db.RetrieveTogglesData();
        _lastQuery = lastQuery;
        _searchInput.text = _lastQuery;
        inspectedIndex = 0;
        ReApplyFilterAndRefresh();
    }

    private void OnEnable() {
        PopupManager.Instance.OnStackChange += OnStackChange;
    }

    private void OnDisable() {
        PopupManager.Instance.OnStackChange -= OnStackChange;
    }

    private void Update() {
        foreach (var element in _entryElementList.Elements) {
            element.ScrollingText = _entryElementList.Scroll.velocity.sqrMagnitude <= 0.001f;
        }
    }

    private void OnDestroy() {
        if (_entryDataCTS != null) {
            _entryDataCTS.Cancel();
            _entryDataCTS.Dispose();
            _entryDataCTS = null;
        }

        for (int iHandle = 0; iHandle < _entryDataHandles.Count; ++iHandle) {
            Addressables.Release(_entryDataHandles[iHandle]);
        }
        _entryDataHandles.Clear();
    }

    private void PopulateList(IReadOnlyList<IDataEntry> data) {
        _entryElementList.Populate(data);

        foreach (var element in _entryElementList.Elements) {
            element.ButtonCallback = OnInspect;
        }
    }

    private void OnStackChange() {
        HideKeyboard();

        if (PopupManager.Instance.ActivePopup == this)
        {
            ReApplyFilterAndRefresh();
            _entryElementList.ScrollTo(inspectedIndex);        
        }
    }

    private void HideKeyboard() {
        _searchInput.enabled = false;
        _searchInput.enabled = true;
    }

    private void ReApplyFilterAndRefresh() {
        if (!_initialized || (Vertical != PopupManager.Instance.IsScreenOnPortrait)) {
            return;
        }

        _db.RefreshFilters(ref _filters, ref _toggles);
        _filteredEntries = new List<IDataEntry>(_db.Entries);
        foreach (var toggle in _toggles) {
            _filteredEntries = toggle.Apply(_filteredEntries);
        }

        foreach (var filter in _filters) {
            _filteredEntries = filter.Apply(_filteredEntries);
        }
        
        _activeFilterIndicator.SetActive((_toggles.Find(t => t.IsOn) != null) || 
            (_filters.Find(f => f.Elements.Find(e => e.State != FilterState.None) != null) != null));
        
        RefreshList();
    }

    private void RefreshList() {
        _currEntries.Clear();
        var displayNameStartsWith = new List<IDataEntry>(_filteredEntries.Count);
        var anyNameStartsWith = new List<IDataEntry>(_filteredEntries.Count);
        var displayNameContains = new List<IDataEntry>(_filteredEntries.Count);
        var anyNameContains = new List<IDataEntry>(_filteredEntries.Count);

        for (int iEntry = 0; iEntry < _filteredEntries.Count; ++iEntry) {
            if (_filteredEntries[iEntry].DisplayName.StartsWith(_lastQuery, true, CultureInfo.InvariantCulture)) {
                displayNameStartsWith.Add(_filteredEntries[iEntry]);
            } else if (_filteredEntries[iEntry].Name.StartsWith(_lastQuery, true, CultureInfo.InvariantCulture) || 
                (_filteredEntries[iEntry].DubNames.Find(dn => dn.StartsWith(_lastQuery, true, CultureInfo.InvariantCulture)) != null)
            ) {
                anyNameStartsWith.Add(_filteredEntries[iEntry]);
            } else if (_filteredEntries[iEntry].DisplayName.ToLower().Contains(_lastQuery.ToLower())) {
                displayNameContains.Add(_filteredEntries[iEntry]);
            } else if (_filteredEntries[iEntry].Name.ToLower().Contains(_lastQuery.ToLower()) || 
                (_filteredEntries[iEntry].DubNames.Find(dn => dn.StartsWith(_lastQuery.ToLower())) != null)
            ) {
                anyNameContains.Add(_filteredEntries[iEntry]);
            }
        }

        _currEntries.AddRange(displayNameStartsWith);
        _currEntries.AddRange(anyNameStartsWith);
        _currEntries.AddRange(displayNameContains);
        _currEntries.AddRange(anyNameContains);

        PopulateList(_currEntries);
        
        bool isEmpty = _currEntries.Count == 0;
        _noEntriesFoundText.SetActive(isEmpty);
    }

    private void OnInputChanged(string query) {
        _lastQuery = query;
        _clearSearch.gameObject.SetActive(!string.IsNullOrEmpty(query));
        _searchIcon.SetActive(string.IsNullOrEmpty(query));
        RefreshList();
    }

    public override object GetRestorationData() {
        PopupData data = new PopupData {
            Filters = _filters,
            Toggles = _toggles,
            LastQuery = _lastQuery,
            DB = _db,
            InspectedIndex = inspectedIndex
        };

        return data;
    }

    public override void Restore(object data) {
        if (data is PopupData popupData) {
            Populate(popupData.DB, popupData.Filters, popupData.Toggles, popupData.LastQuery);
            inspectedIndex = popupData.InspectedIndex;
            // _entryElementList.ScrollTo(cu);
        }
    }

    public override void OnClose() {
        for (int iHandle = 0; iHandle < _entryDataHandles.Count; ++iHandle) {
            Addressables.Release(_entryDataHandles[iHandle]);
        }
        _entryDataHandles.Clear();
    }
}
