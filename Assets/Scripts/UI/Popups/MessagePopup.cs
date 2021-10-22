using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading;
using System.Collections.Generic;

public class MessagePopup : Popup {
    public class PopupData {
        public string Message;
        public string Title;
        public AssetReferenceAtlasedSprite SpriteReference;
        public List<ButtonData> ButtonOptionsList;
    }

    [SerializeField] private TextMeshProUGUI _title = default;
    [SerializeField] private TextMeshProUGUI _content = default;
    [SerializeField] private Image _image = default;
    [SerializeField] private Button _closeButton = default;
    [SerializeField] private ToggleList _toggleList = default;
    [SerializeField] private ButtonElementList _buttonList = default;
    private List<ButtonData> _buttonDataList;
    private List<ToggleData> _toggleDataList;
    private AssetReferenceAtlasedSprite _spriteReference;
    private AsyncOperationHandle _spriteHandle;
    private CancellationTokenSource _cts;
    public bool ShowCloseButton {
        get => _closeButton.transform.parent.gameObject.activeSelf;
        set {
            _closeButton.transform.parent.gameObject.SetActive(value);
            if (!Vertical) {
                _title.alignment = value ? TextAlignmentOptions.Right : TextAlignmentOptions.Center;
            }
        }
    }

    private void Awake() {
        _closeButton.onClick.AddListener(PopupManager.Instance.Back);
    }

    public void Populate(string message, string title = "", 
        AssetReferenceAtlasedSprite spriteReference = null,
        List<ButtonData> buttonDataList = null,
        List<ToggleData> toggleDataList = null
    ) {
        if (_cts != null) {
            _cts.Cancel();
            _cts.Dispose();
        }
        _cts = new CancellationTokenSource();
        if (_spriteHandle.IsValid()) {
            Addressables.Release(_spriteHandle);
        }

        _content.text = message;
        _title.text = string.IsNullOrEmpty(title) ? "Message" : title;
        _image.gameObject.SetActive(false);
        _spriteReference = spriteReference;
        if (_spriteReference?.RuntimeKeyIsValid() ?? false) {
            _spriteHandle = UnityUtils.LoadSprite(_image, _spriteReference, _cts.Token);
        }
        _toggleDataList = toggleDataList;
        _toggleList.Populate(_toggleDataList);

        _buttonDataList = buttonDataList;
        _buttonList.Populate(_buttonDataList);
    }

    public override object GetRestorationData() {
        PopupData popupData = new PopupData {
            Message = _content.text,
            Title = _title.text,
            SpriteReference = _spriteReference,
            ButtonOptionsList = _buttonDataList
        };

        return popupData;
    }

    public override void Restore(object data) {
        if (data is PopupData popupData) {
            Populate(
                popupData.Message, 
                popupData.Title, 
                popupData.SpriteReference, 
                popupData.ButtonOptionsList
            );
        }
    }

    public override void OnClose() {
        if (_spriteHandle.IsValid()) {
            Addressables.Release(_spriteHandle);
        }
    }
}
