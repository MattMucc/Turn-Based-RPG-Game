using System;
using UnityEngine;
using UnityEngine.EventSystems;
public sealed class PlayerInventoryPointer : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, ISelectHandler
{
    public Action Context, Hover, Exit;
    public Action<PointerEventData> PointerContext;
    public Action Select, Cancel;
    public Action<PointerEventData> Begin, Move, End;
    public Action<PlayerInventoryPointer> Drop;
    public bool Dragging { get; private set; }
    public void OnPointerClick(PointerEventData data)
    {
        if (Dragging || data.button != PointerEventData.InputButton.Right) return;
        if (PointerContext != null) PointerContext(data); else Context?.Invoke();
    }
    public void OnPointerEnter(PointerEventData data) => Hover?.Invoke();
    public void OnPointerExit(PointerEventData data) => Exit?.Invoke();
    public void OnSelect(BaseEventData data) { if(!(data is PointerEventData)) Select?.Invoke(); }
    public void OnBeginDrag(PointerEventData data)
    {
        if(data.button!=PointerEventData.InputButton.Left)return;
        Dragging=true;data.eligibleForClick=false;Begin?.Invoke(data);
    }
    public void OnDrag(PointerEventData data) { if(Dragging)Move?.Invoke(data); }
    public void OnEndDrag(PointerEventData data) { if(!Dragging)return;Dragging=false;End?.Invoke(data); }
    public void OnDrop(PointerEventData data)
    {
        var source=data.pointerDrag?data.pointerDrag.GetComponent<PlayerInventoryPointer>():null;
        if(data.button==PointerEventData.InputButton.Left && source && source.Dragging)Drop?.Invoke(source);
    }
    public void CancelDrag() => Dragging=false;
    void OnDisable() { if(Dragging){Dragging=false;Cancel?.Invoke();} }
}
