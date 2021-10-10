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

        if (entry.Sprite.RuntimeKeyIsValid()) {
            AsyncOperationHandle<Sprite> spriteHandle = Addressables.LoadAssetAsync<Sprite>(entry.Sprite);
            _handles.Add(spriteHandle);
            await spriteHandle.WithCancellation(_cts.Token).ContinueWith(sprite => {
                _entryImage.sprite = sprite;
                _entryImage.gameObject.SetActive(sprite != null);
            });
        }

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

        Sprite[] sprites = await UniTask.WhenAll(spritesTasks);
        _fusionSprites.Populate(sprites);
    }

    public void SetSelected(bool state) {
        _fill.color = state ? _selectedColor : _unselectedColor;
    }
}
