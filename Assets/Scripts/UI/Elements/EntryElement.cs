using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

public class EntryElement : MonoBehaviour, IDataUIElement<IDataEntry> {
    [SerializeField] private Button _entryButton;
    [SerializeField] private Image _entryImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private GameObject _loadingWheel;
    [SerializeField] private ScrollContent _scrollingText = default;
    private AsyncOperationHandle<Sprite> _spriteLoading;
    private CancellationTokenSource _cts;
    private IDataEntry _data;
    public Action<IDataEntry> ButtonCallback;
    public bool ScrollingText {
        get => _scrollingText.enabled;
        set => _scrollingText.enabled = value;
    }

    private void Awake() {
        _entryButton.onClick.AddListener(() => ButtonCallback?.Invoke(_data));
    }

    private void OnDestroy() {
        if (_spriteLoading.IsValid()) {
            Addressables.Release(_spriteLoading);
        }
    }

    public async void Populate(IDataEntry data) {
        if (_cts != null) {
            _cts.Cancel();
            _cts.Dispose();
        }
        
        _cts = new CancellationTokenSource();
        _nameText.text = data.DisplayName;
        _data = data;

        _loadingWheel.SetActive(true);
        var newHandle = Addressables.LoadAssetAsync<Sprite>(data.Sprite);;

        try
        {
            await newHandle.WithCancellation(_cts.Token);
            if (newHandle.Status == AsyncOperationStatus.Succeeded) {
                _entryImage.sprite = newHandle.Result;
            }
        } catch (OperationCanceledException) {
        } finally {
            if (newHandle.Status == AsyncOperationStatus.Succeeded) {
                _loadingWheel.SetActive(false);

                if (_spriteLoading.IsValid()) {
                    Addressables.Release(_spriteLoading);
                }

                _spriteLoading = newHandle;
            } else {
                _loadingWheel.SetActive(true);
            }
        }
    }
}