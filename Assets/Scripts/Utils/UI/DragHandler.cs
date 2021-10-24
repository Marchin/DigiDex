using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler {
    public event Action<PointerEventData> OnBeginDragCall;
    public event Action<PointerEventData> OnDragCall;
    public event Action<PointerEventData> OnEndDragCall;
    public event Action<PointerEventData> OnPointerUpCall;
    public event Action<PointerEventData> OnPointerDownCall;
    private Handle _performanceHandle = null;
    
    public void OnBeginDrag(PointerEventData eventData) {
        OnBeginDragCall?.Invoke(eventData);
    }

    public void OnDrag(PointerEventData eventData) {
        OnDragCall?.Invoke(eventData);
    }

    public void OnEndDrag(PointerEventData eventData) {
        OnEndDragCall?.Invoke(eventData);
    }

    public void OnPointerDown(PointerEventData eventData) {
        OnPointerDownCall?.Invoke(eventData);
        _performanceHandle = PerformanceManager.Instance.RequestHighPerformance();
    }

    public void OnPointerUp(PointerEventData eventData) {
        OnPointerUpCall?.Invoke(eventData);
        _performanceHandle?.Complete();
    }

    private void OnDisbale() {
        _performanceHandle?.Complete();
    }
}
