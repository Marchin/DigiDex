using UnityEngine;

public class Popup : MonoBehaviour {
    public virtual void Show() {
        gameObject.SetActive(true);
    }

    public virtual void Hide() {
        gameObject.SetActive(false);
    }

    public virtual void OnClose() {
    }
}
