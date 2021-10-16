using TMPro;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public enum FilterState {
    None,
    Required,
    Excluded,
}

public class FilterEntryData {
    public string Name;
    public FilterState State;
    public Action<FilterState> OnStateChange;
    public AssetReferenceAtlasedSprite Sprite;

    public FilterEntryData Clone() {
        FilterEntryData newEntryData = new FilterEntryData();
        newEntryData.Name = Name;
        newEntryData.State = State;
        newEntryData.OnStateChange = OnStateChange;
        newEntryData.Sprite = Sprite;

        return newEntryData;
    }
}

public class FilterEntryElement : MonoBehaviour, IDataUIElement<FilterEntryData> {
    [SerializeField] private Toggle _requiredToggle = default;
    [SerializeField] private Toggle _excludeToggle = default;
    [SerializeField] private Image _image = default;
    [SerializeField] private TextMeshProUGUI _label = default;
    [SerializeField] private ScrollContent _scrollContent = default;
    private FilterEntryData _entryData;
    private AsyncOperationHandle<Sprite> _spriteHandle;
    private CancellationTokenSource _cts;
    public bool IsScrollContentOn {
        get => (_scrollContent != null) ? _scrollContent.enabled : false;
        set {
            if (_scrollContent != null) {
                _scrollContent.enabled = value;
            }
        }
    }

    private void Awake() {
        _requiredToggle.onValueChanged.AddListener(isOn => {
            if (_entryData == null) {
                return;
            }

            if (isOn) {
                _entryData.State = FilterState.Required;
            } else if (_excludeToggle.isOn) {
                _entryData.State = FilterState.Excluded;
            } else {
                _entryData.State = FilterState.None;
            }

            _entryData.OnStateChange?.Invoke(_entryData.State);
        });
        _excludeToggle.onValueChanged.AddListener(isOn => {
            if (_entryData == null) {
                return;
            }

            if (isOn) {
                _entryData.State = FilterState.Excluded;
            } else if (_requiredToggle.isOn) {
                _entryData.State = FilterState.Required;
            } else {
                _entryData.State = FilterState.None;
            }

            _entryData.OnStateChange?.Invoke(_entryData.State);
        });
    }

    private void OnDestroy() {
        if (_cts != null) {
            _cts.Cancel();
            _cts.Dispose();
        }
        if (_spriteHandle.IsValid()) {
            Addressables.Release(_spriteHandle);
        }
    }

    public void Populate(FilterEntryData data) {
        _entryData = data;
        _label.text = _entryData.Name;

        if (_cts != null) {
            _cts.Cancel();
            _cts.Dispose();
        }
        _cts = new CancellationTokenSource();

        if (_spriteHandle.IsValid()) {
            Addressables.Release(_spriteHandle);
        }

        if (_entryData.Sprite != default && _entryData.Sprite.RuntimeKeyIsValid()) {
            _spriteHandle = Addressables.LoadAssetAsync<Sprite>(_entryData.Sprite);
            _spriteHandle.WithCancellation(_cts.Token).ContinueWith(sprite => {
                _image.sprite = sprite;
                _image.gameObject.SetActive(sprite != null);
            }).Forget();
        } else {
            _image.gameObject.SetActive(false);
        }
        

        switch (data.State) {
            case FilterState.None: {
                _excludeToggle.isOn = false;
                _requiredToggle.isOn = false;
            } break;
            
            case FilterState.Required: {
                _excludeToggle.isOn = false;
                _requiredToggle.isOn = true;
            } break;
            
            case FilterState.Excluded: {
                _excludeToggle.isOn = true;
                _requiredToggle.isOn = false;
            } break;

            default: {
                Debug.LogError($"{data.Name} - invalid filter state");
            } break;
        }
    }
}
