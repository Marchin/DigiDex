using UnityEngine;
using System.Collections.Generic;

public interface IDataElement<T> {
    void Populate(T data);
}

public class DataList<T, D> : MonoBehaviour where T : MonoBehaviour, IDataElement<D> {
    [SerializeField] private T _template = default;
    [SerializeField] private RectTransform _root = default;
	private List<T> _elements = new List<T>();
	private List<T> _pool = new List<T>();

    private void Start() {
        _template.gameObject.SetActive(false);
    }

    public void Populate(List<D> data) {
        if (data == null) {
            Clear();
            return;
        }

        int index = 0;
        for (; index < data.Count; ++index) {
            T element = null;
            if (index < _elements.Count) {
                element = _elements[index];
            } else if (_pool.Count > 0) {
                element = _pool[_pool.Count - 1];
                _elements.Add(element);
                _pool.Remove(element);
            } else {
                element = Instantiate(_template, _root);
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
    }

    public void Clear() {
        for (int iElement = 0; iElement < _elements.Count; ++iElement) {
            _elements[iElement].gameObject.SetActive(false);
        }
        _pool.AddRange(_elements);
        _elements.Clear();
    }
}
