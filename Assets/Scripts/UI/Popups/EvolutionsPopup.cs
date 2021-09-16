using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

public class EvolutionsPopup : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI _sourceDigimonName = default;
    [SerializeField] private Image _sourceDigimonImage = default;
    [SerializeField] private Image _inspectedDigimonImage = default;
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
                    _evolutionList.Elements[i].OnPressed = digimon => OnDigimonSelected(digimon);
                }
                OnDigimonSelected(DigimonListTest.Instance.DigimonDB.Digimons[_evolutionData.PreEvolutions[0].DigimonID]);
            }
        });
        _to.onValueChanged.AddListener(isOn => {
            if (isOn && _evolutionData != null) {
                _evolutionList.Populate(_evolutionData.Evolutions);
                OnDigimonSelected(DigimonListTest.Instance.DigimonDB.Digimons[_evolutionData.Evolutions[0].DigimonID]);
            }
        });
        _closeButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    public void Show(Digimon digimon, EvolutionData evolutionData) {
        gameObject.SetActive(true);
        _sourceDigimonName.text = digimon.Name;

        if (_cts != null) {
            _cts.Cancel();
            _cts.Dispose();
        }
        _cts = new CancellationTokenSource();

        if (digimon.Sprite.RuntimeKeyIsValid()) {
            AsyncOperationHandle<Sprite> spriteHandle = Addressables.LoadAssetAsync<Sprite>(digimon.Sprite);
            _handles.Add(spriteHandle);
            spriteHandle.WithCancellation(_cts.Token).ContinueWith(sprite => {
                _sourceDigimonImage.sprite = sprite;
                _sourceDigimonImage.gameObject.SetActive(sprite != null);
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

    private void OnDigimonSelected(Digimon digimon) {
        if (_inspectedCTS != null) {
            _inspectedCTS.Cancel();
            _inspectedCTS.Dispose();
        }
        _inspectedCTS = new CancellationTokenSource();

        if (digimon.Sprite.RuntimeKeyIsValid()) {
            AsyncOperationHandle<Sprite> spriteHandle = Addressables.LoadAssetAsync<Sprite>(digimon.Sprite);
            _handles.Add(spriteHandle);
            spriteHandle.WithCancellation(_inspectedCTS.Token).ContinueWith(sprite => {
                _inspectedDigimonImage.sprite = sprite;
                _inspectedDigimonImage.gameObject.SetActive(sprite != null);
            }).Forget();
        }
    }
}

//    if (digimon.EvolutionData.RuntimeKeyIsValid()) {
//             AsyncOperationHandle<EvolutionData> evolutionHandle = Addressables.LoadAssetAsync<EvolutionData>(digimon.EvolutionData);
//             _handles.Add(evolutionHandle);
//             evolutionHandle.WithCancellation(_cts.Token).ContinueWith(evolutionData => {
//                 _evolutionData = evolutionData;
//                 _to.gameObject.SetActive(_evolutionData.Evolutions.Count > 0);
//                 _from.gameObject.SetActive(_evolutionData.PreEvolutions.Count > 0);
//                 if (_from.gameObject.activeSelf) {
//                     _from.isOn = false;
//                     _from.isOn = true;
//                 } else if (_to.gameObject.activeSelf) {
//                     _to.Select();
//                 }
//             }).Forget();
//         }

