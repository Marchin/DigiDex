using UnityEngine;
using UnityEngine.UI;

public class ColorElement : MonoBehaviour, IDataUIElement<Color> {
    [SerializeField] private Image _image = default;

    public void Populate(Color color) {
        _image.color = color;
    }
}
