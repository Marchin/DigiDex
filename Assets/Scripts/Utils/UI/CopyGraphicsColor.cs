using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CopyGraphicsColor : MonoBehaviour {
    [SerializeField] private Graphic _button = default;
    [SerializeField] private List<Image> _images = default;

    private void Update() {
        ApplyColor();
    }   

    private void OnValidate() {
        ApplyColor();
    }

    private void ApplyColor() {
        Color color = _button.color * _button.canvasRenderer.GetColor();
        foreach (Image images in _images) {
            images.color = color;
        }
    }
}
