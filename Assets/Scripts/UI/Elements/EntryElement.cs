using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading;

public class EntryElement : MonoBehaviour, IDataUIElement<IDataEntry> {
    [SerializeField] private Button _entryButton;
    [SerializeField] private Image _entryImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    private AsyncOperationHandle<Sprite> _spriteLoading;
    private AsyncOperationHandle<Sprite> _defaultSpriteLoading;
    private CancellationTokenSource _cts;
    private IDataEntry _data;
    public Action<IDataEntry> ButtonCallback;

    private void Awake() {
        _entryButton.onClick.AddListener(() => ButtonCallback?.Invoke(_data));

        _defaultSpriteLoading = Addressables.LoadAssetAsync<Sprite>(ApplicationManager.Instance.MissingSprite);
    }

    private void OnDestroy() {
        if (_spriteLoading.IsValid()) {
            Addressables.Release(_spriteLoading);
        }
        if (_defaultSpriteLoading.IsValid()) {
            Addressables.Release(_defaultSpriteLoading);
        }
    }

    public void Populate(IDataEntry data) {
        if (_cts != null) {
            _cts.Cancel();
            _cts.Dispose();
        }
        
        _cts = new CancellationTokenSource();
        _nameText.text = data.DisplayName;
        _data = data;

        if (_defaultSpriteLoading.IsDone) {
            _entryImage.sprite = _defaultSpriteLoading.Result;
        }

        if (_spriteLoading.IsValid()) {
            Addressables.Release(_spriteLoading);
        }

        _spriteLoading = UnityUtils.LoadSprite(_entryImage, data.Sprite, _cts.Token);
    }
}