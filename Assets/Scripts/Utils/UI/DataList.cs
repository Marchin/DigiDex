using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public interface IDataUIElement<T> {
    void Populate(T data);
}

public class DataList<T, D> : MonoBehaviour where T : MonoBehaviour, IDataUIElement<D> {
    public enum Direction {
        Vertical,
        Horizontal
    }

    private const float MinHandleLenght = 24f;
    private const float ElementReuseScrollPoint = 0.4f;
    [SerializeField] private T _template = default;
    [SerializeField] private RectTransform _root = default;
    [SerializeField] private int _maxDisplayCount = default;
    [SerializeField] private GameObject _overflowDisplay = default;
    [Header("Scrolling")]
    [SerializeField] protected CustomScrollRect _scroll = default;
    [SerializeField] private HorizontalOrVerticalLayoutGroup _layoutGroup = default;
    [SerializeField] private Direction _direction = default;
    [SerializeField] private RectTransform _fakeScrollBar = default;
    [SerializeField] private RectTransform _fakeScrollBarHandle = default;
    [SerializeField] private DragHandler _scrollBarHandler = default;
	private List<T> _elements = new List<T>();
    public IReadOnlyList<T> Elements => _elements;
    public event Action<List<D>> OnPopulate;
    public event Action OnRefresh;
	private List<T> _pool = new List<T>();
    private List<D> _data;
    private int _baseIndex;
    private float _elementNormalizedLength;
    private float _handleSizeAdjustment;
    private float _prevScrollPos;
    private bool _wasScrollingDown;

    private void Start() {
        _template.gameObject.SetActive(false);
        PopupManager.Instance.OnWindowResize += CalculateSizes;
        if (_scroll != null) {
            _scroll.onValueChanged.AddListener(OnScroll);

            if (_scrollBarHandler != null) {
                _scrollBarHandler.OnPointerDownCall += OnScrollBarHandle;
                _scrollBarHandler.OnDragCall += OnScrollBarHandle;
            }
            _scroll.scrollSensitivity = 80f;
            _prevScrollPos = (_direction == Direction.Horizontal) ?
                _scroll.normalizedPosition.x :
                _scroll.normalizedPosition.y;
        }
    }

    private void OnDestroy() {
        PopupManager.Instance.OnWindowResize -= CalculateSizes;
    }

    public void Populate(List<D> data) {
        _data = data;
        if (data == null || data.Count == 0) {
            Clear();
            return;
        }

        if (_overflowDisplay != null) {
            _overflowDisplay.SetActive(false);
        }

        _baseIndex = 0;

        int index = 0;
        for (; index < data.Count; ++index) {
            if (_maxDisplayCount > 0 && index >= _maxDisplayCount) {
                if (_overflowDisplay != null) {
                    _overflowDisplay.SetActive(true);
                }
                break;
            }
            if (index >= _elements.Count) {
                if (_pool.Count > 0) {
                    var aux = _pool[0];
                    _elements.Add(aux);
                    _pool.Remove(aux);
                } else {
                    var aux = Instantiate(_template, _root);
                    aux.name = $"{_template.name} ({_elements.Count})";
                    _elements.Add(aux);
                }
            }
            T element = _elements[index];
            element.Populate(data[index]);
            element.gameObject.SetActive(true);
        }

        while (_elements.Count > index) {
            int index2 = _elements.Count - 1;
            _elements[index2].gameObject.SetActive(false);
            _pool.Insert(0, _elements[index2]);
            _elements.RemoveAt(index2);
        }

        CalculateSizes();

        OnPopulate?.Invoke(data);
    }

    public async void CalculateSizes() {
        if (_scroll != null) {
            try {
                await UniTask.WaitForEndOfFrame(cancellationToken: this.GetCancellationTokenOnDestroy());
            } catch {
                // If destroyed we return
                return;
            }
            if (_scroll == null) {
                return;
            }
            float viewportLength = (_direction == Direction.Horizontal) ? 
                _scroll.viewport.rect.width :
                _scroll.viewport.rect.height;
            float scrollableLength = (_direction == Direction.Horizontal) ? 
                _scroll.content.rect.width - viewportLength :
                _scroll.content.rect.height - viewportLength;
            float elementLength = (_direction == Direction.Horizontal) ? 
                (_template.transform as RectTransform).rect.width :
                (_template.transform as RectTransform).rect.height;
            if (_layoutGroup != null) {
                elementLength += _layoutGroup.spacing;
            }
            _elementNormalizedLength = elementLength / scrollableLength;
            
            if (_fakeScrollBar != null && _fakeScrollBarHandle != null) {
                bool showScrollbar = scrollableLength > 1f;
                if (showScrollbar) {
                    _fakeScrollBar.gameObject.SetActive(true);
                    float scrollBarLength = (_direction == Direction.Horizontal) ?
                        _fakeScrollBar.rect.width : 
                        _fakeScrollBar.rect.height;
                    float handleLength = (viewportLength / (elementLength * _data.Count)) * scrollBarLength;
                    if (handleLength < MinHandleLenght) {
                        _handleSizeAdjustment = MinHandleLenght - handleLength;
                        handleLength = MinHandleLenght;
                    } else {
                        _handleSizeAdjustment = 0f;
                    }
                    if (_direction == Direction.Horizontal) {
                        _fakeScrollBarHandle.anchorMin = new Vector2(0f, 0.5f);
                        _fakeScrollBarHandle.anchorMax = new Vector2(0f, 0.5f);
                        _fakeScrollBarHandle.sizeDelta = new Vector2(
                            handleLength,
                            _fakeScrollBarHandle.rect.height);
                    } else {
                        _fakeScrollBarHandle.anchorMin = new Vector2(0.5f, 1f);
                        _fakeScrollBarHandle.anchorMax = new Vector2(0.5f, 1f);
                        _fakeScrollBarHandle.sizeDelta = new Vector2(
                            _fakeScrollBarHandle.rect.width,
                            handleLength);
                    }
                    OnScroll(_scroll.normalizedPosition);
                } else {
                    _fakeScrollBar.gameObject.SetActive(false);
                }
                if (_direction == Direction.Horizontal) {
                    Vector2 offset = _scroll.viewport.offsetMax;
                    offset.y = showScrollbar ? 
                        -Mathf.CeilToInt(_fakeScrollBarHandle.rect.height) :
                        0;
                    _scroll.viewport.offsetMax = offset;
                } else {
                    Vector2 offset = _scroll.viewport.offsetMax;
                    offset.x = showScrollbar ? 
                        -Mathf.CeilToInt(_fakeScrollBarHandle.rect.width) :
                        0;
                    _scroll.viewport.offsetMax = offset;
                }
            }
        }
    }

    private void Refresh() {
        _baseIndex = Mathf.Min(_baseIndex, _data.Count - _elements.Count);
        for (int iData = _baseIndex, iElement = 0; iElement < _elements.Count; ++iData, ++iElement) {
            _elements[iElement].Populate(_data[iData]);
        }
        OnRefresh?.Invoke();
    }

    public void Clear() {
        if (_overflowDisplay != null) {
            _overflowDisplay.SetActive(false);
        }

        while (_elements.Count > 0) {
            int index = _elements.Count - 1;
            _elements[index].gameObject.SetActive(false);
            _pool.Insert(0, _elements[index]);
            _elements.RemoveAt(index);
        }
    }

    public void ScrollTo(int index) {
        if ((_data == null) || (_scroll == null)) {
            return;
        }

        index = Mathf.Clamp(index, 0, _data.Count - 1);
        int scrolled = Mathf.Min(index, _data.Count - _elements.Count);
        _baseIndex = index;
        Vector2 pos = _scroll.normalizedPosition;
        if (_direction == Direction.Horizontal) {
            pos.x = 1f - (scrolled - index) * _elementNormalizedLength;
        } else {
            pos.y = 1f - (scrolled - index) * _elementNormalizedLength;
        }
        Refresh();
        _scroll.normalizedPosition = pos;
    }

    private void OnScrollBarHandle(PointerEventData eventData) {
        Vector2 pos = eventData.position;
        RectTransform rect = _fakeScrollBar.transform as RectTransform;

        if (_direction == Direction.Horizontal) {
            float minX = rect.TransformPoint(_fakeScrollBar.rect.min).x;
            float maxX = rect.TransformPoint(_fakeScrollBar.rect.max).x;
            float scrollPos = Mathf.InverseLerp(minX, maxX, 
                pos.x + (0.5f * _fakeScrollBarHandle.rect.size.x));
            float scrolledElements = (1f - scrollPos) * _data.Count;
            int scrolled = Mathf.Min(Mathf.FloorToInt(scrolledElements), _data.Count - _elements.Count);
            _baseIndex = scrolled;
            pos.x = 1f - (scrolledElements - scrolled) * _elementNormalizedLength;
        } else {
            float minY = rect.TransformPoint(_fakeScrollBar.rect.min).y;
            float maxY = rect.TransformPoint(_fakeScrollBar.rect.max).y;
            float scrollPos = Mathf.InverseLerp(minY, maxY, 
                pos.y + (0.5f * _fakeScrollBarHandle.rect.size.y));
            float scrolledElements = (1f - scrollPos) * _data.Count;
            int scrolled = Mathf.Min(Mathf.FloorToInt(scrolledElements), _data.Count - _elements.Count);
            _baseIndex = scrolled;
            pos.y = 1f - (scrolledElements - scrolled) * _elementNormalizedLength;
        }

        Refresh();
        _scroll.normalizedPosition = pos;
    }

    private void OnScroll(Vector2 newPos) {
        int newScrollIndex = _baseIndex;
        float pos = (_direction == Direction.Horizontal) ? newPos.x : newPos.y;

        float delta = Mathf.Abs(newPos.y - ElementReuseScrollPoint);
        int count = Mathf.CeilToInt(delta / _elementNormalizedLength);

        bool isScrollingDown = ((pos - _prevScrollPos) != 0f) ?
            ((pos - _prevScrollPos) < 0f) :
            _wasScrollingDown;

        if (count > 0) {
            if (_baseIndex < (_data.Count - _elements.Count) && isScrollingDown && pos < ((ElementReuseScrollPoint))) {
                count = Mathf.Min(count, (_data.Count - _elements.Count) - _baseIndex);
                newScrollIndex += count;
                if (_direction == Direction.Horizontal) {
                    _scroll.CustomSetHorizontalNormalizedPosition(pos + _elementNormalizedLength * count);
                } else {
                    _scroll.CustomSetVerticalNormalizedPosition(pos + _elementNormalizedLength * count);
                }
            } else if (_baseIndex > 0 && !isScrollingDown && pos > (1f - (ElementReuseScrollPoint))) {
                count = Mathf.Min(count, _baseIndex);
                newScrollIndex -= count;
                if (_direction == Direction.Horizontal) {
                    _scroll.CustomSetHorizontalNormalizedPosition(pos - _elementNormalizedLength * count);
                } else {
                    _scroll.CustomSetVerticalNormalizedPosition(pos - _elementNormalizedLength * count);
                }
            }
        }
        
        if (newScrollIndex != _baseIndex) {
            _baseIndex = newScrollIndex;
            Refresh();
        }

        if (_fakeScrollBar != null && _fakeScrollBarHandle != null) {
            if (_direction == Direction.Horizontal) {
                float elementsScrolled = _baseIndex  + 
                    ((1f - _scroll.horizontalNormalizedPosition) / _elementNormalizedLength);
                float newX = -(elementsScrolled / _data.Count) * (_fakeScrollBar.rect.width - _handleSizeAdjustment);
                _fakeScrollBarHandle.anchoredPosition = new Vector2(
                    newX - (0.5f * _fakeScrollBarHandle.rect.width),
                    _fakeScrollBarHandle.anchoredPosition.y
                );
            } else {
                float elementsScrolled = _baseIndex  + 
                    ((1f - _scroll.verticalNormalizedPosition) / _elementNormalizedLength);
                float newY = -(elementsScrolled / _data.Count) * (_fakeScrollBar.rect.height - _handleSizeAdjustment);
                _fakeScrollBarHandle.anchoredPosition = new Vector2(
                    _fakeScrollBarHandle.anchoredPosition.x,
                    newY - (0.5f * _fakeScrollBarHandle.rect.height)
                );
            }
        }

        _prevScrollPos = (_direction == Direction.Horizontal) ? newPos.x : newPos.y;
        _wasScrollingDown = isScrollingDown;
    }
}
