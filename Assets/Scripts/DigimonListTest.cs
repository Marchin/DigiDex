using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class DigimonListTest : MonoBehaviour {
    [SerializeField] private Image _digimonImage = default;
    [SerializeField] private TextMeshProUGUI _digimonName = default;
    [SerializeField] private TextMeshProUGUI _digimonProfile = default;
    [SerializeField] private TMP_InputField _searchInput = default;
    [SerializeField] private CustomScrollRect _scrollRect = default;
    [SerializeField] private VerticalLayoutGroup _layoutGroup = default;
    [SerializeField] private RectTransform _buttonTemplate = default;
    [SerializeField] private Sprite _regularButton = default;
    [SerializeField] private Sprite _selectedButton = default;
    [SerializeField] private Color _regularColor = default;
    [SerializeField] private Color _selectedColor = default;
    [SerializeField] private int _maxButtons = default;
    [SerializeField] private Button _profileButton = default;
    [SerializeField] private Animator _profileAnimator = default;
    [SerializeField] private DigimonList _digimonList = default;
    private Button[] _digimonButtons;
    private TextMeshProUGUI[] _digimonButtonsTexts;
    private float _buttonLenght;
    private float _buttonScrollLength;
    private int _currDigimonScrollIndex;
    private int _selectedDigimonIndex;
    private CancellationTokenSource _digimonDataCTS;
    private List<AsyncOperationHandle> _digimonDataHandles = new List<AsyncOperationHandle>();
    
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
                _selectedDigimonIndex = _currDigimonList.IndexOf(SelectedDigimon);
                RefreshButtons();
                var dataHandle = Addressables.LoadAssetAsync<Digimon>(value.Data);
                _digimonDataHandles.Add(dataHandle);
                _ = dataHandle.WithCancellation(_digimonDataCTS.Token).ContinueWith(digimon => {
                    if (digimon != null) {
                        if (digimon.Image.RuntimeKeyIsValid()) {
                            var spriteHandle = Addressables.LoadAssetAsync<Sprite>(digimon.Image);
                            _digimonDataHandles.Add(spriteHandle);
                            _ = spriteHandle.WithCancellation(_digimonDataCTS.Token).ContinueWith(sprite => {
                                if (sprite != null) {
                                    _digimonImage.gameObject.SetActive(true);
                                    _digimonImage.sprite = sprite;
                                }
                            });
                        }

                        _digimonName.text = digimon.Name;
                        _digimonProfile.text = digimon.ProfileData;
                    }
                });
            } else {
                _selectedDigimonIndex = 0;
                _digimonName.text = "";
                _digimonProfile.text = "";
            }
        }
    }
    private bool _profileOpen = false;
    private List<DigimonReference> _currDigimonList;

    private async void Start() {

        if (_digimonList == null) {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
            return;
        }

        _currDigimonList = _digimonList.Digimons;

        _searchInput.onValueChanged.AddListener(OnInputChanged);

        int buttonCount = Mathf.Min(_digimonList.Digimons.Count, _maxButtons);
        for (int i = 0; i < buttonCount; i++) {
            Instantiate(_buttonTemplate, transform);
        }

        await UniTask.DelayFrame(1);

        _buttonLenght = _buttonTemplate.sizeDelta.y;
        _buttonTemplate.gameObject.SetActive(false);
        _digimonButtons = GetComponentsInChildren<Button>();
        _digimonButtonsTexts = GetComponentsInChildren<TextMeshProUGUI>();

        PopulateButtons();

        _profileButton.onClick.AddListener(() => {
            if (_profileOpen) {
                _profileAnimator.SetTrigger("Fade Out");
            } else {
                _profileAnimator.SetTrigger("Fade In");
            }
            _profileOpen = !_profileOpen;
        });

        SelectedDigimon = _digimonList.Digimons[0];

        _scrollRect.onValueChanged.AddListener(OnScroll);

        float scrollLength = _scrollRect.content.sizeDelta.y - (_scrollRect.transform as RectTransform).sizeDelta.y;
        _buttonScrollLength = (_buttonLenght + _layoutGroup.spacing + _layoutGroup.padding.top) / scrollLength;
    }

    private void OnInputChanged(string query) {
        _currDigimonList = _digimonList.Digimons.Where(digimon => digimon.Name.ToLower().Contains(query.ToLower())).ToList();

        if (_currDigimonList.Contains(SelectedDigimon)) {
            _selectedDigimonIndex = _currDigimonList.IndexOf(SelectedDigimon);
        } else if (_currDigimonList.Count > 0) {
            SelectedDigimon = _currDigimonList[0];
        } else {
            SelectedDigimon = null;
        }

        _scrollRect.normalizedPosition = Vector2.up;
        _currDigimonScrollIndex = 0;
        PopulateButtons();
    }

    private void OnScroll(Vector2 newPos) {
        if (_currDigimonScrollIndex < (_currDigimonList.Count - _digimonButtons.Length) && _scrollRect.velocity.y > 0f && newPos.y < (1f - (_buttonScrollLength * 2f))) {
            _currDigimonScrollIndex++;
            _scrollRect.CustomSetVerticalNormalizedPosition(_scrollRect.normalizedPosition.y + _buttonScrollLength);
        } else if (_currDigimonScrollIndex > 0 && _scrollRect.velocity.y < 0f && newPos.y > ((_buttonScrollLength * 2f))) {
            _currDigimonScrollIndex--;
            _scrollRect.CustomSetVerticalNormalizedPosition(_scrollRect.normalizedPosition.y - _buttonScrollLength);
        }
        PopulateButtons();
    }

    private void PopulateButtons() {
        _currDigimonScrollIndex = Mathf.Min(_currDigimonScrollIndex, Mathf.Max(_currDigimonList.Count - _digimonButtons.Length, 0)); // TODO: account for buttons > digimon
        for (int iDigimon = _currDigimonScrollIndex, iButton = 0; iButton < _digimonButtons.Length; iDigimon++, iButton++) {
            if (iButton < _currDigimonList.Count) {
                _digimonButtonsTexts[iButton].text = _currDigimonList[iDigimon].Name as string;
                int currentIndex = iDigimon;
                _digimonButtons[iButton].onClick.RemoveAllListeners();
                _digimonButtons[iButton].onClick.AddListener(() => {
                    SelectedDigimon = _currDigimonList[currentIndex];
                });
                _digimonButtons[iButton].gameObject.SetActive(true);
            } else {
                _digimonButtons[iButton].gameObject.SetActive(false);
            }
        }

        RefreshButtons();
    }

    private void RefreshButtons() {
        for (int iButton = 0; iButton < _digimonButtons.Length; iButton++) {
            if ((iButton + _currDigimonScrollIndex) == _selectedDigimonIndex) {
                _digimonButtonsTexts[iButton].color = _selectedColor;
                _digimonButtons[iButton].image.sprite = _selectedButton;
            } else {
                _digimonButtonsTexts[iButton].color = _regularColor;
                _digimonButtons[iButton].image.sprite = _regularButton;
            }
        }
    }
}
