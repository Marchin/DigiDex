using TMPro;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

public class InformationData {
    public string Prefix;
    public string Content;
    public AssetReferenceAtlasedSprite SpriteReference;
    public int IndentLevel;
}

public class InformationRow : MonoBehaviour, IDataElement<InformationData> {
    [SerializeField] private float _indentWidth = default;
    [SerializeField] private LayoutElement _indent = default;
    [SerializeField] private GameObject _imageSeparator = default;
    [SerializeField] private Image _image = default;
    [SerializeField] private TextMeshProUGUI _text = default;
    private AsyncOperationHandle<Sprite> _spriteHandle;
    private CancellationTokenSource _cts;

    public void Populate(InformationData data) {
        if (!string.IsNullOrEmpty(data.Prefix)) {
            _text.text = $"{data.Prefix}: {data.Content}";
        } else {
            _text.text = data.Content;
        }
        _image.gameObject.SetActive(false);
        _imageSeparator.SetActive(false);

        _indent.minWidth = data.IndentLevel * _indentWidth;

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
                _imageSeparator.SetActive(sprite != null);
            }).Forget();
        }
    }
}
