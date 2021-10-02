using UnityEngine;

public class Popup : MonoBehaviour {
    public const string VerticalSufix = " (Vertical)";
    public bool Vertical;

    public virtual void OnClose() {
    }
    
    public virtual object GetRestorationData() {
        return null;
    }

    public virtual void Restore(object data) {
    }
}
