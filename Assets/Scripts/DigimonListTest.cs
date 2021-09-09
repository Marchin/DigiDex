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
    [SerializeField] private float _scrollAnimationOffset = 550f;
    [SerializeField] private float _scrollAnimationScale = 0.2f;
    [SerializeField] private float _scrollCenteringSpeedMul = 2f;
    [SerializeField] private Image _digimonImage = default;
    [SerializeField] private TextMeshProUGUI _digimonName = default;
    [SerializeField] private TextMeshProUGUI _digimonProfile = default;
    [SerializeField] private TMP_InputField _searchInput = default;
    [SerializeField] private CustomScrollRect _scrollRect = default;
    [SerializeField] private CustomScrollRect _infoScroll = default;
    [SerializeField] private InformationRowList _info = default;
    [SerializeField] private VerticalLayoutGroup _layoutGroup = default;
    [SerializeField] private RectTransform _buttonTemplate = default;
    [SerializeField] private int _maxButtons = default;
    [SerializeField] private Button _profileButton = default;
    [SerializeField] private Animator _profileAnimator = default;
    [SerializeField] private DigimonDatabase _digimonDB = default;
    private Button[] _digimonButtons;
    private TextMeshProUGUI[] _digimonButtonsTexts;
    private float _buttonNormalizeLenght;
    private float _buttonReuseScrollPoint = 0.3f;
    private int _currDigimonScrollIndex;
    private int _selectedDigimonIndex;
    private CancellationTokenSource _digimonDataCTS;
    private List<AsyncOperationHandle> _digimonDataHandles = new List<AsyncOperationHandle>();
    private bool _profileOpen = false;
    private bool _fixingListPosition = false;
    private List<DigimonReference> _currDigimonList;
    public Button CurrenButton => _digimonButtons[_currButtonIndex];
    public bool IsEmpty => _currDigimonList.Count == 0;
    private int _currButtonIndex;
    public int CurrButtonIndex {
        get => _currButtonIndex;
        set {
            if (_currButtonIndex != value) {
                _currButtonIndex = value;
                RefreshButtons();
            }
        }
    }
    
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
                        _digimonDB.ExtractDigimonData(digimon).ContinueWith(data => {
                            _info.gameObject.SetActive(true);
                            _info.Populate(data);
                            UniTask.DelayFrame(1).ContinueWith(() => _infoScroll.normalizedPosition = Vector2.up).Forget();
                        }).Forget();
                    }
                }).Forget();
            } else {
                _selectedDigimonIndex = 0;
                _digimonName.text = "";
                _digimonProfile.text = "";
            }
        }
    }

    private async void Start() {
        if (_digimonDB == null) {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
            return;
        }

        _currDigimonList = _digimonDB.Digimons;

        _searchInput.onValueChanged.AddListener(OnInputChanged);

        int buttonCount = Mathf.Min(_digimonDB.Digimons.Count, _maxButtons);
        for (int i = 0; i < buttonCount; i++) {
            Instantiate(_buttonTemplate, _scrollRect.content);
        }

        _scrollRect.OnBeginDragEvent += () => {
            _profileButton.gameObject.SetActive(false);
            _fixingListPosition = false;
        };
        _scrollRect.OnEndDragEvent += () => _fixingListPosition = !IsEmpty;

        // Wait for button population
        await UniTask.DelayFrame(1);
        
        // Add padding so that the top and botton buttonns can be centered when scrolling
        _layoutGroup.padding.top = _layoutGroup.padding.bottom =
            Mathf.RoundToInt(0.5f * (_scrollRect.viewport.rect.height - _buttonTemplate.rect.height));

        _buttonTemplate.gameObject.SetActive(false);
        _digimonButtons = _scrollRect.content.GetComponentsInChildren<Button>();
        _digimonButtonsTexts = _scrollRect.content.GetComponentsInChildren<TextMeshProUGUI>();

        PopulateButtons();

        _profileButton.onClick.AddListener(() => {
            if (_profileOpen) {
                _scrollRect.enabled = true;
                _profileAnimator.SetTrigger("Fade Out");
            } else {
                _scrollRect.enabled = false;
                _profileAnimator.SetTrigger("Fade In");
            }
            _profileOpen = !_profileOpen;
        });

        SelectedDigimon = _digimonDB.Digimons[0];

        _scrollRect.onValueChanged.AddListener(OnScroll);

        _scrollRect.normalizedPosition = Vector2.up;

        // If we don't wait these frames the buttons don't get properly animated
        await UniTask.DelayFrame(2);
        AnimateButtons();
    }

    private void Update() {
        if (_fixingListPosition) {
            if (Mathf.Abs(_scrollRect.velocity.y) < 20f) {
                Vector2 viewportCenter = _scrollRect.viewport.transform.position;
                Vector2 selectedButtonPos = CurrenButton.transform
                    .TransformPoint((CurrenButton.transform as RectTransform).rect.center);
                if (!_scrollRect.BeingDragged) {
                    _scrollRect.velocity = _scrollCenteringSpeedMul * Vector2.up * (viewportCenter.y - selectedButtonPos.y);
                }

                if (Mathf.Abs(viewportCenter.y - selectedButtonPos.y) < 30f) {
                    SelectedDigimon = _currDigimonList[_currButtonIndex + _currDigimonScrollIndex];
                    _profileButton.gameObject.SetActive(true);
                    if (Mathf.Abs(viewportCenter.y - selectedButtonPos.y) < 2f) {
                        _fixingListPosition = false;
                    }
                }
            }
        }
    }

    private void OnInputChanged(string query) {
        _fixingListPosition = false;
        _currDigimonList = _digimonDB.Digimons
            .Where(digimon => digimon.Name.ToLower().Contains(query.ToLower()))
            .OrderByDescending(d => d.Name.StartsWith(query, true, CultureInfo.InvariantCulture))
            .ToList();

        if (_currDigimonList.Count > 0) {
            SelectedDigimon = _currDigimonList[0];
        } else {
            SelectedDigimon = null;
        }

        _scrollRect.normalizedPosition = Vector2.up;
        _currDigimonScrollIndex = 0;
        PopulateButtons();
    }

    private void OnScroll(Vector2 newPos) {
        int newScrollIndex = _currDigimonScrollIndex;
        if (_currDigimonScrollIndex < (_currDigimonList.Count - _digimonButtons.Length) && _scrollRect.velocity.y > 0f && newPos.y < ((_buttonReuseScrollPoint))) {
            newScrollIndex++;
            CurrButtonIndex--;
            _scrollRect.CustomSetVerticalNormalizedPosition(_scrollRect.normalizedPosition.y + _buttonNormalizeLenght);
        } else if (_currDigimonScrollIndex > 0 && _scrollRect.velocity.y < 0f && newPos.y > (1f - (_buttonReuseScrollPoint))) {
            newScrollIndex--;
            CurrButtonIndex++;
            _scrollRect.CustomSetVerticalNormalizedPosition(_scrollRect.normalizedPosition.y - _buttonNormalizeLenght);
        }
        if (newScrollIndex != _currDigimonScrollIndex) {
            _currDigimonScrollIndex = newScrollIndex;
            PopulateButtons();
        }

        AnimateButtons();
    }

    private void AnimateButtons() {
        float minT = float.MaxValue;
        Vector2 viewportCenter = _scrollRect.viewport.transform.position;
        Vector2 selectedButtonPos = Vector2.zero;
        for (int iButton = 0; iButton < _digimonButtons.Length; iButton++) {
            Vector2 button = _digimonButtons[iButton].transform.TransformPoint((_digimonButtons[iButton].transform as RectTransform).rect.center);
            float t = Mathf.Abs(viewportCenter.y - button.y) / _scrollRect.viewport.rect.height;
            button.x = viewportCenter.x - Mathf.Lerp(0, _scrollAnimationOffset, t*t);
            _digimonButtons[iButton].transform.localScale = Vector2.one * Mathf.Lerp(1f, _scrollAnimationScale, t*t);
            _digimonButtons[iButton].transform.position = button;
            if (minT > t) {
                selectedButtonPos = button;
                minT = t;
                CurrButtonIndex = iButton;
            }
        }
    }

    private void PopulateButtons() {
        _currDigimonScrollIndex = Mathf.Min(_currDigimonScrollIndex, Mathf.Max(_currDigimonList.Count - _digimonButtons.Length, 0));
        for (int iDigimon = _currDigimonScrollIndex, iButton = 0; iButton < _digimonButtons.Length; iDigimon++, iButton++) {
            if (iButton < _currDigimonList.Count) {
                _digimonButtonsTexts[iButton].text = _currDigimonList[iDigimon].Name;
                _digimonButtons[iButton].gameObject.SetActive(true);
            } else {
                _digimonButtons[iButton].gameObject.SetActive(false);
            }
        }

        float scrollableLength = _scrollRect.content.rect.height - _scrollRect.viewport.rect.height;
        _buttonNormalizeLenght = ((_digimonButtons[0].transform as RectTransform).rect.height + _layoutGroup.spacing) /  scrollableLength;
        RefreshButtons();
    }

    private void RefreshButtons() {
        for (int iButton = 0; iButton < _digimonButtons.Length; iButton++) {
            _digimonButtons[iButton].image.color = iButton == CurrButtonIndex ?
                Color.cyan : Color.white;
        }
    }
}
