using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;

public enum FilterState {
    None,
    Required,
    Excluded,
}

public class FilterEntryData {
    public string Name;
    public FilterState State;
    public Action<FilterState> OnStateChange;
    // public AssetReferenceAtlasedSprite Sprite;

    public FilterEntryData Clone() {
        FilterEntryData newEntryData = new FilterEntryData();
        newEntryData.Name = Name;
        newEntryData.State = State;
        newEntryData.OnStateChange = OnStateChange;

        return newEntryData;
    }
}

public class FilterEntryElement : MonoBehaviour, IDataUIElement<FilterEntryData> {
    [SerializeField] private Toggle _requiredToggle = default;
    [SerializeField] private Toggle _excludeToggle = default;
    [SerializeField] private TextMeshProUGUI _label = default;
    private FilterEntryData _entryData;

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

    public void Populate(FilterEntryData data) {
        _entryData = data;
        _label.text = data.Name;

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
