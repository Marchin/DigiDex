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
    [SerializeField] private RectTransform _elementTemplate = default;
    [SerializeField] private int _maxElements = default;
    private RectTransform[] _elements;
    private TextMeshProUGUI[] _elementsTexts;
    private List<string> _namesList = new List<string>();
    private float _buttonNormalizedLenght;
    private float _buttonReuseScrollPoint = 0.3f;
    private int _currElementScrollIndex;
    public RectTransform CurrenElement => _elements[_currButtonIndex];
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

        int buttonCount = Mathf.Min(nameList.Count, _maxElements);
        for (int i = 0; i < buttonCount; i++) {
            Instantiate(_elementTemplate, _scrollRect.content);
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
            Mathf.RoundToInt(0.5f * (_scrollRect.viewport.rect.height - _elementTemplate.rect.height));

        _elementTemplate.gameObject.SetActive(false);

        _elements = new RectTransform[_scrollRect.content.childCount - 1];
        for (int iChild = 1; iChild < _scrollRect.content.childCount; ++iChild) {
            _elements[iChild - 1] = _scrollRect.content.GetChild(iChild) as RectTransform;
        }
        _elementsTexts = _scrollRect.content.GetComponentsInChildren<TextMeshProUGUI>();

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
        
        string lastName = null;
        if (_namesList != null) {
            lastName = _namesList[_currElementScrollIndex + _currButtonIndex];
        }
        _namesList = nameList;

        if (!string.IsNullOrEmpty(lastName) && _namesList.Contains(lastName)) {
            ScrollTo(lastName);
        } else {
            ResetScroll();
        }
    }

    private void Update() {
        if (_fixingListPosition) {
            if (Mathf.Abs(_scrollRect.velocity.y) < 40f) {
                Vector2 viewportCenter = _scrollRect.viewport.transform.position;
                Vector2 selectedButtonPos = CurrenElement.transform
                    .TransformPoint(CurrenElement.rect.center);
                if (!_scrollRect.BeingDragged) {
                    _scrollRect.velocity = _scrollCenteringSpeedMul * Vector2.up * 
                        (viewportCenter.y - selectedButtonPos.y);
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
        if (_currElementScrollIndex < (_namesList.Count - _elements.Length) &&
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
        for (int iButton = 0; iButton < _elements.Length; iButton++) {
            Vector2 button = _elements[iButton].transform.TransformPoint((_elements[iButton].transform as RectTransform).rect.center);
            float t = Mathf.Abs(viewportCenter.y - button.y) / _scrollRect.viewport.rect.height;
            float sqT = t * t;
            button.x = viewportCenter.x - _elements[iButton].rect.width * 0.5f - Mathf.Lerp(0, _scrollAnimationOffset, sqT);
            _elements[iButton].transform.localScale = Vector2.one * Mathf.Lerp(1f, _scrollAnimationScale, sqT);
            _elements[iButton].transform.position = button;
            if (minT > t) {
                selectedButtonPos = button;
                minT = t;
                currButtonIndex = iButton;
            }
        }
        CurrButtonIndex = currButtonIndex;
    }

    private void PopulateButtons() {
        _currElementScrollIndex = Mathf.Min(_currElementScrollIndex, Mathf.Max(_namesList.Count - _elements.Length, 0));
        for (int iDigimon = _currElementScrollIndex, iButton = 0; iButton < _elements.Length; iDigimon++, iButton++) {
            if (iButton < _namesList.Count) {
                _elementsTexts[iButton].text = _namesList[iDigimon];
                _elements[iButton].gameObject.SetActive(true);
            } else {
                _elements[iButton].gameObject.SetActive(false);
            }
        }

        float scrollableLength = _scrollRect.content.rect.height - _scrollRect.viewport.rect.height;
        _buttonNormalizedLenght = (_elements[0].rect.height + _layoutGroup.spacing) /  scrollableLength;
    }

    public void ScrollTo(string name) {
        int index = _namesList.IndexOf(name);
        if (index >= 0) {
            _currButtonIndex = Mathf.Min(
                index,
                Mathf.FloorToInt((float)Mathf.Min(_elements.Length, _namesList.Count) * 0.5f)
            );

            int prevElementScrollIndex = _currElementScrollIndex;
            _currElementScrollIndex = index - _currButtonIndex;

            int newIndex = _currButtonIndex + _currElementScrollIndex;
            if (_currElementScrollIndex != prevElementScrollIndex) {
                PopulateButtons();
            }
            _scrollRect.verticalNormalizedPosition = 1f - _currButtonIndex * _buttonNormalizedLenght;
            AnimateButtons();
            OnConfirmed?.Invoke(newIndex);
        }
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
