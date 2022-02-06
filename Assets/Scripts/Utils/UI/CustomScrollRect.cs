using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomScrollRect : ScrollRect {
    public bool BeingDragged { get; private set; }
    public event Action OnBeginDragEvent;
    public event Action OnEndDragEvent;
    private OperationBySubscription.Subscription _performanceHandle = null;

    public override void OnBeginDrag(PointerEventData eventData) {
        base.OnBeginDrag(eventData);
        OnBeginDragEvent?.Invoke();
        BeingDragged = true;
    }

    public override void OnEndDrag(PointerEventData eventData) {
        base.OnEndDrag(eventData);
        OnEndDragEvent?.Invoke();
        BeingDragged = false;
    }

    private void Update() {
        float vel = velocity.sqrMagnitude;
        bool isHandleNull = _performanceHandle == null;
        if (isHandleNull != (velocity.sqrMagnitude <= 0.01f)) {
            if (isHandleNull) {
                _performanceHandle = PerformanceManager.Instance.HighPerformance.Subscribe();
            } else {
                _performanceHandle.Finish();
                _performanceHandle = null;
            }
        }
    }

    protected override void OnDisable() {
        base.OnDisable();
        _performanceHandle?.Finish();
    }

    public void CustomSetVerticalNormalizedPosition(float value) {
        float anchoredYBeforeSet = content.anchoredPosition.y;
        var prevVelocity = velocity;
        SetNormalizedPosition(value, 1);
        m_ContentStartPosition += new Vector2(0f, content.anchoredPosition.y - anchoredYBeforeSet);
        velocity = prevVelocity;
    }

    public void CustomSetHorizontalNormalizedPosition(float value) {
        float anchoredXBeforeSet = content.anchoredPosition.x;
        var prevVelocity = velocity;
        SetNormalizedPosition(value, 0);
        m_ContentStartPosition += new Vector2(content.anchoredPosition.x - anchoredXBeforeSet, 0f);
        velocity = prevVelocity;
    }
}
