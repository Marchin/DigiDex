using TMPro;
using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

public class EvolutionElement : MonoBehaviour, IDataUIElement<Evolution> {
    [SerializeField] private TextMeshProUGUI _name = default;
    [SerializeField] private ScrollContent _scrollingText = default;
    [SerializeField] private Image _entryImage = default;
    [SerializeField] private Image _fill = default;
    [SerializeField] private Button _button = default;
    [SerializeField] private ColorList _evolutionTypeIndicators = default;
    [SerializeField] private SpriteList _fusionSprites = default;
    [SerializeField] private Color _selectedColor = default;
    private Color _unselectedColor;
    private List<AsyncOperationHandle<Sprite>> _handles = new List<AsyncOperationHandle<Sprite>>();
    private CancellationTokenSource _cts;
    public Evolution Data { get; private set; }
    public Action<Evolution> OnPressed;
    public bool ScrollingText {
        get => _scrollingText.enabled;
        set => _scrollingText.enabled = value;
    }

    private void Awake() {
        _button.onClick.AddListener(() => OnPressed?.Invoke(Data));
        _unselectedColor = _fill.color;
    }

    public async void Populate(Evolution data) {
        Data = data;
        IDataEntry entry = data.Entry.FetchEntryData();

        if (_cts != null) {
            _cts.Cancel();
            _cts.Dispose();
        }
        _cts = new CancellationTokenSource();

        _name.text = entry.Name;

        var entryImageHandle = UnityUtils.LoadSprite(_entryImage, entry.Sprite, _cts.Token);
        _handles.Add(entryImageHandle);
        List<UniTask<Sprite>> spritesTasks = new List<UniTask<Sprite>>(data.FusionEntries.Length);
        for (int iFusionID = 0; iFusionID < data.FusionEntries.Length; ++iFusionID) {
            IDataEntry fusion = data.FusionEntries[iFusionID].FetchEntryData();
            
            if (entry.Sprite.RuntimeKeyIsValid()) {
                AsyncOperationHandle<Sprite> spriteHandle = Addressables.LoadAssetAsync<Sprite>(fusion.Sprite);
                _handles.Add(spriteHandle);
                spritesTasks.Add(spriteHandle.WithCancellation(_cts.Token));
            }
        }

        _evolutionTypeIndicators.Populate(data.GetEvolutionColors());

        var sprites = await UniTask.WhenAll(spritesTasks).SuppressCancellationThrow();
        if (!sprites.IsCanceled) {
            _fusionSprites.Populate(sprites.Result);
        }
    }

    public void SetSelected(bool state) {
        _fill.color = state ? _selectedColor : _unselectedColor;
    }
}
