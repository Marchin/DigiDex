using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

public class ElementScrollList : MonoBehaviour {
    // Delay for populating the button list and being good to animate
    // We already tried forcing a canvas update and/or waiting until end of frame
    // This was the most consistent
    public const int FrameDelayToAnimateList = 2;
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
    private float _elementNormalizedHeight;
    private float _elementWidth;
    private float _elementReuseScrollPoint = 0.3f;
    private int _currElementScrollIndex;
    private RectTransform CurrenElement => _elements[_currElementIndex];
    private bool _fixingListPosition = false;
    private Action<int> OnConfirmed;
    public event Action OnBeginDrag;
    public event Action OnEndDrag;
    public event Action<int> OnSelectedElementChanged;
    public bool IsEmpty => _namesList.Count == 0;
    public bool ScrollEnabled {
        get => _scrollRect.enabled;
        set => _scrollRect.enabled = value;
    }
    private int _currElementIndex;
    private int CurrElementIndex {
        get => _currElementIndex;
        set {
            if (_currElementIndex != value) {
                _currElementIndex = value;
                OnSelectedElementChanged?.Invoke(value);
            }
        }
    }
    public int CurrentIndex {
        get => _currElementScrollIndex + _currElementIndex;
        set {
            value = UnityUtils.Repeat(value, _namesList.Count);
            ScrollTo(_namesList[value]);
            OnConfirmed?.Invoke(value);
        }
    }

    public async void Initialize(List<string> nameList, Action<int> onConfirmed) {
        if (nameList == null) {
            Debug.LogError("List of names is null");
            return;
        }

        OnConfirmed = onConfirmed;

        _namesList = nameList;

        int elementCount = Mathf.Min(nameList.Count, _maxElements);
        for (int i = 0; i < elementCount; i++) {
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

        Canvas.ForceUpdateCanvases();
        
        // Add padding so that the top and botton elementns can be centered when scrolling
        _layoutGroup.padding.top = _layoutGroup.padding.bottom =
            Mathf.RoundToInt(0.5f * (_scrollRect.viewport.rect.height - _elementTemplate.rect.height));

        _elementWidth = _elementTemplate.rect.width;
        _elementTemplate.gameObject.SetActive(false);

        _elements = new RectTransform[_scrollRect.content.childCount - 1];
        for (int iChild = 1; iChild < _scrollRect.content.childCount; ++iChild) {
            _elements[iChild - 1] = _scrollRect.content.GetChild(iChild) as RectTransform;
        }
        _elementsTexts = _scrollRect.content.GetComponentsInChildren<TextMeshProUGUI>();

        PopulateElements();

        OnConfirmed?.Invoke(0);

        _scrollRect.onValueChanged.AddListener(OnScroll);

        _scrollRect.normalizedPosition = Vector2.up;

        // HACK: If we don't wait these frames the elements don't get properly animated
        await UniTask.DelayFrame(FrameDelayToAnimateList);
        AnimateElements();
    }

    public void UpdateList(List<string> nameList) {
        _fixingListPosition = false;
        if (nameList == null) {
            Debug.LogError("List of names is null");
            return;
        }
        
        string lastName = null;
        if (_namesList != null && _namesList.Count > 0) {
            lastName = _namesList[_currElementScrollIndex + _currElementIndex];
        } else {
            _currElementScrollIndex = 0;
            _currElementIndex = 0;
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
                Vector2 selectedElementPos = CurrenElement.transform
                    .TransformPoint(CurrenElement.rect.center);
                if (!_scrollRect.BeingDragged) {
                    _scrollRect.velocity = _scrollCenteringSpeedMul * Vector2.up * 
                        (viewportCenter.y - selectedElementPos.y);
                }

                if (Mathf.Abs(viewportCenter.y - selectedElementPos.y) < 80f) {
                    OnConfirmed?.Invoke(_currElementIndex + _currElementScrollIndex);
                    if (Mathf.Abs(viewportCenter.y - selectedElementPos.y) < 2f) {
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
            newPos.y < ((_elementReuseScrollPoint))
        ) {
            newScrollIndex++;
            CurrElementIndex--;
            _scrollRect.CustomSetVerticalNormalizedPosition(_scrollRect.normalizedPosition.y + _elementNormalizedHeight);
        } else if (_currElementScrollIndex > 0 && _scrollRect.velocity.y < 0f && newPos.y > (1f - (_elementReuseScrollPoint))) {
            newScrollIndex--;
            CurrElementIndex++;
            _scrollRect.CustomSetVerticalNormalizedPosition(_scrollRect.normalizedPosition.y - _elementNormalizedHeight);
        }
        if (newScrollIndex != _currElementScrollIndex) {
            _currElementScrollIndex = newScrollIndex;
            PopulateElements();
        }

        AnimateElements();
    }

    private void AnimateElements() {
        float minT = float.MaxValue;
        Vector2 viewportCenter = _scrollRect.viewport.transform.position;
        Vector2 selectedElementPos = Vector2.zero;
        int currElementIndex = 0;
        for (int iElement = 0; iElement < _elements.Length; iElement++) {
            Vector2 element = _elements[iElement].transform.TransformPoint((_elements[iElement].transform as RectTransform).rect.center);
            float t = Mathf.Abs(viewportCenter.y - element.y) / _scrollRect.viewport.rect.height;
            float sqT = t * t;
            element.x = viewportCenter.x - Mathf.Lerp(0, _scrollAnimationOffset, sqT);
            _elements[iElement].transform.localScale = Vector2.one * Mathf.Lerp(1f, _scrollAnimationScale, sqT);
            _elements[iElement].transform.position = element;
            if (minT > t) {
                selectedElementPos = element;
                minT = t;
                currElementIndex = iElement;
            }
        }
        CurrElementIndex = currElementIndex;
    }

    private void PopulateElements() {
        _currElementScrollIndex = Mathf.Min(_currElementScrollIndex, Mathf.Max(_namesList.Count - _elements.Length, 0));
        for (int iDigimon = _currElementScrollIndex, iElement = 0; iElement < _elements.Length; iDigimon++, iElement++) {
            if (iDigimon < _namesList.Count) {
                _elementsTexts[iElement].text = _namesList[iDigimon];
                _elements[iElement].gameObject.SetActive(true);
            } else {
                _elements[iElement].gameObject.SetActive(false);
            }
        }

        Canvas.ForceUpdateCanvases();

        float scrollableHeight = _scrollRect.content.rect.height - _scrollRect.viewport.rect.height;
        _elementNormalizedHeight = (_elements[0].rect.height + _layoutGroup.spacing) /  scrollableHeight;
    }

    public async void ScrollTo(string name) {
        enabled = false;
        int index = _namesList.IndexOf(name);
        if (index >= 0) {
            int halfElementsIndex = Mathf.FloorToInt(
                (float)Mathf.Min(_elements.Length, _namesList.Count) * 0.5f);

            if (index < halfElementsIndex) {
                _currElementIndex = index;
            } else if (((_namesList.Count - 1) - index) < halfElementsIndex) {
                _currElementIndex = Mathf.Min(_elements.Length, _namesList.Count) - (_namesList.Count - index);
            } else {
                _currElementIndex = halfElementsIndex;
            }

            _currElementScrollIndex = Mathf.Min(index - _currElementIndex, 
                Mathf.Max(_namesList.Count - _elements.Length, 0));

            PopulateElements();
            int newIndex = _currElementIndex + _currElementScrollIndex;
            _scrollRect.verticalNormalizedPosition = 1f - _currElementIndex * _elementNormalizedHeight;
            OnConfirmed?.Invoke(newIndex);
            await UniTask.DelayFrame(FrameDelayToAnimateList);
            AnimateElements();
        }
        enabled = true;
    }

    public async void ResetScroll() {
        _scrollRect.enabled = false;
        _scrollRect.normalizedPosition = Vector2.up;
        _currElementScrollIndex = 0;
        PopulateElements();

        await UniTask.DelayFrame(FrameDelayToAnimateList);

        _scrollRect.enabled = true;
        AnimateElements();
        
        if (_namesList.Count > 0) {
            OnConfirmed?.Invoke(0);
        } else {
            OnConfirmed?.Invoke(-1);
        }
    }
}
