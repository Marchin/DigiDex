using UnityEngine;
using UnityEngine.EventSystems;

public class PointerDetector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    public bool IsPointerIn { get; private set; }

    public void OnPointerEnter(PointerEventData eventData) {
        IsPointerIn = true;
    }

    public void OnPointerExit(PointerEventData eventData) {
        IsPointerIn = false;
    }
}
