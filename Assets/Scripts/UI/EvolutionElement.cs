using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

public class EvolutionElement : MonoBehaviour, IDataUIElement<Evolution> {
    [SerializeField] private TextMeshProUGUI _name = default;
    [SerializeField] private Image _entryImage = default;
    [SerializeField] private Button _button = default;
    [SerializeField] private ColorList _evolutionTypeIndicators = default;
    [SerializeField] private SpriteList _fusionSprites = default;
    private List<AsyncOperationHandle<Sprite>> _handles = new List<AsyncOperationHandle<Sprite>>();
    private CancellationTokenSource _cts;
    private Evolution _data;
    public Action<Evolution> OnPressed;

    private void Awake() {
        _button.onClick.AddListener(() => OnPressed?.Invoke(_data));
    }

    public async void Populate(Evolution data) {
        _data = data;
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

        List<Color> colors = new List<Color>();
        if (data.Type.HasFlag(EvolutionType.Warp)) {
            colors.Add(new Color(1f, 0.6f, 0f));
        }
        if (data.Type.HasFlag(EvolutionType.Armor)) {
            colors.Add(Color.cyan);
        }
        if (data.Type.HasFlag(EvolutionType.Side)) {
            colors.Add(Color.magenta);
        }
        if (data.Type.HasFlag(EvolutionType.Fusion)) {
            colors.Add(Color.green);
        }
        if (data.Type.HasFlag(EvolutionType.Main)) {
            colors.Add(Color.red);
        }
        if (data.Type.HasFlag(EvolutionType.Spirit)) {
            colors.Add(Color.yellow);
        }
        _evolutionTypeIndicators.Populate(colors);

        Sprite[] sprites = await UniTask.WhenAll(spritesTasks);
        _fusionSprites.Populate(sprites.ToList());
    }
}
