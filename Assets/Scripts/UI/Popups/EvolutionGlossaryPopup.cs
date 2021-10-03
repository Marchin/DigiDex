using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EvolutionGlossaryPopup : Popup {
    [SerializeField] private ColorPlusTextList _list = default;
    [SerializeField] private Button _closeButton = default;

    private void Start() {
        EvolutionType[] evolutionTypes = Enum.GetValues(typeof(EvolutionType)) as EvolutionType[];
        List<ColorPlusTextData> dataList = new List<ColorPlusTextData>(evolutionTypes.Length);

        foreach (var evolutionType in evolutionTypes) {
            if (evolutionType == EvolutionType.Regular) {
                continue;
            }

            ColorPlusTextData data = new ColorPlusTextData();
            data.ElementColor = Evolution.GetEvolutionColors(evolutionType)[0];
            data.Text = Regex.Replace(evolutionType.ToString(), "([a-z])([A-Z])", "$1 $2") + " Evolution";
            dataList.Add(data);
        }

        _list.Populate(dataList);

        _closeButton.onClick.AddListener(PopupManager.Instance.Back);
    }
}
