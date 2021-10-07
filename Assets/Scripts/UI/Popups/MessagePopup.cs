using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessagePopup : Popup {
    public class PopupData {
        public string Message;
    }

    [SerializeField] private TextMeshProUGUI _text = default;
    [SerializeField] private Button _closeButton = default;

    private void Awake() {
        _closeButton.onClick.AddListener(PopupManager.Instance.Back);
    }

    public void Populate(string message) {
        _text.text = message;
    }

    public override object GetRestorationData() {
        PopupData popupData = new PopupData {
            Message = _text.text
        };

        return popupData;
    }

    public override void Restore(object data) {
        if (data is PopupData popupData) {
            Populate(popupData.Message);
        }
    }
}
