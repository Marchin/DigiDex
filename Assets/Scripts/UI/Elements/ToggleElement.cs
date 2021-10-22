using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

public class ToggleData : ICloneable {
    public string Name;
    public bool IsOn;

    public ToggleData() {}
    public ToggleData(string name) {
        Name = name;
    }

    public virtual object Clone() {
        return this.MemberwiseClone();
    }
}

public class ToggleElement : MonoBehaviour, IDataUIElement<ToggleData> {
    [SerializeField] private TextMeshProUGUI _text = default;
    [SerializeField] private Toggle _toggle = default;
    private ToggleData _toggleData;
    
    private void Awake() {
        _toggle.onValueChanged.AddListener(isOn => _toggleData.IsOn = isOn);
    }

    public void Populate(ToggleData data) {
        _toggleData = data;
        _text.text = data.Name;
        _toggle.isOn = data.IsOn;
    }
}