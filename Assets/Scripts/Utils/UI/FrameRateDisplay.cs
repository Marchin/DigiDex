using TMPro;
using UnityEngine;

public class FrameRateDisplay : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI _text = default;

    private void Update() {
        _text.text = Mathf.RoundToInt(1f / Time.deltaTime).ToString();
    }
}
