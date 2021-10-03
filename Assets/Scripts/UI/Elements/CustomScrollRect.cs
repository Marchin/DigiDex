using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomScrollRect : ScrollRect {
    public bool BeingDragged { get; private set; }
    public event Action OnBeginDragEvent;
    public event Action OnEndDragEvent;

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

    public void CustomSetVerticalNormalizedPosition(float value) {
        float anchoredYBeforeSet = content.anchoredPosition.y;
        var prevVelocity = velocity;
        SetNormalizedPosition(value, 1);
        m_ContentStartPosition += new Vector2(0f, content.anchoredPosition.y - anchoredYBeforeSet);
        velocity = prevVelocity;
    }

}
