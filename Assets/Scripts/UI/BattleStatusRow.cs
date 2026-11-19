using System.Collections.Generic;
using Game.GridPrototype;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BattleStatusRow : MonoBehaviour
{
    sealed class Entry { public RectTransform Rect;public StatusBadgeGraphic Graphic;public TMP_Text Count;public StatusBadgeHover Hover; }
    readonly List<Entry> entries=new();
    RectTransform tooltip, tooltipRoot;
    TMP_Text tooltipText;
    TMP_FontAsset font;
    public float IconSize=30;
    [Min(0)] public float Spacing=6;
    public TargetUILayout Layout;
    TargetUILayout Templates=>Layout?Layout:Resources.Load<TargetUILayout>("UI/Targets/Targets");
    StatusTooltipView tooltipView;
    int hovered=-1;
    List<StatusPresentation> statuses=new();
    public bool PointerInside=>hovered>=0;
    public void Initialize(RectTransform overlay,TMP_FontAsset textFont)
    {
        if(tooltip){if(Application.isPlaying)Destroy(tooltip.gameObject);else DestroyImmediate(tooltip.gameObject);}
        tooltipRoot=overlay;font=textFont;
        if(!overlay)return; // Display-only world-space row: no detached tooltip.
        if(Templates && Templates.StatusTooltip){
            tooltipView=Instantiate(Templates.StatusTooltip,overlay,false);tooltip=(RectTransform)tooltipView.transform;tooltipText=tooltipView.Details;tooltip.name="Status tooltip";
            tooltip.gameObject.SetActive(false);return;
        }
        tooltip=NewRect("Status tooltip",overlay,new Vector2(280,130));tooltip.gameObject.AddComponent<Image>().color=new Color(.025f,.035f,.05f,.98f);
        tooltip.GetComponent<Image>().raycastTarget=false;tooltip.pivot=new Vector2(.5f,1);
        tooltipText=NewRect("Details",tooltip,Vector2.zero).gameObject.AddComponent<TextMeshProUGUI>();
        var textRect=tooltipText.rectTransform;textRect.anchorMin=Vector2.zero;textRect.anchorMax=Vector2.one;textRect.offsetMin=new Vector2(12,10);textRect.offsetMax=new Vector2(-12,-10);
        tooltipText.font=font;tooltipText.fontSize=13;tooltipText.color=Color.white;tooltipText.alignment=TextAlignmentOptions.TopLeft;tooltipText.raycastTarget=false;
        tooltip.gameObject.SetActive(false);
    }
    static RectTransform NewRect(string name,Transform parent,Vector2 size)
    {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.sizeDelta=size;return r;}
    public void Show(GridUnit state,bool objectClock=false)
    {
        var next=StatusPresentation.For(state,objectClock);
        if(hovered>=0 && (hovered>=next.Count || hovered>=statuses.Count || next[hovered].Name!=statuses[hovered].Name))Exit();
        statuses=next;
        for(int i=0;i<statuses.Count;i++)
        {
            if(i==entries.Count)
            {
                int index=i;
                RectTransform r;StatusBadgeGraphic graphic;TMP_Text count;
                if(Templates && Templates.StatusBadge){var view=Instantiate(Templates.StatusBadge,transform,false);r=(RectTransform)view.transform;graphic=view.Graphic;count=view.Count;}
                else {
                r=NewRect("Status",transform,new Vector2(30,30));
                r.gameObject.AddComponent<CanvasRenderer>();
                graphic=r.gameObject.AddComponent<StatusBadgeGraphic>();
                count=NewRect("Remaining",r,new Vector2(32,13)).gameObject.AddComponent<TextMeshProUGUI>();count.rectTransform.anchoredPosition=new Vector2(0,-12);
                count.font=font;count.fontSize=11;count.fontStyle=FontStyles.Bold;count.color=Color.white;count.alignment=TextAlignmentOptions.Center;count.outlineWidth=.25f;count.outlineColor=Color.black;count.raycastTarget=false;
                }
                var hover=r.gameObject.AddComponent<StatusBadgeHover>();hover.Enter=()=>{hovered=index;DrawTooltip();};hover.Exit=Exit;
                entries.Add(new Entry{Rect=r,Graphic=graphic,Count=count,Hover=hover});
            }
            var entry=entries[i];entry.Rect.gameObject.SetActive(true);entry.Rect.name=statuses[i].Name;
            float size=Mathf.Clamp(IconSize,12,64);
            if(Templates && Templates.StatusBadge){var authored=(RectTransform)Templates.StatusBadge.transform;entry.Rect.localScale=Vector3.one*(size/Mathf.Max(1,authored.rect.width));}
            else entry.Rect.sizeDelta=new Vector2(size,size);
            entry.Rect.anchoredPosition=new Vector2((i-(statuses.Count-1)*.5f)*(size+Spacing),0);
            if(!(Templates && Templates.StatusBadge)){entry.Count.fontSize=size<24?8:11;entry.Count.rectTransform.anchoredPosition=new Vector2(0,-size*.3f);
            entry.Count.rectTransform.sizeDelta=new Vector2(size+4,size<24?10:13);}
            entry.Graphic.Symbol=statuses[i].Symbol;entry.Graphic.SetVerticesDirty();entry.Count.text=statuses[i].Count;
        }
        for(int i=statuses.Count;i<entries.Count;i++)entries[i].Rect.gameObject.SetActive(false);
        DrawTooltip();
    }
    void DrawTooltip()
    {
        if(!tooltip)return;
        bool show=isActiveAndEnabled && hovered>=0 && hovered<statuses.Count;tooltip.gameObject.SetActive(show);if(!show)return;
        tooltipText.text=statuses[hovered].Tooltip;
        float width=tooltipView?((RectTransform)tooltipView.transform).rect.width:280;
        float inset=tooltipText.rectTransform.offsetMin.x-tooltipText.rectTransform.offsetMax.x;
        float height=tooltipText.GetPreferredValues(tooltipText.text,Mathf.Max(30,width-inset),0).y+(tooltipView?tooltipView.VerticalPadding:24);tooltip.sizeDelta=new Vector2(width,height);
        Vector3 local=tooltipRoot.InverseTransformPoint(entries[hovered].Rect.position);
        tooltip.localPosition=new Vector3(Mathf.Clamp(local.x,tooltipRoot.rect.xMin+width*.5f+4,tooltipRoot.rect.xMax-width*.5f-4),Mathf.Clamp(local.y-24,tooltipRoot.rect.yMin+height+4,tooltipRoot.rect.yMax-4),0);
        tooltip.SetAsLastSibling();
    }
    void Exit(){hovered=-1;if(tooltip)tooltip.gameObject.SetActive(false);}
    void OnDisable()=>Exit();
    void OnDestroy(){if(tooltip){if(Application.isPlaying)Destroy(tooltip.gameObject);else DestroyImmediate(tooltip.gameObject);}}
}
public sealed class StatusBadgeHover : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerClickHandler
{
    public System.Action Enter,Exit;
    public void OnPointerEnter(PointerEventData data)=>Enter?.Invoke();
    public void OnPointerExit(PointerEventData data)=>Exit?.Invoke();
    public void OnPointerClick(PointerEventData data) { } // Clicking an icon must not dismiss Examine or issue an attack.
}
