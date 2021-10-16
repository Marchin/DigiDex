using TMPro;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

public class InformationData {
    public int IndentLevel;
    public string Prefix;
    public string Content;
    public AssetReferenceAtlasedSprite SpriteReference;
    public Action OnMoreInfo;
}

public class InformationElement : MonoBehaviour, IDataUIElement<InformationData> {
    [SerializeField] private int _indentWidth = default;
    [SerializeField] private Image _image = default;
    [SerializeField] private GameObject _prefixContainer = default;
    [SerializeField] private TextMeshProUGUI _prefix = default;
    [SerializeField] private TextMeshProUGUI _content = default;
    [SerializeField] private Button _moreInfoButton = default;
    [SerializeField] private GameObject _moreInfoVisual = default;
    [SerializeField] private LayoutGroup _layoutGroup = default;
    [SerializeField] private ScrollContent _scrollContent = default;
    private AsyncOperationHandle<Sprite> _spriteHandle;
    private CancellationTokenSource _cts;

    public void Populate(InformationData data) {
        if (!string.IsNullOrEmpty(data.Prefix)) {
            if (_prefix != null) {
                _prefix.gameObject.SetActive(true);
                _prefix.text = $"{data.Prefix}:";
                _content.text = data.Content;
            } else {
                _content.text = $"{data.Prefix}: {data.Content}";
            }
        } else {
            if (_prefix != null) {
                _prefix.gameObject.SetActive(false);
            }
            _content.text = data.Content;
        }

        if (_prefixContainer != null) {
            _prefixContainer.SetActive(!string.IsNullOrEmpty(data.Prefix));
        }

        if (_scrollContent != null) {
            _scrollContent.Refresh();
        }

        _image.gameObject.SetActive(false);
        _layoutGroup.padding.left = (data.IndentLevel * _indentWidth);

        if (_spriteHandle.IsValid()) {
            Addressables.Release(_spriteHandle);
        }
        
        if (_cts != null) {
            _cts.Cancel();
            _cts.Dispose();
        }
        _cts = new CancellationTokenSource();

        if (data.SpriteReference?.RuntimeKeyIsValid() ?? false) {
            _spriteHandle = Addressables.LoadAssetAsync<Sprite>(data.SpriteReference);
            _spriteHandle.WithCancellation(_cts.Token).ContinueWith(sprite => {
                _image.sprite = sprite;
                _image.gameObject.SetActive(sprite != null);
            }).Forget();
        }

        _moreInfoButton.onClick.RemoveAllListeners();
        _moreInfoButton.onClick.AddListener(() => data.OnMoreInfo?.Invoke());
        _moreInfoButton.interactable = data.OnMoreInfo != null;
        _moreInfoVisual.gameObject.SetActive(data.OnMoreInfo != null);
    }
}
