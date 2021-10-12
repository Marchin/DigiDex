using UnityEngine;

public class ScrollContent : MonoBehaviour {
    public enum Direction {
        Horizontal,
        Vertical
    }

    [SerializeField] private RectTransform _container = default;
    [SerializeField] private RectTransform _content = default;
    [SerializeField] private Direction _direction = default;
    [SerializeField] private float _speed = 128;
    private Vector2 _dirVector;
    private Vector2 _initPivot;
    private Vector2 _initPos;
    private bool _isVertical;
    private bool _initialized;

    private void Start() {
        _isVertical = (_direction == Direction.Vertical);
        _initPivot = _content.pivot;
        _initPos = _content.anchoredPosition;
        _dirVector = _isVertical ? Vector2.up : Vector2.left;
        _content.pivot = _isVertical ? new Vector2(0.5f, 1f) : new Vector2(0f, 0.5f);
        _initialized = true;
    }

    private void OnDisable() {
        Refresh();
    }

    public void Refresh() {
        if (!_initialized) {
            return;
        }
        float scrollDist = _isVertical ? 
            _content.rect.height - _container.rect.height :
            (_content.rect.width - _container.rect.width);
        if (scrollDist <= 0) {
            _content.pivot = _initPivot;
            _content.anchoredPosition = _initPos;
        } else {
            _content.pivot =  _isVertical ? new Vector2(0.5f, 1f) : new Vector2(0f, 0.5f);
            Vector2 newPos = _content.anchoredPosition;
            if (_isVertical) {
                newPos.y = 0f;
            } else {
                newPos.x = 0f;
            }
            _content.anchoredPosition = _initPos;
        }
    }

    private void Update() {
        float scrollDist = _isVertical ? 
            _content.rect.height - _container.rect.height :
            (_content.rect.width - _container.rect.width);
        if (scrollDist <= 0) {
            _content.pivot = _initPivot;
            _content.anchoredPosition = _initPos;
            return;
        }

        bool scrollFinished = _isVertical ?
            _content.anchoredPosition.y >= _content.rect.height :
            _content.anchoredPosition.x <= -_content.rect.width;

        if (scrollFinished) {
            Vector2 newPos = _content.anchoredPosition;
            if (_isVertical) {
                newPos.y = -_container.rect.height;
            } else {
                newPos.x = _container.rect.width;
            }
            _content.anchoredPosition = newPos;
        } else {
            _content.anchoredPosition += _dirVector * _speed * Time.deltaTime;
        }
    }
}
