using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomScrollRect : ScrollRect {
    public void CustomSetVerticalNormalizedPosition(float value) {
        float anchoredYBeforeSet = content.anchoredPosition.y;
        var prevVelocity = velocity;
        SetNormalizedPosition(value, 1);
        m_ContentStartPosition += new Vector2(0f, content.anchoredPosition.y - anchoredYBeforeSet);
        velocity = prevVelocity;
    }

}
