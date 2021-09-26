using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

public class ButtonScrollList : MonoBehaviour {
    [SerializeField] private float _scrollAnimationOffset = 550f;
    [SerializeField] private float _scrollAnimationScale = 0.2f;
    [SerializeField] private float _scrollCenteringSpeedMul = 2f;
    [SerializeField] private CustomScrollRect _scrollRect = default;
    [SerializeField] private VerticalLayoutGroup _layoutGroup = default;
    [SerializeField] private RectTransform _buttonTemplate = default;
    [SerializeField] private int _maxButtons = default;
    private Button[] _buttons;
    private TextMeshProUGUI[] _buttonsTexts;
    private List<string> _namesList = new List<string>();
    private float _buttonNormalizedLenght;
    private float _buttonReuseScrollPoint = 0.3f;
    private int _currElementScrollIndex;
    public Button CurrenButton => _buttons[_currButtonIndex];
    private bool _fixingListPosition = false;
    private Action<int> OnConfirmed;
    public event Action OnBeginDrag;
    public event Action OnEndDrag;
    public event Action<int> OnSelectedButtonChanged;
    public bool IsEmpty => _namesList.Count == 0;
    public bool ScrollEnabled {
        get => _scrollRect.enabled;
        set => _scrollRect.enabled = value;
    }
    private int _currButtonIndex;
    public int CurrButtonIndex {
        get => _currButtonIndex;
        set {
            if (_currButtonIndex != value) {
                _currButtonIndex = value;
                OnSelectedButtonChanged?.Invoke(value);
            }
        }
    }

    public async void Initialize(List<string> nameList, Action<int> onConfirmed) {
        if (nameList == null) {
            Debug.LogError("List of names is null");
            return;
        }

        OnConfirmed = onConfirmed;

        _namesList = nameList;

        int buttonCount = Mathf.Min(nameList.Count, _maxButtons);
        for (int i = 0; i < buttonCount; i++) {
            Instantiate(_buttonTemplate, _scrollRect.content);
        }

        _scrollRect.OnBeginDragEvent += () => {
            _fixingListPosition = false;
            OnBeginDrag?.Invoke();
        };
        _scrollRect.OnEndDragEvent += () => {
            _fixingListPosition = !IsEmpty;
            OnEndDrag?.Invoke();
        };

        // Wait for button population
        await UniTask.DelayFrame(1);
        
        // Add padding so that the top and botton buttonns can be centered when scrolling
        _layoutGroup.padding.top = _layoutGroup.padding.bottom =
            Mathf.RoundToInt(0.5f * (_scrollRect.viewport.rect.height - _buttonTemplate.rect.height));

        _buttonTemplate.gameObject.SetActive(false);
        _buttons = _scrollRect.content.GetComponentsInChildren<Button>();
        _buttonsTexts = _scrollRect.content.GetComponentsInChildren<TextMeshProUGUI>();

        PopulateButtons();

        OnConfirmed?.Invoke(0);

        _scrollRect.onValueChanged.AddListener(OnScroll);

        _scrollRect.normalizedPosition = Vector2.up;

        // If we don't wait these frames the buttons don't get properly animated
        await UniTask.DelayFrame(2);
        AnimateButtons();
    }

    public void UpdateList(List<string> nameList) {
        _fixingListPosition = false;
        if (nameList == null) {
            Debug.LogError("List of names is null");
            return;
        }
        _namesList = nameList;

        ResetScroll();
    }

    private void Update() {
        if (_fixingListPosition) {
            if (Mathf.Abs(_scrollRect.velocity.y) < 40f) {
                Vector2 viewportCenter = _scrollRect.viewport.transform.position;
                Vector2 selectedButtonPos = CurrenButton.transform
                    .TransformPoint((CurrenButton.transform as RectTransform).rect.center);
                if (!_scrollRect.BeingDragged) {
                    _scrollRect.velocity = _scrollCenteringSpeedMul * Vector2.up * (viewportCenter.y - selectedButtonPos.y);
                }

                if (Mathf.Abs(viewportCenter.y - selectedButtonPos.y) < 80f) {
                    OnConfirmed?.Invoke(_currButtonIndex + _currElementScrollIndex);
                    if (Mathf.Abs(viewportCenter.y - selectedButtonPos.y) < 2f) {
                        _fixingListPosition = false;
                    }
                }
            }
        }
    }

    private void OnScroll(Vector2 newPos) {
        int newScrollIndex = _currElementScrollIndex;
        if (_currElementScrollIndex < (_namesList.Count - _buttons.Length) &&
            _scrollRect.velocity.y > 0f &&
            newPos.y < ((_buttonReuseScrollPoint))
        ) {
            newScrollIndex++;
            CurrButtonIndex--;
            _scrollRect.CustomSetVerticalNormalizedPosition(_scrollRect.normalizedPosition.y + _buttonNormalizedLenght);
        } else if (_currElementScrollIndex > 0 && _scrollRect.velocity.y < 0f && newPos.y > (1f - (_buttonReuseScrollPoint))) {
            newScrollIndex--;
            CurrButtonIndex++;
            _scrollRect.CustomSetVerticalNormalizedPosition(_scrollRect.normalizedPosition.y - _buttonNormalizedLenght);
        }
        if (newScrollIndex != _currElementScrollIndex) {
            _currElementScrollIndex = newScrollIndex;
            PopulateButtons();
        }

        AnimateButtons();
    }

    private void AnimateButtons() {
        float minT = float.MaxValue;
        Vector2 viewportCenter = _scrollRect.viewport.transform.position;
        Vector2 selectedButtonPos = Vector2.zero;
        int currButtonIndex = 0;
        for (int iButton = 0; iButton < _buttons.Length; iButton++) {
            Vector2 button = _buttons[iButton].transform.TransformPoint((_buttons[iButton].transform as RectTransform).rect.center);
            float t = Mathf.Abs(viewportCenter.y - button.y) / _scrollRect.viewport.rect.height;
            button.x = viewportCenter.x - (_buttons[iButton].transform as RectTransform).rect.width*0.5f - Mathf.Lerp(0, _scrollAnimationOffset, t*t);
            _buttons[iButton].transform.localScale = Vector2.one * Mathf.Lerp(1f, _scrollAnimationScale, t*t);
            _buttons[iButton].transform.position = button;
            if (minT > t) {
                selectedButtonPos = button;
                minT = t;
                currButtonIndex = iButton;
            }
        }
        CurrButtonIndex = currButtonIndex;
    }

    private void PopulateButtons() {
        _currElementScrollIndex = Mathf.Min(_currElementScrollIndex, Mathf.Max(_namesList.Count - _buttons.Length, 0));
        for (int iDigimon = _currElementScrollIndex, iButton = 0; iButton < _buttons.Length; iDigimon++, iButton++) {
            if (iButton < _namesList.Count) {
                _buttonsTexts[iButton].text = _namesList[iDigimon];
                _buttons[iButton].gameObject.SetActive(true);
            } else {
                _buttons[iButton].gameObject.SetActive(false);
            }
        }

        float scrollableLength = _scrollRect.content.rect.height - _scrollRect.viewport.rect.height;
        _buttonNormalizedLenght = ((_buttons[0].transform as RectTransform).rect.height + _layoutGroup.spacing) /  scrollableLength;
    }

    public async void ResetScroll() {
        _scrollRect.enabled = false;
        _scrollRect.normalizedPosition = Vector2.up;
        _currElementScrollIndex = 0;
        PopulateButtons();

        await UniTask.DelayFrame(2);

        _scrollRect.enabled = true;
        AnimateButtons();
        
        if (_namesList.Count > 0) {
            OnConfirmed?.Invoke(0);
        } else {
            OnConfirmed?.Invoke(-1);
        }
    }
}
