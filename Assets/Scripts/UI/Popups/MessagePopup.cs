using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading;

public class MessagePopup : Popup {
    public class PopupData {
        public string Message;
        public string Title;
        public AssetReferenceAtlasedSprite SpriteReference;
    }

    [SerializeField] private TextMeshProUGUI _title = default;
    [SerializeField] private TextMeshProUGUI _content = default;
    [SerializeField] private Image _image = default;
    [SerializeField] private Button _closeButton = default;
    private AssetReferenceAtlasedSprite _spriteReference;
    private AsyncOperationHandle _spriteHandle;
    private CancellationTokenSource _cts;

    private void Awake() {
        _closeButton.onClick.AddListener(PopupManager.Instance.Back);
    }

    public void Populate(string message, string title = "", 
        AssetReferenceAtlasedSprite spriteReference = null
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
    }

    public override object GetRestorationData() {
        PopupData popupData = new PopupData {
            Message = _content.text,
            Title = _title.text,
            SpriteReference = _spriteReference
        };

        return popupData;
    }

    public override void Restore(object data) {
        if (data is PopupData popupData) {
            Populate(popupData.Message, popupData.Title, popupData.SpriteReference);
        }
    }

    public override void OnClose() {
        if (_spriteHandle.IsValid()) {
            Addressables.Release(_spriteHandle);
        }
    }
}
