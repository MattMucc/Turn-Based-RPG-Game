using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PlayerMenuFocus : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    public Action Preview;
    public void OnPointerEnter(PointerEventData data) { Preview?.Invoke(); }
    public void OnSelect(BaseEventData data)
    {
        Preview?.Invoke();
        var scroll = GetComponentInParent<ScrollRect>();
        if (!scroll || !scroll.content || !scroll.viewport) return;
        Canvas.ForceUpdateCanvases();
        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, transform);
        float offset = bounds.min.y < scroll.viewport.rect.yMin ? scroll.viewport.rect.yMin - bounds.min.y
            : bounds.max.y > scroll.viewport.rect.yMax ? scroll.viewport.rect.yMax - bounds.max.y : 0;
        scroll.content.anchoredPosition += Vector2.up * offset;
    }
}
