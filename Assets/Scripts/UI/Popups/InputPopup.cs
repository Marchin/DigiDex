using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

public class InputPopup : Popup {
    public class PopupData {
        public string Title;
        public string Message;
        public Action<string> OnConfirm;
        public string ButtonText;
        public string InputText;
        public bool ReadOnly;
        public bool ShowCloseButton;
    }

    [SerializeField] private TextMeshProUGUI _title = default;
    [SerializeField] private TextMeshProUGUI _message = default;
    [SerializeField] private TextMeshProUGUI _confirmButtonText = default;
    [SerializeField] private InputField _input = default;
    [SerializeField] private Button _confirmButton = default;
    [SerializeField] private Button _closeButton = default;
    [SerializeField] private PopupCloser _backgroundCloser = default;
    private OperationBySubscription.Subscription _disableBackButton;
    private bool ShowCloseButton {
        get => _closeButton.gameObject.activeSelf;
        set {
            _closeButton.gameObject.SetActive(value);
            _backgroundCloser.CloserEnabled = value;
            if (value) {
                _disableBackButton?.Finish();
                _disableBackButton = null;
            } else {
                if (_disableBackButton == null) {
                    _disableBackButton = ApplicationManager.Instance.DisableBackButton.Subscribe();
                }
            }
        }
    }
    private Action<string> OnConfirm;

    private void Awake() {
        _closeButton.onClick.AddListener(() => _ = PopupManager.Instance.Back());
    }

    private void OnEnable() {
        PopupManager.Instance.OnStackChange += HideKeyboard;
    }

    private void OnDisable() {
        PopupManager.Instance.OnStackChange -= HideKeyboard;
        _disableBackButton?.Finish();
        _disableBackButton = null;
    }

    private void HideKeyboard() {
        _input.enabled = false;
        _input.enabled = true;
    }

    public void Populate(
        string message, string title, 
        Action<string> onConfirm,
        string buttonText = "Confirm",
        string inputText = "",
        bool readOnly = false,
        bool showCloseButton = true
    ) {
        ShowCloseButton = showCloseButton;
        _message.text = message;
        _title.text = title;
        OnConfirm = onConfirm;
        _confirmButtonText.text = buttonText;
        _input.text = inputText;
        _input.readOnly = readOnly;
        _confirmButton.gameObject.SetActive(onConfirm != null);
        _confirmButton.onClick.RemoveAllListeners();
        _confirmButton.onClick.AddListener(() => {
            OnConfirm?.Invoke(_input.text);
        });
    }

    public override object GetRestorationData() {
        PopupData data = new PopupData {
            Title = _title.text,
            Message = _message.text,
            OnConfirm = this.OnConfirm,
            ButtonText = _confirmButtonText.text,
            InputText = _input.text,
            ReadOnly = _input.readOnly,
            ShowCloseButton = ShowCloseButton
        };

        return data;
    }

    public override void Restore(object data) {
        if (data is PopupData popupData) {
            Populate(
                popupData.Message,
                popupData.Title,
                popupData.OnConfirm,
                popupData.ButtonText,
                popupData.InputText,
                popupData.ReadOnly,
                popupData.ShowCloseButton
            );
        }
    }
}
