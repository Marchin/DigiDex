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
        public List<ToggleData> ToggleOptionsList;
        public int Columns;
    }

    [SerializeField] private TextMeshProUGUI _title = default;
    [SerializeField] private TextMeshProUGUI _content = default;
    [SerializeField] private Image _image = default;
    [SerializeField] private Button _closeButton = default;
    [SerializeField] private ToggleList _toggleList = default;
    [SerializeField] private ButtonElementList _buttonList = default;
    [SerializeField] private GridLayoutGroup _buttonGrid = default;
    private List<ButtonData> _buttonDataList;
    private List<ToggleData> _toggleDataList;
    private AssetReferenceAtlasedSprite _spriteReference;
    private AsyncOperationHandle _spriteHandle;
    private CancellationTokenSource _cts;
    private int _columns;
    public bool ShowCloseButton {
        get => _closeButton.transform.parent.gameObject.activeSelf;
        set {
            _closeButton.gameObject.SetActive(value);
        }
    }

    private void Awake() {
        _closeButton.onClick.AddListener(() => _ = PopupManager.Instance.Back());
    }

    public void Populate(
        string message = "",
        string title = "", 
        AssetReferenceAtlasedSprite spriteReference = null,
        List<ButtonData> buttonDataList = null,
        List<ToggleData> toggleDataList = null,
        int columns = 2
    ) {
        if (_cts != null) {
            _cts.Cancel();
            _cts.Dispose();
        }
        _cts = new CancellationTokenSource();
        if (_spriteHandle.IsValid()) {
            Addressables.Release(_spriteHandle);
        }

        ShowCloseButton = true;
        _content.text = message;
        _content.gameObject.SetActive(!string.IsNullOrEmpty(_content.text));
        _title.text = string.IsNullOrEmpty(title) ? "Message" : title;
        _image.gameObject.SetActive(false);
        _spriteReference = spriteReference;
        if (_spriteReference?.RuntimeKeyIsValid() ?? false) {
            _spriteHandle = UnityUtils.LoadSprite(_image, _spriteReference, _cts.Token);
        }
        _toggleDataList = toggleDataList;
        _toggleList.Populate(_toggleDataList);
        _toggleList.gameObject.SetActive(toggleDataList != null && toggleDataList.Count > 0);

        _buttonDataList = buttonDataList;
        _buttonList.Populate(_buttonDataList);
        _buttonList.gameObject.SetActive(_buttonDataList != null && _buttonDataList.Count > 0);
        _buttonGrid.constraintCount = columns;
        _columns = columns;
    }

    public override object GetRestorationData() {
        PopupData popupData = new PopupData {
            Message = _content.text,
            Title = _title.text,
            SpriteReference = _spriteReference,
            ButtonOptionsList = _buttonDataList,
            ToggleOptionsList = _toggleDataList,
            Columns = _columns
        };

        return popupData;
    }

    public override void Restore(object data) {
        if (data is PopupData popupData) {
            Populate(
                popupData.Message, 
                popupData.Title, 
                popupData.SpriteReference, 
                popupData.ButtonOptionsList,
                popupData.ToggleOptionsList,
                popupData.Columns
            );
        }
    }

    public override void OnClose() {
        if (_spriteHandle.IsValid()) {
            Addressables.Release(_spriteHandle);
        }
    }
}
