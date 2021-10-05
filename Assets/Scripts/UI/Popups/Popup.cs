using UnityEngine;

public abstract class Popup : MonoBehaviour {
    public const string VerticalSufix = " (Vertical)";
    public bool Vertical;
    public bool FullScreen = true;

    public virtual void OnClose() {}
    public abstract object GetRestorationData();
    public abstract void Restore(object data);
}
