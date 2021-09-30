using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using System.Linq;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;

public class DatabaseViewPopup : Popup {
    [SerializeField] private Image _entryImage = default;
    [SerializeField] private TMP_InputField _searchInput = default;
    [SerializeField] private Button _clearSearch = default;
    [SerializeField] private GameObject _searchIcon = default;
    [SerializeField] private Button _profileButton = default;
    [SerializeField] private ElementScrollList _elementScrollList = default;
    [SerializeField] private Button _filterButton = default;
    private CancellationTokenSource _entryDataCTS;
    private List<AsyncOperationHandle> _entryDataHandles = new List<AsyncOperationHandle>();
    private IEnumerable<IDataEntry> _filteredEntries;
    private IEnumerable<IDataEntry> _currEntries;
    private Dictionary<string, FilterData> _filters;
    private Dictionary<string, ToggleFilterData> _toggles;
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
            if (_selectedEntry != null) {
                if (_selectedEntry.Sprite.RuntimeKeyIsValid()) {
                    var spriteHandle = Addressables.LoadAssetAsync<Sprite>(_selectedEntry.Sprite);
                    _entryDataHandles.Add(spriteHandle);
                    spriteHandle.WithCancellation(_entryDataCTS.Token).ContinueWith(sprite => {
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

        _filterButton.onClick.AddListener(async () => {
            var popup = await PopupManager.Instance.GetOrLoadPopup<FilterPopup>();
            popup.Populate(_filters, _toggles, (filters , toggles) => {
                _filters = filters;
                _toggles = toggles;

                // Filters get applied when the popup stack refreshes
            });
        });

        _elementScrollList.OnSelectedElementChanged += _ => {
            _profileButton.gameObject.SetActive(false);
        };

        _searchInput.onValueChanged.AddListener(OnInputChanged);
        _clearSearch.onClick.AddListener(() => _searchInput.text = "");

        _profileButton.onClick.AddListener(() => {
            PopupManager.Instance.GetOrLoadPopup<EntryViewPopup>().ContinueWith(popup => {
                popup.Populate(SelectedEntry);
            });
        });

        _initialized = true;
    }

    public void Populate(IDatabase database) {
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
                }
            }
        );

        _entryDict = _db.EntryList.ToDictionary(e => e.Hash);
        _toggles = _db.RetrieveTogglesData();
        _filters = _db.RetrieveFiltersData();
        ReApplyFilterAndRefresh();
    }

    private void OnEnable() {
        PopupManager.Instance.OnStackChange += ReApplyFilterAndRefresh;
    }

    private void OnDisable() {
        PopupManager.Instance.OnStackChange -= ReApplyFilterAndRefresh;
    }

    private void ReApplyFilterAndRefresh() {
        if (!_initialized) {
            return;
        }

        if (PopupManager.Instance.ActivePopup == this) {
            _filteredEntries = new List<IDataEntry>(_db.EntryList);
            foreach (var toggle in _toggles) {
                _filteredEntries = toggle.Value.Apply(_filteredEntries);
            }

            foreach (var filter in _filters) {
                _filteredEntries = filter.Value.Apply(_filteredEntries);
            }

            
            _elementScrollList.ScrollEnabled = true;
            RefreshList();
        } else {
            _elementScrollList.ScrollEnabled = false;
        }
    }

    private void RefreshList() {
        _currEntries = _filteredEntries
            .Where(entry => entry.Name.ToLower().Contains(_lastQuery.ToLower()))
            .OrderByDescending(e => e.Name.StartsWith(_lastQuery, true, CultureInfo.InvariantCulture))
            .ToList();

        _elementScrollList.UpdateList(_currEntries.Select(e => e.Name).ToList());
    }

    private void OnInputChanged(string query) {
        _lastQuery = query;
        _clearSearch.gameObject.SetActive(!string.IsNullOrEmpty(query));
        _searchIcon.SetActive(string.IsNullOrEmpty(query));
        RefreshList();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            PopupManager.Instance.Back();
        }
    }
}
