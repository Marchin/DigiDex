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

public class DigimonListTest : MonoBehaviour {
    [SerializeField] private Image _digimonImage = default;
    [SerializeField] private TextMeshProUGUI _digimonName = default;
    [SerializeField] private TextMeshProUGUI _digimonProfile = default;
    [SerializeField] private TMP_InputField _searchInput = default;
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
    private DigimonDatabase DigimonDB => _centralDB.DigimonDB;
    private CancellationTokenSource _digimonDataCTS;
    private List<AsyncOperationHandle> _digimonDataHandles = new List<AsyncOperationHandle>();
    private List<DigimonReference> _currDigimonList;
    private bool _profileOpen = false;
    
    private DigimonReference _selectedDigimon;
    public DigimonReference SelectedDigimon {
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
                var dataHandle = Addressables.LoadAssetAsync<Digimon>(value.Data);
                _digimonDataHandles.Add(dataHandle);
                dataHandle.WithCancellation(_digimonDataCTS.Token).ContinueWith(digimon => {
                    if (digimon != null) {
                        if (digimon.Sprite.RuntimeKeyIsValid()) {
                            var spriteHandle = Addressables.LoadAssetAsync<Sprite>(digimon.Sprite);
                            _digimonDataHandles.Add(spriteHandle);
                            spriteHandle.WithCancellation(_digimonDataCTS.Token).ContinueWith(sprite => {
                                if (sprite != null) {
                                    _digimonImage.gameObject.SetActive(true);
                                    _digimonImage.sprite = sprite;
                                }
                            }).Forget();
                        }

                        _digimonName.text = digimon.Name;
                        _digimonProfile.text = digimon.ProfileData;

                        _info.gameObject.SetActive(false);
                        digimon.ExtractInformationData(_centralDB).ContinueWith(data => {
                            _info.gameObject.SetActive(true);
                            _info.Populate(data);
                            UniTask.DelayFrame(1).ContinueWith(() => _infoScroll.normalizedPosition = Vector2.up).Forget();
                        }).Forget();
                    }
                }).Forget();
            } else {
                _digimonName.text = "";
                _digimonProfile.text = "";
            }
        }
    }

    private void Start() {
        if (DigimonDB == null) {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
            return;
        }

        _currDigimonList = DigimonDB.Digimons;
        _animatedScroll.Initialize(
            nameList: _currDigimonList.Select(d => d.Name).ToList(),
            onSelected: (index) => {
                if (index >= 0 && index <= _currDigimonList.Count) {
                    SelectedDigimon = _currDigimonList[index];
                    _profileButton.gameObject.SetActive(true);
                } else {
                    SelectedDigimon = null;
                    _profileButton.gameObject.SetActive(false);
                }
            }
        );

        _animatedScroll.OnBeginDrag += () => _profileButton.gameObject.SetActive(false);

        _searchInput.onValueChanged.AddListener(OnInputChanged);

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

    private void OnInputChanged(string query) {
        _currDigimonList = DigimonDB.Digimons
            .Where(digimon => digimon.Name.ToLower().Contains(query.ToLower()))
            .OrderByDescending(d => d.Name.StartsWith(query, true, CultureInfo.InvariantCulture))
            .ToList();

        _animatedScroll.UpdateList(_currDigimonList.Select(d => d.Name).ToList());
    }

}
