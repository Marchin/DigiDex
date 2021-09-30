using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class EntryViewPopup : Popup {
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
    private List<AsyncOperationHandle> _dataHandles = new List<AsyncOperationHandle>();
    private EvolutionData _currEvolutionData;
    private IDatabase _db;
    private IDataEntry _entry;
    private CancellationTokenSource _cts;

    private void Awake() {
        _evolutionButton.onClick.AddListener(async () => {
            var popup = await PopupManager.Instance.GetOrLoadPopup<EvolutionsPopup>();
            popup.Populate(_entry, _currEvolutionData);
        });
        _closeButton.onClick.AddListener(() => {
            if (PopupManager.Instance.ActivePopup == this) {
                PopupManager.Instance.Back();
            }
        });

        _dataToggle.onValueChanged.AddListener(isOn => _dataContent.gameObject.SetActive(isOn));
        _profileToggle.onValueChanged.AddListener(isOn => _profileContent.gameObject.SetActive(isOn));
        
        _favoriteButton.onValueChanged.AddListener(isOn => {
            if (isOn) {
                _db.Favorites.Add(_entry.Hash);
            } else {
                _db.Favorites.Remove(_entry.Hash);
            }
        });
    }

    public void Populate(IDataEntry data) {
        _entry = data;
        _db = ApplicationManager.Instance.GetDatabase(_entry);

        if (_cts != null) {
            _cts.Cancel();
            _cts.Dispose();
        }
        _cts = new CancellationTokenSource();

        _dataScroll.verticalNormalizedPosition = 1f;
        _profileScroll.verticalNormalizedPosition = 1f;

        switch (data) {
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
                    if (digimon.EvolutionData.RuntimeKeyIsValid()) {
                        var evolutionHandle = Addressables.LoadAssetAsync<EvolutionData>(digimon.EvolutionData);
                        _dataHandles.Add(evolutionHandle);
                        evolutionHandle.WithCancellation(_cts.Token).ContinueWith(evolutionData => {
                            if (evolutionData != null) {
                                _evolutionButton.gameObject.SetActive(true);
                                _evolutionButton.interactable = (evolutionData.PreEvolutions.Count > 0 || evolutionData.Evolutions.Count > 0);
                                _currEvolutionData = evolutionData;
                            }
                        });
                    }
                }

                List<InformationData> informationData = digimon.ExtractInformationData();
                _info.Populate(informationData);
                UniTask.DelayFrame(1).ContinueWith(() => _dataScroll.normalizedPosition = Vector2.up).Forget();
            } break;
        }
    }
}