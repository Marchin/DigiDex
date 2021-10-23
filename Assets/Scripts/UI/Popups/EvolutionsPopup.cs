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

public class EvolutionsPopup : Popup {
    public enum Tab {
        From,
        To
    }

    public class PopupData {
        public IDataEntry SourceEntry;
        public EvolutionData EvolutionData;
        public Tab CurrTab;
        public Evolution CurrPreEvolution;
        public Evolution CurrEvolution;
        public Evolution CurrTabEvolution {
            get {
                switch (CurrTab) {
                    case Tab.From:
                        return CurrPreEvolution;
                    case Tab.To:
                        return CurrEvolution;
                    default:
                        return null;
                }
            }
            set {
                switch (CurrTab) {
                    case Tab.From: {
                        CurrPreEvolution = value;
                    } break;
                    case Tab.To: {
                        CurrEvolution = value;
                    } break;
                }
            }
        }
        public List<Evolution> CurrEvolutionList {
            get {
                switch (CurrTab) {
                    case Tab.From:
                        return EvolutionData.PreEvolutions;
                    case Tab.To:
                        return EvolutionData.Evolutions;
                    default:
                        return null;
                }
            }
        }
    }

    [SerializeField] private TextMeshProUGUI _sourceEntryName = default;
    [SerializeField] private Image _sourceEntryImage = default;
    [SerializeField] private Image _inspectedEntryImage = default;
    [SerializeField] private EvolutionList _evolutionList = default;
    [SerializeField] private Toggle _from = default;
    [SerializeField] private Toggle _to = default;
    [SerializeField] private Button _closeButton = default;
    [SerializeField] private Button _inspectButton = default;
    [SerializeField] private Button _evolutionDetailsButton = default;
    [SerializeField] private Button _glossaryButton = default;
    [SerializeField] private Button _dbViewButton = default;
    [SerializeField] private ScrollRect _scroll = default;
    [SerializeField] private GameObject _loadingWheel = default;
    private List<AsyncOperationHandle> _handles = new List<AsyncOperationHandle>();
    private CancellationTokenSource _cts;
    private CancellationTokenSource _inspectedCTS;
    private IDataEntry SourceEntry => _popupData.SourceEntry;
    private EvolutionData EvolutionData => _popupData.EvolutionData;
    private float _fromScrollPos = 1f;
    private float _toScrollPos = 1f;
    private bool _initialized = false;
    private PopupData _popupData = new PopupData();

    private void Awake() {
        _from.onValueChanged.AddListener(isOn => {
            if (isOn && EvolutionData != null) {
                if (_initialized && _popupData.CurrTab == Tab.From) {
                    return;
                }
                _popupData.CurrTab = Tab.From;
                _toScrollPos = _initialized ? _scroll.verticalNormalizedPosition : 1f;
                _evolutionList.Populate(EvolutionData.PreEvolutions);
                for (int i = 0; i < _evolutionList.Elements.Count; ++i) {
                    _evolutionList.Elements[i].OnPressed = entry => OnEvolutionSelected(entry);
                }
                Evolution evo = _popupData.CurrPreEvolution ?? _popupData.EvolutionData.PreEvolutions[0];
                OnEvolutionSelected(evo);
                Canvas.ForceUpdateCanvases();
                _scroll.verticalNormalizedPosition = _fromScrollPos;
            }
        });
        _to.onValueChanged.AddListener(isOn => {
            if (isOn && EvolutionData != null) {
                if (_initialized && _popupData.CurrTab == Tab.To) {
                    return;
                }
                _popupData.CurrTab = Tab.To;
                _fromScrollPos = _initialized ? _scroll.verticalNormalizedPosition : 1f;
                _evolutionList.Populate(EvolutionData.Evolutions);
                for (int i = 0; i < _evolutionList.Elements.Count; ++i) {
                    _evolutionList.Elements[i].OnPressed = entry => OnEvolutionSelected(entry);
                }
                Evolution evo = _popupData.CurrEvolution ?? _popupData.EvolutionData.Evolutions[0];
                OnEvolutionSelected(evo);
                Canvas.ForceUpdateCanvases();
                _scroll.verticalNormalizedPosition = _toScrollPos;
            }
        });
        _inspectButton.onClick.AddListener(() => {
            PopupManager.Instance.GetOrLoadPopup<EntryViewPopup>(restore: true)
                .ContinueWith(popup => {
                    Action prev = null;
                    Action next = null;
                    int uniqueEvolution = _popupData.CurrEvolutionList.Select(e => e.Entry).Distinct().Count();
                    if (uniqueEvolution > 1) {
                        prev = () => {
                            int index = _popupData.CurrEvolutionList.IndexOf(_popupData.CurrTabEvolution);
                            Debug.Assert(index >= 0, "Invalid Evolution");
                            EntryIndex entryIndex = _popupData.CurrTabEvolution.Entry;
                            do {
                                index = UnityUtils.Repeat(--index, _popupData.CurrEvolutionList.Count);
                            } while (_popupData.CurrEvolutionList[index].Entry == entryIndex);
                            _popupData.CurrTabEvolution = _popupData.CurrEvolutionList[index];
                            EntryViewPopup activePopupInstance = 
                                PopupManager.Instance.GetLoadedPopupOfType<EntryViewPopup>();
                            activePopupInstance?.Populate(_popupData.CurrTabEvolution.Entry.FetchEntryData());
                        };
                        next = () => {
                            int index = _popupData.CurrEvolutionList.IndexOf(_popupData.CurrTabEvolution);
                            Debug.Assert(index >= 0, "Invalid Evolution");
                            EntryIndex entryIndex = _popupData.CurrTabEvolution.Entry;
                            do {
                                index = UnityUtils.Repeat(++index, _popupData.CurrEvolutionList.Count);
                            } while (_popupData.CurrEvolutionList[index].Entry == entryIndex);
                            _popupData.CurrTabEvolution = _popupData.CurrEvolutionList[index];
                            EntryViewPopup activePopupInstance = 
                                PopupManager.Instance.GetLoadedPopupOfType<EntryViewPopup>();
                            activePopupInstance?.Populate(_popupData.CurrTabEvolution.Entry.FetchEntryData());
                        };
                    }
                    popup.Initialize(prev, next);
                    popup.Populate(_popupData.CurrTabEvolution.Entry.FetchEntryData());
                }).Forget();
        });
        _evolutionDetailsButton.onClick.AddListener(() => {
            PopupManager.Instance.GetOrLoadPopup<EvolutionDetailsPopup>(true).ContinueWith(popup => {
                bool isPreEvolution = _popupData.CurrTab == Tab.From;
                popup.Populate(_popupData.SourceEntry, _popupData.CurrTabEvolution, isPreEvolution);
            });
        });
        _glossaryButton.onClick.AddListener(() =>
            PopupManager.Instance.GetOrLoadPopup<EvolutionGlossaryPopup>().Forget());
        _closeButton.onClick.AddListener(PopupManager.Instance.Back);
        _dbViewButton.onClick.AddListener(() => PopupManager.Instance.ClearStackUntilPopup<DatabaseViewPopup>());
        _evolutionList.OnRefresh += RefreshSelected;
        _loadingWheel.SetActive(false);
    }

    public void Populate(IDataEntry entry, EvolutionData evolutionData) {
        var popupData = new PopupData {
            SourceEntry = entry,
            EvolutionData = evolutionData,
        };
        Populate(popupData);
    }

    public void Populate(PopupData popupData) {
        _initialized = false;
        _popupData = popupData;
        _sourceEntryName.text = popupData.SourceEntry.Name;

        Evolution initEvolution = null;
        switch (popupData.CurrTab) {
            case Tab.From: {
                initEvolution = popupData.CurrPreEvolution;
            } break;

            case Tab.To: {
                initEvolution = popupData.CurrEvolution;
            } break;
        }

        _fromScrollPos = 1f;
        _toScrollPos = 1f;

        if (_cts != null) {
            _cts.Cancel();
            _cts.Dispose();
        }
        _cts = new CancellationTokenSource();

        if (SourceEntry.Sprite.RuntimeKeyIsValid()) {
            _loadingWheel.SetActive(true);
            _sourceEntryImage.gameObject.SetActive(false);
            AsyncOperationHandle<Sprite> spriteHandle = Addressables.LoadAssetAsync<Sprite>(SourceEntry.Sprite);
            _handles.Add(spriteHandle);
            spriteHandle.WithCancellation(_cts.Token).ContinueWith(sprite => {
                _loadingWheel.SetActive(false);
                _sourceEntryImage.sprite = sprite;
                _sourceEntryImage.gameObject.SetActive(sprite != null);
            }).SuppressCancellationThrow().Forget();
        }
        _to.gameObject.SetActive(EvolutionData.Evolutions.Count > 0);
        _from.gameObject.SetActive(EvolutionData.PreEvolutions.Count > 0);

        if (popupData.CurrEvolution != null) {
            switch (popupData.CurrTab) {
                case Tab.From: {
                    _from.isOn = false;
                    _from.isOn = true;
                } break;

                case Tab.To: {
                    _to.isOn = false;
                    _to.isOn = true;
                } break;
            }
        } else {
            if (_to.gameObject.activeSelf) {
                _to.isOn = false;
                _to.isOn = true;
            } else if (_from.gameObject.activeSelf) {
                _from.isOn = false;
                _from.isOn = true;
            }
        }

        Canvas.ForceUpdateCanvases();

        if (initEvolution != null) {
            int index = _popupData.CurrEvolutionList.IndexOf(initEvolution);
            _scroll.SnapTo(_evolutionList.Elements[index].transform as RectTransform);
        }

        _initialized = true;
    }

    private void OnEvolutionSelected(Evolution evolution) {
        _popupData.CurrTabEvolution = evolution;
        IDataEntry entry = evolution.Entry.FetchEntryData();

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
        RefreshSelected();
    }

    private void RefreshSelected() {
        foreach (var element in _evolutionList.Elements) {
            element.SetSelected(element.Data == _popupData.CurrTabEvolution);
        }
    }
    
    public override object GetRestorationData() {
        return _popupData;
    }

    public override void Restore(object data) {
        if (data is PopupData popupData) {
            Populate(popupData);
        }
    }
    
    public override void OnClose() {
        for (int iHandle = 0; iHandle < _handles.Count; ++iHandle) {
            Addressables.Release(_handles[iHandle]);
        }
        _handles.Clear();
    }
}