using System;
using UnityEngine;
using UnityEngine.EventSystems;
public sealed class PlayerInventoryPointer : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Action Context, Hover, Exit;
    public void OnPointerClick(PointerEventData data) { if(data.button == PointerEventData.InputButton.Right) Context?.Invoke(); }
    public void OnPointerEnter(PointerEventData data) => Hover?.Invoke();
    public void OnPointerExit(PointerEventData data) => Exit?.Invoke();
}
