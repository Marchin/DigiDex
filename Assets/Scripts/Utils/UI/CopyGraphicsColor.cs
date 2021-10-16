using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CopyGraphicsColor : MonoBehaviour {
    [SerializeField] private Graphic _graphic = default;
    [SerializeField] private List<Image> _images = default;

    private void Update() {
        ApplyColor();
    }   

    private void OnValidate() {
        if (_graphic != null) {
            ApplyColor();
        }
    }

    private void ApplyColor() {
        Color color = _graphic.color * _graphic.canvasRenderer.GetColor();
        foreach (Image image in _images) {
            color.a = image.color.a;
            image.color = color;
        }
    }
}
