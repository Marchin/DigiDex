using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class EntryViewPopup : Popup {
    public enum Tab {
        Data,
        Profile
    }

    public class PopupData {
        public Action Prev;
        public Action Next;
        public IDataEntry Entry;
        public Tab CurrTab;
        public float DataScrollPos;
        public float ProfileScrollPos;
    }


    [SerializeField] private Image _image = default;
    [SerializeField] private TextMeshProUGUI _profile = default;
    [SerializeField] private CustomScrollRect _dataScroll = default;
    [SerializeField] private CustomScrollRect _profileScroll = default;
    [SerializeField] private InformationRowList _info = default;
    [SerializeField] private Toggle _dataToggle = default;
    [SerializeField] private Toggle _profileToggle = default;
    [SerializeField] private RectTransform _dataContent = default;
    [SerializeField] private RectTransform _profileContent = default;
    [SerializeField] private Toggle _favoriteButton = default;
    [SerializeField] private Button _evolutionButton = default;
    [SerializeField] private Button _closeButton = default;
    [SerializeField] private Button _prevButton = default;
    [SerializeField] private Button _nextButton = default;
    [SerializeField] private GameObject _nextPrevButtonContainer = default;
    public event Action<IDataEntry> OnPopulate;
    private Action _prev;
    private Action _next;
    private List<AsyncOperationHandle> _dataHandles = new List<AsyncOperationHandle>();
    private EvolutionData _currEvolutionData;
    private IDatabase _db;
    private IDataEntry _entry;
    private CancellationTokenSource _cts;
    private Tab _currTab;
    
    private void Awake() {
        _evolutionButton.onClick.AddListener(async () => {
            var popup = await PopupManager.Instance.GetOrLoadPopup<EvolutionsPopup>(restore: true);
            popup.Populate(_entry, _currEvolutionData);
        });
        _closeButton.onClick.AddListener(() => {
            if (PopupManager.Instance.ActivePopup == this) {
                PopupManager.Instance.Back();
            }
        });
        _prevButton.onClick.AddListener(() => _prev?.Invoke());
        _nextButton.onClick.AddListener(() => _next?.Invoke());

        _dataToggle.onValueChanged.AddListener(isOn => {
            _dataContent.gameObject.SetActive(isOn);
            if (isOn) {
                _currTab = Tab.Data;
            }
        });
        _profileToggle.onValueChanged.AddListener(isOn => {
            _profileContent.gameObject.SetActive(isOn);
            if (isOn) {
                _currTab = Tab.Profile;
            }
        });
        
        _favoriteButton.onValueChanged.AddListener(isOn => {
            if (isOn) {
                _db.Favorites.Add(_entry.Hash);
            } else {
                _db.Favorites.Remove(_entry.Hash);
            }
        });
    }

    public void Initialize(Action prev, Action next) {
        _prev = prev;
        _next = next;
        _prevButton.gameObject.SetActive(_prev != null);
        _nextButton.gameObject.SetActive(_next != null);
        if (_nextPrevButtonContainer != null) {
            _nextPrevButtonContainer.SetActive(_prev != null || _next != null);
        }
    }

    public void Populate(IDataEntry entry) {
        _entry = entry;
        _db = ApplicationManager.Instance.GetDatabase(_entry);

        if (_cts != null) {
            _cts.Cancel();
            _cts.Dispose();
        }
        _cts = new CancellationTokenSource();

        _dataScroll.verticalNormalizedPosition = 1f;
        _profileScroll.verticalNormalizedPosition = 1f;

        switch (entry) {
            case Digimon digimon: {
                _profile.text = digimon.ProfileData;
                _favoriteButton.isOn = _db.Favorites.Contains(digimon.Hash);
                _dataToggle.isOn = true;
                
                if (digimon.Sprite.RuntimeKeyIsValid()) {
                    var spriteHandle = Addressables.LoadAssetAsync<Sprite>(digimon.Sprite);
                    _dataHandles.Add(spriteHandle);
                    spriteHandle.WithCancellation(_cts.Token).ContinueWith(sprite => {
                        if (sprite != null) {
                            _image.gameObject.SetActive(true);
                            _image.sprite = sprite;
                        }
                    }).Forget();
                }

                if (digimon.EvolutionData.RuntimeKeyIsValid()) {
                        var evolutionHandle = Addressables.LoadAssetAsync<EvolutionData>(digimon.EvolutionData);
                        _dataHandles.Add(evolutionHandle);
                        evolutionHandle.WithCancellation(_cts.Token).ContinueWith(evolutionData => {
                        if (evolutionData != null) {
                            _evolutionButton.gameObject.SetActive(true);
                            _evolutionButton.interactable = (evolutionData.PreEvolutions.Count > 0 || 
                                evolutionData.Evolutions.Count > 0);
                            _currEvolutionData = evolutionData;
                        }
                    });
                }

                List<InformationData> informationData = digimon.ExtractInformationData();
                _info.Populate(informationData);
                UniTask.DelayFrame(1).ContinueWith(() => _dataScroll.normalizedPosition = Vector2.up).Forget();
            } break;
        }

        OnPopulate?.Invoke(_entry);
    }

    public override object GetRestorationData() {
        PopupData data = new PopupData {
            Prev = _prev,
            Next = _next,
            Entry = _entry,
            CurrTab = _currTab,
            DataScrollPos = _dataScroll.verticalNormalizedPosition,
            ProfileScrollPos = _profileScroll.verticalNormalizedPosition
        };

        return data;
    }

    public async override void Restore(object data) {
        if (data is PopupData popupData) {
            Initialize(popupData.Prev, popupData.Next);
            Populate(popupData.Entry);
            await UniTask.DelayFrame(1);
            Canvas.ForceUpdateCanvases();
            // Turn both on to adjust scroll
            _dataContent.gameObject.SetActive(true);
            _profileScroll.gameObject.SetActive(true);
            _dataScroll.verticalNormalizedPosition = popupData.DataScrollPos;
            _profileScroll.verticalNormalizedPosition = popupData.ProfileScrollPos;
            _currTab = popupData.CurrTab;
            switch (_currTab) {
                case Tab.Data: {
                    _dataToggle.isOn = false;
                    _dataToggle.isOn = true;
                } break;
                case Tab.Profile: {
                    _profileToggle.isOn = false;
                    _profileToggle.isOn = true;
                } break;
            }
        }
    }
}
