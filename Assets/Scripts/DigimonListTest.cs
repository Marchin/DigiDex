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

public class DigimonListTest : MonoBehaviour {
    [SerializeField] private Image _digimonImage = default;
    [SerializeField] private TextMeshProUGUI _digimonName = default;
    [SerializeField] private TextMeshProUGUI _digimonProfile = default;
    [SerializeField] private TMP_InputField _searchInput = default;
    [SerializeField] private Button _clearSearch = default;
    [SerializeField] private CustomScrollRect _infoScroll = default;
    [SerializeField] private InformationRowList _info = default;
    [SerializeField] private Button _profileButton = default;
    [SerializeField] private Toggle _dataToggle = default;
    [SerializeField] private Toggle _profileToggle = default;
    [SerializeField] private RectTransform _dataContent = default;
    [SerializeField] private RectTransform _profileContent = default;
    [SerializeField] private Animator _profileAnimator = default;
    [SerializeField] private CentralDatabase _centralDB = default;
    [SerializeField] private ButtonScrollList _animatedScroll = default;
    [SerializeField] private FilterPopup _filterPopup = default;
    [SerializeField] private Button _filterButton = default;
    private DigimonDatabase DigimonDB => _centralDB.DigimonDB;
    private CancellationTokenSource _digimonDataCTS;
    private List<AsyncOperationHandle> _digimonDataHandles = new List<AsyncOperationHandle>();
    private List<Digimon> _filteredDigimonList;
    private List<Digimon> _currDigimonList;
    private List<FilterData> _filters;
    private bool _profileOpen = false;
    private string _lastQuery = "";
    
    private Digimon _selectedDigimon;
    public Digimon SelectedDigimon {
        get => _selectedDigimon;
        private set {
            if (_selectedDigimon == value) {
                return;
            }

            if (_digimonDataCTS != null) {
                _digimonDataCTS.Cancel();
                _digimonDataCTS.Dispose();
            }
            _digimonDataCTS = new CancellationTokenSource();

            for (int iHandle = 0; iHandle < _digimonDataHandles.Count; ++iHandle) {
                Addressables.Release(_digimonDataHandles[iHandle]);
            }
            _digimonDataHandles.Clear();

            _selectedDigimon = value;

            _digimonImage.gameObject.SetActive(false);
            if (_selectedDigimon != null) {
                _animatedScroll.RefreshButtons();
                if (_selectedDigimon.Sprite.RuntimeKeyIsValid()) {
                    var spriteHandle = Addressables.LoadAssetAsync<Sprite>(_selectedDigimon.Sprite);
                    _digimonDataHandles.Add(spriteHandle);
                    spriteHandle.WithCancellation(_digimonDataCTS.Token).ContinueWith(sprite => {
                        if (sprite != null) {
                            _digimonImage.gameObject.SetActive(true);
                            _digimonImage.sprite = sprite;
                        }
                    }).Forget();
                }

                _digimonName.text = _selectedDigimon.Name;
                _digimonProfile.text = _selectedDigimon.ProfileData;

                _info.gameObject.SetActive(false);
                _selectedDigimon.ExtractInformationData(_centralDB).ContinueWith(data => {
                    _info.gameObject.SetActive(true);
                    _info.Populate(data);
                    UniTask.DelayFrame(1).ContinueWith(() => _infoScroll.normalizedPosition = Vector2.up).Forget();
                }).Forget();
            }
        }
    }

    private async void Start() {
        if (DigimonDB == null) {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
            return;
        }

        await Addressables.InitializeAsync();

        _currDigimonList = _filteredDigimonList = DigimonDB.Digimons;
        _animatedScroll.Initialize(
            nameList: _currDigimonList.Select(d => d.Name).ToList(),
            onConfirmed: (index) => {
                if (index >= 0 && index <= _currDigimonList.Count) {
                    SelectedDigimon = _currDigimonList[index];
                    _profileButton.gameObject.SetActive(true);
                } else {
                    SelectedDigimon = null;
                    _profileButton.gameObject.SetActive(false);
                }
            }
        );

        _clearSearch.gameObject.SetActive(false);

        _filters = DigimonDB.RetrieveFilterData();
        _filterPopup.Initialize(filters => {
            _filters = filters;
            Dictionary<string, FilterData> filtersDict = filters.ToDictionary(filter => filter.Name);
            List<int> requiredFields = filtersDict["Fields"].Elements
                .Where(e => e.State == FilterState.Required)
                .Select(e => filtersDict["Fields"].Elements.IndexOf(e))
                .ToList();
            List<int> excludedFields = filtersDict["Fields"].Elements
                .Where(e => e.State == FilterState.Excluded)
                .Select(e => filtersDict["Fields"].Elements.IndexOf(e))
                .ToList();
            List<int> requiredAttributes = filtersDict["Attributes"].Elements
                .Where(e => e.State == FilterState.Required)
                .Select(e => filtersDict["Attributes"].Elements.IndexOf(e))
                .ToList();
            List<int> excludedAttributes = filtersDict["Attributes"].Elements
                .Where(e => e.State == FilterState.Excluded)
                .Select(e => filtersDict["Attributes"].Elements.IndexOf(e))
                .ToList();
            List<int> requiredLevels = filtersDict["Levels"].Elements
                .Where(e => e.State == FilterState.Required)
                .Select(e => filtersDict["Levels"].Elements.IndexOf(e))
                .ToList();
            List<int> excludedLevels = filtersDict["Levels"].Elements
                .Where(e => e.State == FilterState.Excluded)
                .Select(e => filtersDict["Levels"].Elements.IndexOf(e))
                .ToList();
            List<int> requiredTypes = filtersDict["Types"].Elements
                .Where(e => e.State == FilterState.Required)
                .Select(e => filtersDict["Types"].Elements.IndexOf(e))
                .ToList();
            List<int> excludedTypes = filtersDict["Types"].Elements
                .Where(e => e.State == FilterState.Excluded)
                .Select(e => filtersDict["Types"].Elements.IndexOf(e))
                .ToList();

            _filteredDigimonList = DigimonDB.Digimons
                .Where(digimon => (requiredFields.Except(digimon.FieldIDs).Count() == 0) &&
                    !excludedFields.Any(index => digimon.FieldIDs.Contains(index)))
                .Where(digimon => (requiredAttributes.Except(digimon.AttributeIDs).Count() == 0) &&
                    (!excludedAttributes.Any(index => digimon.AttributeIDs.Contains(index))))
                .Where(digimon => (requiredLevels.Except(digimon.LevelIDs).Count() == 0) &&
                    (!excludedLevels.Any(index => digimon.LevelIDs.Contains(index))))
                .Where(digimon => (requiredTypes.Except(digimon.TypeIDs).Count() == 0) &&
                    (excludedTypes.Count == 0 || !excludedTypes.Any(index => digimon.TypeIDs.Contains(index))))
                .ToList();
            
            RefreshList();
        });
        _filterPopup.gameObject.SetActive(false);
        _filterButton.onClick.AddListener(() => _filterPopup.Show(_filters));

        _animatedScroll.OnSelectedButtonChanged += _ => _profileButton.gameObject.SetActive(false);

        _searchInput.onValueChanged.AddListener(OnInputChanged);
        _clearSearch.onClick.AddListener(() => _searchInput.text = "");

        _dataToggle.onValueChanged.AddListener(isOn => _dataContent.gameObject.SetActive(isOn));
        _profileToggle.onValueChanged.AddListener(isOn => _profileContent.gameObject.SetActive(isOn));

        _profileButton.onClick.AddListener(() => {
            if (_profileOpen) {
                _animatedScroll.ScrollEnabled = true;
                _profileAnimator.SetTrigger("Fade Out");
            } else {
                _dataToggle.isOn = true;
                _animatedScroll.ScrollEnabled = false;
                _profileAnimator.SetTrigger("Fade In");
            }
            _profileOpen = !_profileOpen;
        });
    }

    private void RefreshList() {
        _currDigimonList = _filteredDigimonList
            .Where(digimon => digimon.Name.ToLower().Contains(_lastQuery.ToLower()))
            .OrderByDescending(d => d.Name.StartsWith(_lastQuery, true, CultureInfo.InvariantCulture))
            .ToList();

        _animatedScroll.UpdateList(_currDigimonList.Select(d => d.Name).ToList());
    }

    private void OnInputChanged(string query) {
        _lastQuery = query;
        _clearSearch.gameObject.SetActive(!string.IsNullOrEmpty(query));
        RefreshList();
    }

}
