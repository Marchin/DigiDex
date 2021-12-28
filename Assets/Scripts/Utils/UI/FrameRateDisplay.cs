using TMPro;
using UnityEngine;

public class FrameRateDisplay : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI _text = default;

    private void Awake() {
#if !DEVELOPMENT_BUILD && !UNITY_EDITOR
        gameObject.SetActive(false);
#endif
    }

    private void Update() {
        _text.text = Mathf.RoundToInt(1f / Time.deltaTime).ToString();
    }
}
