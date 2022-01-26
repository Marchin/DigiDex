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
        public Database DB;
        public string SelectedEntry;
    }

    [SerializeField] private Image _entryImage = default;
    [SerializeField] private InputField _searchInput = default;
    [SerializeField] private Button _clearSearch = default;
    [SerializeField] private GameObject _searchIcon = default;
    [SerializeField] private Button _profileButton = default;
    [SerializeField] private ElementScrollList _elementScrollList = default;
    [SerializeField] private Button _filterButton = default;
    [SerializeField] private Button _closeButton = default;
    [SerializeField] private GameObject _activeFilterIndicator = default;
    [SerializeField] private GameObject _loadingWheel = default;
    [SerializeField] private GameObject _noEntriesFoundText = default;
    [SerializeField] private GameObject _highlighter = default;
    private CancellationTokenSource _entryDataCTS;
    private List<AsyncOperationHandle> _entryDataHandles = new List<AsyncOperationHandle>();
    private IEnumerable<IDataEntry> _filteredEntries;
    private List<IDataEntry> _currEntries;
    private IEnumerable<FilterData> _filters;
    private IEnumerable<ToggleActionData> _toggles;
    private string _lastQuery = "";
    private IEnumerable<IDataEntry> _entries;
    private Database _db;
    private bool _initialized;
    private IDataEntry _selectedEntry;
    public IDataEntry SelectedEntry {
        get => _selectedEntry;
        private set {
            if (value != null && _selectedEntry == value) {
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
            if (_selectedEntry != null) {
                _loadingWheel.SetActive(true);
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
            } else {
                _loadingWheel.SetActive(false);
                var spriteHandle = Addressables.LoadAssetAsync<Sprite>(ApplicationManager.Instance.MissingSpirte);
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

        _closeButton.onClick.AddListener(() => _ = PopupManager.Instance.Back());
        _profileButton.onClick.AddListener(() => {
            PopupManager.Instance.GetOrLoadPopup<EntryViewPopup>(restore: false).ContinueWith(popup => {
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
        Database database,
        IEnumerable<FilterData> filters = null,
        IEnumerable<ToggleActionData> toggles = null,
        string lastQuery = ""
    ) {
        _db = database;
        _filteredEntries = _entries = _db.Entries.OrderBy(e => e.DisplayName);
        _currEntries = _entries.ToList();
        _elementScrollList.Initialize(
            nameList: _currEntries.Select(e => e.DisplayName).ToList(),
            onConfirmed: (index) => {
                int count = _currEntries.Count();
                if (index >= 0 && count > 0 && index <= count) {
                    SelectedEntry = _currEntries[index];
                    _profileButton.gameObject.SetActive(true);
                } else {
                    SelectedEntry = null;
                    _profileButton.gameObject.SetActive(false);
                }
            }
        );
        _filters = filters ?? _db.RetrieveFiltersData();
        _toggles = toggles ?? _db.RetrieveTogglesData();
        _lastQuery = lastQuery;
        _searchInput.text = _lastQuery;
        ReApplyFilterAndRefresh();
    }

    private void OnEnable() {
        PopupManager.Instance.OnStackChange += OnStackChange;
    }

    private void OnDisable() {
        PopupManager.Instance.OnStackChange -= OnStackChange;
    }

    private void OnStackChange() {
        ReApplyFilterAndRefresh();
        HideKeyboard();
        _elementScrollList.enabled = PopupManager.Instance.ActivePopup == this;        
    }

    private void HideKeyboard() {
        _searchInput.enabled = false;
        _searchInput.enabled = true;
    }

    private void ReApplyFilterAndRefresh() {
        if (!_initialized || (Vertical != PopupManager.Instance.IsScreenOnPortrait)) {
            return;
        }

        if (PopupManager.Instance.ActivePopup == this) {
            _db.RefreshFilters(ref _filters, ref _toggles);
            _filteredEntries = new List<IDataEntry>(_entries);
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
            .Where(entry => entry.DisplayName.StartsWith(_lastQuery, true, CultureInfo.InvariantCulture))
            .Concat(_filteredEntries.Where(entry => entry.Name.StartsWith(_lastQuery, true, CultureInfo.InvariantCulture) || entry.DubNames.Any(dn => dn.StartsWith(_lastQuery, true, CultureInfo.InvariantCulture))))
            .Concat(_filteredEntries.Where(entry => entry.DisplayName.ToLower().Contains(_lastQuery.ToLower())))
            .Concat(_filteredEntries.Where(entry => entry.Name.ToLower().Contains(_lastQuery.ToLower()) || entry.DubNames.Any(dn => dn.ToLower().Contains(_lastQuery.ToLower()))))
            .Distinct()
            .ToList(); 

        var entryList = _currEntries.Select(e => e.DisplayName).ToList();
        _elementScrollList.UpdateList(entryList);

        bool isEmpty = entryList.Count == 0;
        _noEntriesFoundText.SetActive(isEmpty);
        _highlighter.SetActive(!isEmpty);
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
            SelectedEntry = SelectedEntry?.DisplayName
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
