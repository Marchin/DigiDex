using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ColorPlusTextData {
    public Color ElementColor;
    public string Text;
}

public class ColorPlusTextElement : MonoBehaviour, IDataUIElement<ColorPlusTextData> {
    [SerializeField] private Image _image = default;
    [SerializeField] private TextMeshProUGUI _text = default;

    public void Populate(ColorPlusTextData data) {
        _image.color = data.ElementColor;
        _text.text = data.Text;
    }
}
