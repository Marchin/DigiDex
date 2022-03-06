using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupCloser : MonoBehaviour {
    [SerializeField] private Button _button = default;
    public bool CloserEnabled {
        get => _button.enabled;
        set => _button.enabled = value;
    }

    private void Awake() {
        _button.onClick.AddListener(() => _ = PopupManager.Instance.Back());
    }
}
