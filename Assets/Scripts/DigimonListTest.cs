﻿using TMPro;
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
    [SerializeField] private Toggle _favoriteButton = default;
    [SerializeField] private Button _evolutionButton = default;
    [SerializeField] private Button _filterButton = default;
    public DigimonDatabase DigimonDB => _centralDB.DigimonDB;
    private CancellationTokenSource _digimonDataCTS;
    private List<AsyncOperationHandle> _digimonDataHandles = new List<AsyncOperationHandle>();
    private List<Digimon> _filteredDigimonList;
    private List<Digimon> _currDigimonList;
    private Dictionary<string, FilterData> _filters;
    private Dictionary<string, ToggleData> _toggles;
    private bool _profileOpen = false;
    private string _lastQuery = "";
    private static DigimonListTest _instance;
    public static DigimonListTest Instance => _instance;
    private Dictionary<Hash128, Digimon> _digimonDict;
    private HashSet<Hash128> _favoriteDigimons;
    
    private EvolutionData _currEvolutionData;
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

                if (_selectedDigimon.EvolutionData.RuntimeKeyIsValid()) {
                    var evolutionHandle = Addressables.LoadAssetAsync<EvolutionData>(_selectedDigimon.EvolutionData);
                    _digimonDataHandles.Add(evolutionHandle);
                    evolutionHandle.WithCancellation(_digimonDataCTS.Token).ContinueWith(evolutionData => {
                        if (evolutionData != null) {
                            _evolutionButton.gameObject.SetActive(true);
                            _evolutionButton.interactable = (evolutionData.PreEvolutions.Count > 0 || evolutionData.Evolutions.Count > 0);
                            _currEvolutionData = evolutionData;
                        }
                    });
                }

                _digimonName.text = _selectedDigimon.Name;
                _digimonProfile.text = _selectedDigimon.ProfileData;
                _favoriteButton.isOn = _favoriteDigimons.Contains(_selectedDigimon.Hash);

                List<InformationData> informationData = _selectedDigimon.ExtractInformationData(_centralDB);
                _info.Populate(informationData);
                UniTask.DelayFrame(1).ContinueWith(() => _infoScroll.normalizedPosition = Vector2.up).Forget();
            }
        }
    }

    private void Awake() {
        if (_instance != null && _instance != this) {
            Destroy(this);
        } else {
            _instance = this;
        }
    }

    private async void Start() {
        if (DigimonDB == null) {
            UnityUtils.Quit();
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
                    _favoriteButton.gameObject.SetActive(true);
                } else {
                    SelectedDigimon = null;
                    _favoriteButton.gameObject.SetActive(false);
                }
            }
        );

        _digimonDict = DigimonDB.Digimons.ToDictionary(d => d.Hash);
        _favoriteDigimons = DigimonDB.LoadFavorites();

        _clearSearch.gameObject.SetActive(false);

        _toggles = DigimonDB.RetrieveTogglesData();
        _filters = DigimonDB.RetrieveFiltersData();
        _filterButton.onClick.AddListener(async () => {
            var popup = await PopupManager.Instance.GetOrLoadPopup<FilterPopup>();
            popup.Populate(_filters, _toggles, (filters , toggles) => {
                _filters = filters;
                _toggles = toggles;
                List<int> requiredFields = _filters[DigimonDatabase.FieldsFilter].Elements
                    .Where(e => e.State == FilterState.Required)
                    .Select(e => _filters[DigimonDatabase.FieldsFilter].Elements.IndexOf(e))
                    .ToList();
                List<int> excludedFields = _filters[DigimonDatabase.FieldsFilter].Elements
                    .Where(e => e.State == FilterState.Excluded)
                    .Select(e => _filters[DigimonDatabase.FieldsFilter].Elements.IndexOf(e))
                    .ToList();
                List<int> requiredAttributes = _filters[DigimonDatabase.AttributesFilter].Elements
                    .Where(e => e.State == FilterState.Required)
                    .Select(e => _filters[DigimonDatabase.AttributesFilter].Elements.IndexOf(e))
                    .ToList();
                List<int> excludedAttributes = _filters[DigimonDatabase.AttributesFilter].Elements
                    .Where(e => e.State == FilterState.Excluded)
                    .Select(e => _filters[DigimonDatabase.AttributesFilter].Elements.IndexOf(e))
                    .ToList();
                List<int> requiredLevels = _filters[DigimonDatabase.LevelsFilter].Elements
                    .Where(e => e.State == FilterState.Required)
                    .Select(e => _filters[DigimonDatabase.LevelsFilter].Elements.IndexOf(e))
                    .ToList();
                List<int> excludedLevels = _filters[DigimonDatabase.LevelsFilter].Elements
                    .Where(e => e.State == FilterState.Excluded)
                    .Select(e => _filters[DigimonDatabase.LevelsFilter].Elements.IndexOf(e))
                    .ToList();
                List<int> requiredTypes = _filters[DigimonDatabase.TypesFilter].Elements
                    .Where(e => e.State == FilterState.Required)
                    .Select(e => _filters[DigimonDatabase.TypesFilter].Elements.IndexOf(e))
                    .ToList();
                List<int> excludedTypes = _filters[DigimonDatabase.TypesFilter].Elements
                    .Where(e => e.State == FilterState.Excluded)
                    .Select(e => _filters[DigimonDatabase.TypesFilter].Elements.IndexOf(e))
                    .ToList();

                _filteredDigimonList = new List<Digimon>(DigimonDB.Digimons);

                foreach (var toggle in toggles) {
                    switch (toggle.Key) {
                        case DigimonDatabase.FavoritesToggle: {
                           if (toggle.Value.IsOn) {
                                _filteredDigimonList = _filteredDigimonList
                                    .Where(d => _favoriteDigimons.Contains(d.Hash))
                                    .ToList();
                            }
                        } break;
                        case DigimonDatabase.ReverseToggle: {
                           if (toggle.Value.IsOn) {
                                _filteredDigimonList.Reverse();
                            }
                        } break;
                    }
                }

                _filteredDigimonList = _filteredDigimonList
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
        });

        _evolutionButton.onClick.AddListener(async () => {
            var popup = await PopupManager.Instance.GetOrLoadPopup<EvolutionsPopup>();
            popup.Populate(SelectedDigimon, _currEvolutionData);
        });

        _animatedScroll.OnSelectedButtonChanged += _ => {
            _profileButton.gameObject.SetActive(false);
            _favoriteButton.gameObject.SetActive(false);
            _evolutionButton.gameObject.SetActive(false);
        };

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

        _favoriteButton.onValueChanged.AddListener(isOn => {
            if (isOn) {
                _favoriteDigimons.Add(SelectedDigimon.Hash);
            } else {
                _favoriteDigimons.Remove(SelectedDigimon.Hash);
                if (_toggles[DigimonDatabase.FavoritesToggle].IsOn) {
                    _filteredDigimonList.Remove(SelectedDigimon);
                    RefreshList();
                }
            }
        });
    }

    private void OnApplicationQuit() {
        DigimonDB.SaveFavorites(_favoriteDigimons);
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

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            PopupManager.Instance.Back();
        }
    }
}
