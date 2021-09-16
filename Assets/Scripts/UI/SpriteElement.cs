using UnityEngine;
using UnityEngine.UI;

public class SpriteElement : MonoBehaviour, IDataUIElement<Sprite> {
    [SerializeField] private Image _image = default;

    public void Populate(Sprite data) {
        _image.sprite = data;
    }
}
