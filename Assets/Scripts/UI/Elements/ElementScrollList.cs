using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

public class ElementScrollList : MonoBehaviour {
    // Delay for populating the button list and being good to animate
    // We already tried forcing a canvas update and/or waiting until end of frame
    // This was the most consistent
    public const int FrameDelayToAnimateList = 2;
    private const float ElementReuseScrollPoint = 0.3f;
    private const float MinHandleHeight = 24f;
    [SerializeField] private float _scrollAnimationOffset = 550f;
    [SerializeField] private float _scrollAnimationScale = 0.2f;
    [SerializeField] private float _scrollCenteringSpeedMul = 2f;
    [SerializeField] private CustomScrollRect _scrollRect = default;
    [SerializeField] private VerticalLayoutGroup _layoutGroup = default;
    [SerializeField] private RectTransform _elementTemplate = default;
    [SerializeField] private int _maxElements = default;
    [SerializeField] private RectTransform _fakeScrollBar = default;
    [SerializeField] private RectTransform _fakeScrollBarHandle = default;
    [SerializeField] private DragHandler _scrollBarHandler = default;
    private RectTransform[] _elements;
    private ScrollContent[] _scrollContents;
    private TextMeshProUGUI[] _elementsTexts;
    private List<string> _namesList = new List<string>();
    private float _elementNormalizedHeight;
    private int _currElementScrollIndex;
    private RectTransform CurrenElement => _elements[_currElementIndex];
    private bool _fixingListPosition = false;
    private float _handleSizeAdjustment = 0f;
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
                foreach (var content in _scrollContents) {
                    content.enabled = false;
                }
                _scrollContents[_currElementIndex].enabled = true;
                OnSelectedElementChanged?.Invoke(value);
            }
        }
    }
    public int CurrentIndex {
        get => _currElementScrollIndex + CurrElementIndex;
        set {
            value = UnityUtils.Repeat(value, _namesList.Count);
            ScrollTo(value);
            OnConfirmed?.Invoke(value);
        }
    }

    private void Awake() {
        for (int i = 0; i < _maxElements; i++) {
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

        _scrollBarHandler.OnPointerUpCall += eventData => {
            _fixingListPosition = true;
        };
        _scrollBarHandler.OnPointerDownCall += eventData => {
            OnScrollBarHandle(eventData);
            _fixingListPosition = false;
        };
        _scrollBarHandler.OnDragCall += eventData => {
            OnScrollBarHandle(eventData);
            _fixingListPosition = false;
        };

        Canvas.ForceUpdateCanvases();
        _elementTemplate.gameObject.SetActive(false);

        _elements = new RectTransform[_scrollRect.content.childCount - 1];
        for (int iChild = 1; iChild < _scrollRect.content.childCount; ++iChild) {
            _elements[iChild - 1] = _scrollRect.content.GetChild(iChild) as RectTransform;
        }
        _elementsTexts = _scrollRect.content.GetComponentsInChildren<TextMeshProUGUI>();
        _scrollContents = _scrollRect.content.GetComponentsInChildren<ScrollContent>();
        _scrollRect.onValueChanged.AddListener(OnScroll);
        PopupManager.Instance.OnWindowResize += PopulateElements;
        PopupManager.Instance.OnWindowResize += AdjustMargins;
    }

    private void OnDestroy() {
        PopupManager.Instance.OnWindowResize -= PopulateElements;
        PopupManager.Instance.OnWindowResize -= AdjustMargins;
    }

    public async void Initialize(List<string> nameList, Action<int> onConfirmed) {
        if (nameList == null) {
            Debug.LogError("List of names is null");
            return;
        }

        OnConfirmed = onConfirmed;
        _namesList = nameList;
        _currElementScrollIndex = 0;
        CurrElementIndex = 0;

        PopulateElements();
        OnConfirmed?.Invoke(0);
        
        AdjustMargins();
        
        // HACK: If we don't wait these frames the elements don't get properly animated
        await UniTask.DelayFrame(FrameDelayToAnimateList,
            cancellationToken: UniTaskCancellationExtensions.GetCancellationTokenOnDestroy(this));
        

        _scrollRect.normalizedPosition = Vector2.up;
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
            lastName = _namesList[_currElementScrollIndex + CurrElementIndex];
        } else {
            _currElementScrollIndex = 0;
            CurrElementIndex = 0;
        }
        _namesList = nameList;

        if (!string.IsNullOrEmpty(lastName) && _namesList.Contains(lastName)) {
            ScrollTo(lastName, withAnimation: true);
        } else {
            ResetScroll();
        }
        PopulateElements();
        OnScroll(_scrollRect.normalizedPosition);
    }

    private void AdjustMargins() {
        // Add padding so that the top and bottom elements can be centered when scrolling
        _layoutGroup.padding.top = _layoutGroup.padding.bottom =
            Mathf.CeilToInt(0.5f * (_scrollRect.viewport.rect.height - _elements[0].rect.height));
        _layoutGroup.enabled = false;
        _layoutGroup.enabled = true;
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
                    OnConfirmed?.Invoke(CurrElementIndex + _currElementScrollIndex);
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
            newPos.y < ((ElementReuseScrollPoint))
        ) {
            newScrollIndex++;
            CurrElementIndex--;
            _scrollRect.CustomSetVerticalNormalizedPosition(
                _scrollRect.normalizedPosition.y + _elementNormalizedHeight);
        } else if (_currElementScrollIndex > 0 && 
            _scrollRect.velocity.y < 0f && newPos.y > (1f - (ElementReuseScrollPoint))
        ) {
            newScrollIndex--;
            CurrElementIndex++;
            _scrollRect.CustomSetVerticalNormalizedPosition(
                _scrollRect.normalizedPosition.y - _elementNormalizedHeight);
        }
        if (newScrollIndex != _currElementScrollIndex) {
            _currElementScrollIndex = newScrollIndex;
            PopulateElements();
        }

        float elementsScrolled = _currElementScrollIndex  + 
            ((1f - _scrollRect.verticalNormalizedPosition) / _elementNormalizedHeight);
        float newY = -(elementsScrolled / _namesList.Count) *
            (_fakeScrollBar.rect.height - _handleSizeAdjustment);
        _fakeScrollBarHandle.anchoredPosition = new Vector2(
            _fakeScrollBarHandle.anchoredPosition.x,
            newY - (0.5f * _fakeScrollBarHandle.rect.height)
        );

        AnimateElements();
    }

    private void OnScrollBarHandle(PointerEventData eventData) {
        Vector2 pos = eventData.position;
        RectTransform rect = _fakeScrollBar.transform as RectTransform;

        float minY = rect.TransformPoint(_fakeScrollBar.rect.min).y;
        float maxY = rect.TransformPoint(_fakeScrollBar.rect.max).y;
        float scrollPos = Mathf.InverseLerp(minY, maxY, 
            pos.y + (0.5f * _fakeScrollBarHandle.rect.size.y));
        float scrolledElements = (1f - scrollPos) * _namesList.Count;
        float scrolled = Mathf.Min(scrolledElements, _namesList.Count);
        ScrollTo(scrolled, withAnimation: false);
        OnScroll(pos);
    }

    private void AnimateElements() {
        float minT = float.MaxValue;
        Vector2 viewportCenter = _scrollRect.viewport.transform.position;
        Vector2 selectedElementPos = Vector2.zero;
        int currElementIndex = 0;
        for (int iElement = 0; iElement < _elements.Length; iElement++) {
            Vector2 element = _elements[iElement].TransformPoint(_elements[iElement].rect.center);
            float t = Mathf.Abs(viewportCenter.y - element.y) / _scrollRect.viewport.rect.height;
            float sqT = t * t;
            element.x = viewportCenter.x + _layoutGroup.padding.left - 
                Mathf.Lerp(0, _scrollAnimationOffset, sqT);
            _elements[iElement].localScale = Vector2.one * Mathf.Lerp(1f, _scrollAnimationScale, sqT);
            _elements[iElement].position = element;
            if (minT > t) {
                selectedElementPos = element;
                minT = t;
                currElementIndex = iElement;
            }
        }
        CurrElementIndex = currElementIndex;
    }

    private void PopulateElements() {
        _currElementScrollIndex = Mathf.Min(_currElementScrollIndex, 
            Mathf.Max(_namesList.Count - _elements.Length, 0));
        for (int iDigimon = _currElementScrollIndex, iElement = 0; 
            iElement < _elements.Length; iDigimon++,
            iElement++
        ) {
            if (iDigimon < _namesList.Count) {
                _elementsTexts[iElement].text = _namesList[iDigimon];
                _elements[iElement].gameObject.SetActive(true);
            } else {
                _elements[iElement].gameObject.SetActive(false);
            }
        }

        Canvas.ForceUpdateCanvases();

        foreach (var content in _scrollContents) {
            content.Refresh();
        }

        float scrollableHeight = _scrollRect.content.rect.height - _scrollRect.viewport.rect.height;
        bool showScrollbar = scrollableHeight > _layoutGroup.spacing;
        if (showScrollbar) {
            _fakeScrollBar.gameObject.SetActive(true);
            _elementNormalizedHeight = (_elements[0].rect.height + _layoutGroup.spacing) /
                scrollableHeight;
            float handleLength = (1f / ( _namesList.Count)) * _fakeScrollBar.rect.height;
            if (handleLength < MinHandleHeight) {
                _handleSizeAdjustment = MinHandleHeight - handleLength;
                handleLength = MinHandleHeight;
            } else {
                _handleSizeAdjustment = 0f;
            }
            _fakeScrollBarHandle.anchorMin = new Vector2(0.5f, 1f);
            _fakeScrollBarHandle.anchorMax = new Vector2(0.5f, 1f);
            _fakeScrollBarHandle.sizeDelta = new Vector2(_fakeScrollBarHandle.rect.width, handleLength);
            Vector2 offset = _scrollRect.viewport.offsetMax;
            offset.x = -Mathf.CeilToInt(_fakeScrollBarHandle.rect.width);
            _scrollRect.viewport.offsetMax = offset;
        } else {
            _fakeScrollBar.gameObject.SetActive(false);
            Vector2 offset = _scrollRect.viewport.offsetMax;
            offset.x = 0f;
            _scrollRect.viewport.offsetMax = offset;
        }
    }

    public void ScrollTo(string name, bool withAnimation = false) {
        int index = _namesList.IndexOf(name);
        ScrollTo(index);
    }

    public async void ScrollTo(float scrolled, bool withAnimation = false) {
        enabled = false;
        scrolled = Mathf.Clamp(scrolled, 0, _namesList.Count - 1);
        int halfElementsIndex = Mathf.FloorToInt(
            (float)Mathf.Min(_elements.Length, _namesList.Count) * 0.5f);

        int scrolledIndex = Mathf.RoundToInt(scrolled);
        if (scrolledIndex < halfElementsIndex) {
            CurrElementIndex = scrolledIndex;
        } else if (((_namesList.Count - 1) - scrolledIndex) < halfElementsIndex) {
            CurrElementIndex = Mathf.Min(_elements.Length, _namesList.Count) -
                (_namesList.Count - scrolledIndex);
        } else {
            CurrElementIndex = halfElementsIndex;
        }

        _currElementScrollIndex = Mathf.Min(scrolledIndex - CurrElementIndex, 
            Mathf.Max(_namesList.Count - _elements.Length, 0));

        PopulateElements();
        int newIndex = CurrElementIndex + _currElementScrollIndex;
        _scrollRect.verticalNormalizedPosition = 
            1f - (CurrElementIndex + (scrolled - scrolledIndex)) * _elementNormalizedHeight;
        OnConfirmed?.Invoke(newIndex);

        if (withAnimation) {
            await UniTask.DelayFrame(FrameDelayToAnimateList,
                cancellationToken: UniTaskCancellationExtensions.GetCancellationTokenOnDestroy(this));
            AnimateElements();
        }
        enabled = true;
    }

    public async void ResetScroll() {
        _scrollRect.enabled = false;
        _scrollRect.normalizedPosition = Vector2.up;
        _currElementScrollIndex = 0;
        PopulateElements();

        await UniTask.DelayFrame(FrameDelayToAnimateList,
            cancellationToken: UniTaskCancellationExtensions.GetCancellationTokenOnDestroy(this));

        _scrollRect.enabled = true;
        AnimateElements();
        
        if (_namesList.Count > 0) {
            OnConfirmed?.Invoke(0);
        } else {
            OnConfirmed?.Invoke(-1);
        }
    }
}
