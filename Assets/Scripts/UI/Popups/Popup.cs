using UnityEngine;

public class Popup : MonoBehaviour {
    public const string VerticalSufix = " (Vertical)";
    public bool Vertical;

    public virtual void Show() {
        gameObject.SetActive(true);
    }

    public virtual void Hide() {
        gameObject.SetActive(false);
    }

    public virtual void OnClose() {
    }
}
