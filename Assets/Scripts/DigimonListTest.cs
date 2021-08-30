using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class DigimonListTest : MonoBehaviour {
    [SerializeField] private Image _digimonImage = default;
    [SerializeField] private TextMeshProUGUI _digimonName = default;
    [SerializeField] private TextMeshProUGUI _digimonProfile = default;
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
    private int _selectedDigimonIndex = 0;
    private bool _profileOpen = false;

    private async void Start() {

        if (_digimonList == null) {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
            return;
        }


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

        _digimonImage.sprite = _digimonList.Digimons[0].Image;
        _digimonName.text = _digimonList.Digimons[0].Name;
        _digimonProfile.text = _digimonList.Digimons[0].ProfileData;

        _scrollRect.onValueChanged.AddListener(OnScroll);

        float scrollLength = _scrollRect.content.sizeDelta.y - (_scrollRect.transform as RectTransform).sizeDelta.y;
        _buttonScrollLength = (_buttonLenght + _layoutGroup.spacing + _layoutGroup.padding.top) / scrollLength;
    }

    private void OnScroll(Vector2 newPos) {
        if (_currDigimonScrollIndex < (_digimonList.Digimons.Count - _digimonButtons.Length) && _scrollRect.velocity.y > 0f && newPos.y < (1f - (_buttonScrollLength * 2f))) {
            _currDigimonScrollIndex++;
            _scrollRect.CustomSetVerticalNormalizedPosition(_scrollRect.normalizedPosition.y + _buttonScrollLength);
        } else if (_currDigimonScrollIndex > 0 && _scrollRect.velocity.y < 0f && newPos.y > ((_buttonScrollLength * 2f))) {
            _currDigimonScrollIndex--;
            _scrollRect.CustomSetVerticalNormalizedPosition(_scrollRect.normalizedPosition.y - _buttonScrollLength);
        }
        PopulateButtons();
    }

    private void PopulateButtons() {
        _currDigimonScrollIndex = Mathf.Min(_currDigimonScrollIndex, _digimonList.Digimons.Count - _digimonButtons.Length); // TODO: account for buttons > digimon
        for (int iDigimon = _currDigimonScrollIndex, iButton = 0; iButton < _digimonButtons.Length; iDigimon++, iButton++) {
            _digimonButtonsTexts[iButton].text = _digimonList.Digimons[iDigimon].Name;
            int currentIndex = iDigimon;
            _digimonButtons[iButton].onClick.RemoveAllListeners();
            _digimonButtons[iButton].onClick.AddListener(() => {
                _selectedDigimonIndex = currentIndex;
                _digimonImage.sprite = _digimonList.Digimons[currentIndex].Image;
                _digimonName.text = _digimonList.Digimons[currentIndex].Name;
                _digimonProfile.text = _digimonList.Digimons[currentIndex].ProfileData;
                RefreshButtons();
            });
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
