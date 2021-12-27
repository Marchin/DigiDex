using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

public class InputPopup : Popup {
    public class PopupData {
        public string Title;
        public string Message;
        public Action<string> OnConfirm;
    }
    [SerializeField] private TextMeshProUGUI _title = default;
    [SerializeField] private TextMeshProUGUI _message = default;
    [SerializeField] private InputField _input = default;
    [SerializeField] private Button _confirmButton = default;
    [SerializeField] private Button _closeButton = default;
    private Action<string> OnConfirm;

    private void Awake() {
        _closeButton.onClick.AddListener(PopupManager.Instance.Back);
    }

    public void Populate(string message, string title, Action<string> onConfirm) {
        _message.text = message;
        _title.text = title;
        OnConfirm = onConfirm;
        _confirmButton.onClick.RemoveAllListeners();
        _confirmButton.onClick.AddListener(() => {
            if (!string.IsNullOrEmpty(_input.text)) {
                OnConfirm(_input.text);
            }
        });
    }

    public override object GetRestorationData() {
        PopupData data = new PopupData {
            Title = _title.text,
            Message = _message.text,
            OnConfirm = this.OnConfirm
        };

        return data;
    }

    public override void Restore(object data) {
        if (data is PopupData popupData) {
            Populate(popupData.Message, popupData.Title, popupData.OnConfirm);
        }
    }
}
