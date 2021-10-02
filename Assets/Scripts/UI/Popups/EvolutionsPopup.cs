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

public class EvolutionsPopupData {
    public IDataEntry Entry;
    public EvolutionData EvolutionData;
}

public class EvolutionsPopup : Popup {
    [SerializeField] private TextMeshProUGUI _sourceEntryName = default;
    [SerializeField] private Image _sourceEntryImage = default;
    [SerializeField] private Image _inspectedEntryImage = default;
    [SerializeField] private EvolutionList _evolutionList = default;
    [SerializeField] private Toggle _from = default;
    [SerializeField] private Toggle _to = default;
    [SerializeField] private Button _closeButton = default;
    [SerializeField] private Button _inspectButton = default;
    [SerializeField] private ScrollRect _scroll = default;
    private List<AsyncOperationHandle> _handles = new List<AsyncOperationHandle>();
    private CancellationTokenSource _cts;
    private CancellationTokenSource _inspectedCTS;
    private Evolution _currEvolution;
    private IDataEntry _sourceEntry;
    private EvolutionData _evolutionData;
    private float _fromScrollPos = 1f;
    private float _toScrollPos = 1f;
    private bool _initialized = false;
    private List<Evolution> CurrEvolutionList {
        get {
            if (_from.isOn) {
                return _evolutionData.PreEvolutions;
            } else if (_to.isOn) {
                return _evolutionData.Evolutions;
            } else {
                return null;
            }
        }
    }

    private void Awake() {
        _from.onValueChanged.AddListener(isOn => {
            if (isOn && _evolutionData != null) {
                _toScrollPos = _initialized ? _scroll.verticalNormalizedPosition : 1f;
                _evolutionList.Populate(_evolutionData.PreEvolutions);
                for (int i = 0; i < _evolutionList.Elements.Count; ++i) {
                    _evolutionList.Elements[i].OnPressed = entry => OnEvolutionSelected(entry);
                }
                OnEvolutionSelected(_evolutionData.PreEvolutions[0]);
                Canvas.ForceUpdateCanvases();
                _scroll.verticalNormalizedPosition = _fromScrollPos;
            }
        });
        _to.onValueChanged.AddListener(isOn => {
            if (isOn && _evolutionData != null) {
                _fromScrollPos = _initialized ? _scroll.verticalNormalizedPosition : 1f;
                _evolutionList.Populate(_evolutionData.Evolutions);
                for (int i = 0; i < _evolutionList.Elements.Count; ++i) {
                    _evolutionList.Elements[i].OnPressed = entry => OnEvolutionSelected(entry);
                }
                OnEvolutionSelected(_evolutionData.Evolutions[0]);
                Canvas.ForceUpdateCanvases();
                _scroll.verticalNormalizedPosition = _toScrollPos;
            }
        });
        _inspectButton.onClick.AddListener(() => {
            PopupManager.Instance.GetOrLoadPopup<EntryViewPopup>(restore: true)
                .ContinueWith(popup => {
                    Action prev = null;
                    Action next = null;
                    // if (CurrEvolutionList.Count > 1) {
                    //     prev = () => {
                    //         int index = CurrEvolutionList.IndexOf(_currEvolution);
                    //         --index;
                    //         index = index % CurrEvolutionList.Count;
                    //         if (index < 0) {
                    //             index = CurrEvolutionList.Count + index;
                    //         }
                    //         OnEvolutionSelected(_evolutionData.Evolutions[index]);
                    //         Debug.Assert(index >= 0, "Invalid Evolution");
                    //     };
                    //     next = () => {
                    //         int index = CurrEvolutionList.IndexOf(_currEvolution);
                    //         ++index;
                    //         index = index % CurrEvolutionList.Count;
                    //         if (index < 0) {
                    //             index = CurrEvolutionList.Count + index;
                    //         }
                    //         OnEvolutionSelected(_evolutionData.Evolutions[index]);
                    //         Debug.Assert(index >= 0, "Invalid Evolution");
                    //     };
                    // }
                    popup.Initialize(prev, next);
                    popup.Populate(_currEvolution.Entry.FetchEntryData());
                }).Forget();
        });
        _closeButton.onClick.AddListener(() => PopupManager.Instance.Back());
    }

    public void Populate(IDataEntry entry, EvolutionData evolutionData) {
        _initialized = false;
        _sourceEntry = entry;
        _sourceEntryName.text = entry.Name;

        _fromScrollPos = 1f;
        _toScrollPos = 1f;

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
            }).SuppressCancellationThrow().Forget();
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
        _initialized = true;
    }

    private void OnEvolutionSelected(Evolution evolution) {
        _currEvolution = evolution;
        IDataEntry entry = _currEvolution.Entry.FetchEntryData();

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
            }).SuppressCancellationThrow().Forget();
        }
    }
    
    public override object GetRestorationData() {
        EvolutionsPopupData data = new EvolutionsPopupData {
            Entry = _sourceEntry,
            EvolutionData = _evolutionData
        };


        return data;
    }

    public override void Restore(object data) {
        if (data is EvolutionsPopupData popupData) {
            Populate(popupData.Entry, popupData.EvolutionData);
        }
    }
}