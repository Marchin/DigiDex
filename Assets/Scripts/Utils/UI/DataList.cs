using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Linq;
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

    private const float ElementReuseScrollPoint = 0.3f;
    [SerializeField] private T _template = default;
    [SerializeField] private RectTransform _root = default;
    [SerializeField] private int _maxDisplayCount = default;
    [SerializeField] private GameObject _overflowDisplay = default;
    [SerializeField] protected CustomScrollRect _scroll = default;
    [SerializeField] private HorizontalOrVerticalLayoutGroup _layoutGroup = default;
    [SerializeField] private Direction _direction = default;
    [SerializeField] private RectTransform _fakeScrollBar = default;
    [SerializeField] private RectTransform _fakeScrollBarHandle = default;
    [SerializeField] private DragHandler _scrollBarHandler = default;
	private List<T> _elements = new List<T>();
    public IReadOnlyList<T> Elements => _elements;
    public event Action<List<D>> OnPopulate;
	private List<T> _pool = new List<T>();
    private List<D> _data;
    private int _baseIndex;
    private float _elementNormalizedLength;

    private void Start() {
        _template.gameObject.SetActive(false);
        if (_scroll != null) {
            _scroll.onValueChanged.AddListener(OnScroll);

            if (_scrollBarHandler != null) {
                _scrollBarHandler.OnPointerDownCall += OnScrollBarHandle;
                _scrollBarHandler.OnDragCall += OnScrollBarHandle;
            }
        }
    }

    public void Populate(IEnumerable<D> data) {
        Populate(data?.ToList());
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
            T element = null;
            if (index < _elements.Count) {
                element = _elements[index];
            } else if (_pool.Count > 0) {
                element = _pool[_pool.Count - 1];
                _elements.Add(element);
                _pool.Remove(element);
            } else {
                element = Instantiate(_template, _root);
                element.name = $"{_template.name} ({_elements.Count})";
                _elements.Add(element);
            }
            element.Populate(data[index]);
            element.gameObject.SetActive(true);
        }

        while (_elements.Count > index) {
            _elements[_elements.Count - 1].gameObject.SetActive(false);
            _pool.Add(_elements[_elements.Count - 1]);
            _elements.RemoveAt(_elements.Count - 1);
        }

        CalculateElementNormalizedLength();

        OnPopulate?.Invoke(data);
    }

    private async void CalculateElementNormalizedLength() {
        if (_scroll != null) {
            await UniTask.WaitForEndOfFrame();
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
                _fakeScrollBar.gameObject.SetActive(scrollableLength > 0);
                float totalLength = (elementLength * _data.Count) - viewportLength;
                float handleLength = (viewportLength / (elementLength * _data.Count));
                if (_direction == Direction.Horizontal) {
                    _fakeScrollBarHandle.anchorMin = new Vector2(0f, 0.5f);
                    _fakeScrollBarHandle.anchorMax = new Vector2(0f, 0.5f);
                    _fakeScrollBarHandle.sizeDelta = new Vector2(
                        handleLength * _fakeScrollBar.rect.width,
                        _fakeScrollBarHandle.rect.height);
                    Vector2 offset = _scroll.viewport.offsetMax;
                    offset.y = (scrollableLength > 0) ? 
                        -Mathf.CeilToInt(_fakeScrollBarHandle.rect.height) :
                        0;
                    _scroll.viewport.offsetMax = offset;
                } else {
                    _fakeScrollBarHandle.anchorMin = new Vector2(0.5f, 1f);
                    _fakeScrollBarHandle.anchorMax = new Vector2(0.5f, 1f);
                    _fakeScrollBarHandle.sizeDelta = new Vector2(
                        _fakeScrollBarHandle.rect.width,
                        handleLength * _fakeScrollBar.rect.height);
                    Vector2 offset = _scroll.viewport.offsetMax;
                    offset.x = (scrollableLength > 0) ? 
                        -Mathf.CeilToInt(_fakeScrollBarHandle.rect.width) :
                        0;
                    _scroll.viewport.offsetMax = offset;
                }
                OnScroll(_scroll.normalizedPosition);
            }
        }
    }

    private void Refresh() {
        _baseIndex = Mathf.Min(_baseIndex, _data.Count - _elements.Count);
        for (int iData = _baseIndex, iElement = 0; iElement < _elements.Count; ++iData, ++iElement) {
            _elements[iElement].Populate(_data[iData]);
        }
    }

    public void Clear() {
        if (_overflowDisplay != null) {
            _overflowDisplay.SetActive(false);
        }

        for (int iElement = 0; iElement < _elements.Count; ++iElement) {
            _elements[iElement].gameObject.SetActive(false);
        }
        _pool.AddRange(_elements);
        _elements.Clear();
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
        float velocity = (_direction == Direction.Horizontal) ? _scroll.velocity.x : _scroll.velocity.y;
        
        if (_baseIndex < (_data.Count - _elements.Count) &&
            velocity > 0f &&
            pos < ((ElementReuseScrollPoint))
        ) {
            newScrollIndex++;
            if (_direction == Direction.Horizontal) {
                _scroll.CustomSetHorizontalNormalizedPosition(pos + _elementNormalizedLength);
            } else {
                _scroll.CustomSetVerticalNormalizedPosition(pos + _elementNormalizedLength);
            }
        } else if (_baseIndex > 0 && velocity < 0f && pos > (1f - (ElementReuseScrollPoint))) {
            newScrollIndex--;
            if (_direction == Direction.Horizontal) {
                _scroll.CustomSetHorizontalNormalizedPosition(pos - _elementNormalizedLength);
            } else {
                _scroll.CustomSetVerticalNormalizedPosition(pos - _elementNormalizedLength);
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
                float newX = -(elementsScrolled / _data.Count) * _fakeScrollBar.rect.width;
                _fakeScrollBarHandle.anchoredPosition = new Vector2(
                    newX - (0.5f * _fakeScrollBarHandle.rect.width),
                    _fakeScrollBarHandle.anchoredPosition.y
                );
            } else {
                float elementsScrolled = _baseIndex  + 
                    ((1f - _scroll.verticalNormalizedPosition) / _elementNormalizedLength);
                float newY = -(elementsScrolled / _data.Count) * _fakeScrollBar.rect.height;
                _fakeScrollBarHandle.anchoredPosition = new Vector2(
                    _fakeScrollBarHandle.anchoredPosition.x,
                    newY - (0.5f * _fakeScrollBarHandle.rect.height)
                );
            }
        }
    }
}
