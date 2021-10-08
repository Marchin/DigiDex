using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;

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
    private AsyncOperation _spriteHandle;

    private void Awake() {
        _closeButton.onClick.AddListener(PopupManager.Instance.Back);
    }

    public void Populate(string message, string title = "", 
        AssetReferenceAtlasedSprite spriteReference = null
    ) {
        _content.text = message;
        _title.text = string.IsNullOrEmpty(title) ? "Message" : title;
        _image.gameObject.SetActive(false);
        _spriteReference = spriteReference;
        if (_spriteReference.RuntimeKeyIsValid()) {
            Addressables.LoadAssetAsync<Sprite>(spriteReference).Completed += operation => {
                if (operation.Status == AsyncOperationStatus.Succeeded) {
                    _image.sprite = operation.Result;
                    _image.gameObject.SetActive(operation.Result != null);
                }
            };
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
        if (_spriteReference.RuntimeKeyIsValid()) {
            _spriteReference.ReleaseAsset();
        }
    }
}
