using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

public class EvolutionsPopup : Popup {
    [SerializeField] private TextMeshProUGUI _sourceEntryName = default;
    [SerializeField] private Image _sourceEntryImage = default;
    [SerializeField] private Image _inspectedEntryImage = default;
    [SerializeField] private EvolutionList _evolutionList = default;
    [SerializeField] private Toggle _from = default;
    [SerializeField] private Toggle _to = default;
    [SerializeField] private Button _closeButton = default;
    private List<AsyncOperationHandle> _handles = new List<AsyncOperationHandle>();
    private CancellationTokenSource _cts;
    private CancellationTokenSource _inspectedCTS;
    private EvolutionData _evolutionData;

    private void Awake() {
        _from.onValueChanged.AddListener(isOn => {
            if (isOn && _evolutionData != null) {
                _evolutionList.Populate(_evolutionData.PreEvolutions);
                for (int i = 0; i < _evolutionList.Elements.Count; ++i) {
                    _evolutionList.Elements[i].OnPressed = entry => OnEntrySelected(entry);
                }
                OnEntrySelected(_evolutionData.PreEvolutions[0].Entry.FetchEntry());
            }
        });
        _to.onValueChanged.AddListener(isOn => {
            if (isOn && _evolutionData != null) {
                _evolutionList.Populate(_evolutionData.Evolutions);
                OnEntrySelected(_evolutionData.Evolutions[0].Entry.FetchEntry());
            }
        });
        _closeButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    public void Populate(IDataEntry entry, EvolutionData evolutionData) {
        gameObject.SetActive(true);
        _sourceEntryName.text = entry.Name;

        if (_cts != null) {
            _cts.Cancel();
            _cts.Dispose();
        }
        _cts = new CancellationTokenSource();

        if (entry.Sprite.RuntimeKeyIsValid()) {
            AsyncOperationHandle<Sprite> spriteHandle = Addressables.LoadAssetAsync<Sprite>(entry.Sprite);
            _handles.Add(spriteHandle);
            spriteHandle.WithCancellation(_cts.Token).ContinueWith(sprite => {
                _sourceEntryImage.sprite = sprite;
                _sourceEntryImage.gameObject.SetActive(sprite != null);
            }).Forget();
        }
        _evolutionData = evolutionData;
        _to.gameObject.SetActive(_evolutionData.Evolutions.Count > 0);
        _from.gameObject.SetActive(_evolutionData.PreEvolutions.Count > 0);
        if (_from.gameObject.activeSelf) {
            _from.isOn = false;
            _from.isOn = true;
        } else if (_to.gameObject.activeSelf) {
            _to.Select();
        }
    }

    private void OnEntrySelected(IDataEntry entry) {
        if (_inspectedCTS != null) {
            _inspectedCTS.Cancel();
            _inspectedCTS.Dispose();
        }
        _inspectedCTS = new CancellationTokenSource();

        if (entry.Sprite.RuntimeKeyIsValid()) {
            AsyncOperationHandle<Sprite> spriteHandle = Addressables.LoadAssetAsync<Sprite>(entry.Sprite);
            _handles.Add(spriteHandle);
            spriteHandle.WithCancellation(_inspectedCTS.Token).ContinueWith(sprite => {
                _inspectedEntryImage.sprite = sprite;
                _inspectedEntryImage.gameObject.SetActive(sprite != null);
            }).Forget();
        }
    }
}