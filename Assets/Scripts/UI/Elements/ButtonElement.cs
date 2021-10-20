using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

public class ButtonData {
    public string Text;
    public Action Callback;
}

public class ButtonElement : MonoBehaviour, IDataUIElement<ButtonData> {
    [SerializeField] private TextMeshProUGUI _text = default;
    [SerializeField] private Button _button = default;

    public void Populate(ButtonData data) {
        _text.text = data.Text;
        _button.onClick.AddListener(() => data.Callback());
    }
}
