using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;

public class DatabaseViewPopup : Popup {
    public class PopupData {
        public IEnumerable<FilterData> Filters;
        public IEnumerable<ToggleActionData> Toggles;
        public string LastQuery;
        public IDatabase DB;
        public string SelectedEntry;
    }

    [SerializeField] private Image _entryImage = default;
    [SerializeField] private TMP_InputField _searchInput = default;
    [SerializeField] private Button _clearSearch = default;
    [SerializeField] private GameObject _searchIcon = default;
    [SerializeField] private Button _profileButton = default;
    [SerializeField] private ElementScrollList _elementScrollList = default;
    [SerializeField] private Button _filterButton = default;
    [SerializeField] private Button _closeButton = default;
    [SerializeField] private GameObject _activeFilterIndicator = default;
    [SerializeField] private GameObject _loadingWheel = default;
    private CancellationTokenSource _entryDataCTS;
    private List<AsyncOperationHandle> _entryDataHandles = new List<AsyncOperationHandle>();
    private IEnumerable<IDataEntry> _filteredEntries;
    private IEnumerable<IDataEntry> _currEntries;
    private IEnumerable<FilterData> _filters;
    private IEnumerable<ToggleActionData> _toggles;
    private string _lastQuery = "";
    private Dictionary<Hash128, IDataEntry> _entryDict;
    private IDatabase _db;
    private bool _initialized;
    private IDataEntry _selectedEntry;
    public IDataEntry SelectedEntry {
        get => _selectedEntry;
        private set {
            if (_selectedEntry == value) {
                return;
            }

            if (_entryDataCTS != null) {
                _entryDataCTS.Cancel();
                _entryDataCTS.Dispose();
            }
            _entryDataCTS = new CancellationTokenSource();

            for (int iHandle = 0; iHandle < _entryDataHandles.Count; ++iHandle) {
                Addressables.Release(_entryDataHandles[iHandle]);
            }
            _entryDataHandles.Clear();

            _selectedEntry = value;

            _entryImage.gameObject.SetActive(false);
            _loadingWheel.SetActive(true);
            if (_selectedEntry != null) {
                if (_selectedEntry.Sprite.RuntimeKeyIsValid()) {
                    var spriteHandle = Addressables.LoadAssetAsync<Sprite>(_selectedEntry.Sprite);
                    _entryDataHandles.Add(spriteHandle);
                    spriteHandle.WithCancellation(_entryDataCTS.Token).ContinueWith(sprite => {
                        _loadingWheel.SetActive(false);
                        if (sprite != null) {
                            _entryImage.gameObject.SetActive(true);
                            _entryImage.sprite = sprite;
                        }
                    }).Forget();
                }
            }
        }
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

        _elementScrollList.OnSelectedElementChanged += _ => {
            _profileButton.gameObject.SetActive(false);
        };

        _searchInput.onValueChanged.AddListener(OnInputChanged);
        _clearSearch.onClick.AddListener(() => _searchInput.text = "");

        _closeButton.onClick.AddListener(PopupManager.Instance.Back);
        _profileButton.onClick.AddListener(() => {
            PopupManager.Instance.GetOrLoadPopup<EntryViewPopup>().ContinueWith(popup => {
                Action prev = null;
                Action next = null;

                if (_currEntries.Count() > 1) {
                    prev = () => {
                        --_elementScrollList.CurrentIndex;
                        EntryViewPopup activePopupInstance = PopupManager.Instance.GetLoadedPopupOfType<EntryViewPopup>();
                        activePopupInstance?.Populate(SelectedEntry);
                    };
                    next = () => {
                        ++_elementScrollList.CurrentIndex;
                        EntryViewPopup activePopupInstance = PopupManager.Instance.GetLoadedPopupOfType<EntryViewPopup>();
                        activePopupInstance?.Populate(SelectedEntry);
                    };
                }
                
                popup.Initialize(prev, next);
                popup.Populate(SelectedEntry);
            });
        });

        _initialized = true;
    }

    public void Populate(
        IDatabase database,
        IEnumerable<FilterData> filters = null,
        IEnumerable<ToggleActionData> toggles = null,
        string lastQuery = ""
    ) {
        _db = database;
        _currEntries = _filteredEntries = database.EntryList;
        _elementScrollList.Initialize(
            nameList: _currEntries.Select(e => e.Name).ToList(),
            onConfirmed: (index) => {
                if (index >= 0 && index <= _currEntries.Count()) {
                    SelectedEntry = _currEntries.ElementAt(index);
                    _profileButton.gameObject.SetActive(true);
                } else {
                    SelectedEntry = null;
                    _profileButton.gameObject.SetActive(false);
                }
            }
        );

        _entryDict = _db.EntryList.ToDictionary(e => e.Hash);
        _filters = filters ?? _db.RetrieveFiltersData();
        _toggles = toggles ?? _db.RetrieveTogglesData();
        _lastQuery = lastQuery;
        _searchInput.text = _lastQuery;
        ReApplyFilterAndRefresh();
    }

    private void OnEnable() {
        PopupManager.Instance.OnStackChange += ReApplyFilterAndRefresh;
    }

    private void OnDisable() {
        PopupManager.Instance.OnStackChange -= ReApplyFilterAndRefresh;
    }

    private void ReApplyFilterAndRefresh() {
        if (!_initialized || (Vertical != PopupManager.Instance.IsScreenOnPortrait)) {
            return;
        }

        if (PopupManager.Instance.ActivePopup == this) {
            _db.RefreshFilters(ref _filters, ref _toggles);
            _filteredEntries = new List<IDataEntry>(_db.EntryList);
            foreach (var toggle in _toggles) {
                _filteredEntries = toggle.Apply(_filteredEntries);
            }

            foreach (var filter in _filters) {
                _filteredEntries = filter.Apply(_filteredEntries);
            }

            _activeFilterIndicator.SetActive(_toggles.Any(t => t.IsOn) || 
                _filters.Any(f => f.Elements.Any(e => e.State != FilterState.None)));
            
            _elementScrollList.ScrollEnabled = true;
            RefreshList();
        } else {
            _elementScrollList.ScrollEnabled = false;
        }
    }

    private void RefreshList() {
        _currEntries = _filteredEntries
            .Where(entry => entry.Name.StartsWith(_lastQuery, true, CultureInfo.InvariantCulture))
            .Concat(_filteredEntries.Where(entry => entry.DubNames.Any(dn => dn.StartsWith(_lastQuery, true, CultureInfo.InvariantCulture))))
            .Concat(_filteredEntries.Where(entry => entry.Name.ToLower().Contains(_lastQuery.ToLower())))
            .Concat(_filteredEntries.Where(entry => entry.DubNames.Any(dn => dn.ToLower().Contains(_lastQuery.ToLower()))))
            .Distinct()
            .ToList();

        _elementScrollList.UpdateList(_currEntries.Select(e => e.Name).ToList());
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
            SelectedEntry = SelectedEntry.Name
        };

        return data;
    }

    public async override void Restore(object data) {
        if (data is PopupData popupData) {
            Populate(popupData.DB, popupData.Filters, popupData.Toggles, popupData.LastQuery);
            await UniTask.DelayFrame(ElementScrollList.FrameDelayToAnimateList + 1);
            _elementScrollList.ScrollTo(popupData.SelectedEntry, withAnimation: true);
        }
    }

    public override void OnClose() {
        for (int iHandle = 0; iHandle < _entryDataHandles.Count; ++iHandle) {
            Addressables.Release(_entryDataHandles[iHandle]);
        }
        _entryDataHandles.Clear();
    }
}
