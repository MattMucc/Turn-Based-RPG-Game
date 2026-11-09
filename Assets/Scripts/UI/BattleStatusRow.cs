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
    int hovered=-1;
    List<StatusPresentation> statuses=new();
    public bool PointerInside=>hovered>=0;
    public void Initialize(RectTransform overlay,TMP_FontAsset textFont)
    {
        tooltipRoot=overlay;font=textFont;
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
                int index=i;var r=NewRect("Status",transform,new Vector2(30,30));
                var graphic=r.gameObject.AddComponent<StatusBadgeGraphic>();
                var count=NewRect("Remaining",r,new Vector2(32,13)).gameObject.AddComponent<TextMeshProUGUI>();count.rectTransform.anchoredPosition=new Vector2(0,-12);
                count.font=font;count.fontSize=11;count.fontStyle=FontStyles.Bold;count.color=Color.white;count.alignment=TextAlignmentOptions.Center;count.outlineWidth=.25f;count.outlineColor=Color.black;count.raycastTarget=false;
                var hover=r.gameObject.AddComponent<StatusBadgeHover>();hover.Enter=()=>{hovered=index;DrawTooltip();};hover.Exit=Exit;
                entries.Add(new Entry{Rect=r,Graphic=graphic,Count=count,Hover=hover});
            }
            var entry=entries[i];entry.Rect.gameObject.SetActive(true);entry.Rect.name=statuses[i].Name;
            entry.Rect.anchoredPosition=new Vector2((i-(statuses.Count-1)*.5f)*36,0);
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
        float height=tooltipText.GetPreferredValues(tooltipText.text,256,0).y+24;tooltip.sizeDelta=new Vector2(280,height);
        Vector3 local=tooltipRoot.InverseTransformPoint(entries[hovered].Rect.position);
        tooltip.anchoredPosition=new Vector2(Mathf.Clamp(local.x,tooltipRoot.rect.xMin+144,tooltipRoot.rect.xMax-144),Mathf.Clamp(local.y-24,tooltipRoot.rect.yMin+height+4,tooltipRoot.rect.yMax-4));
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
public sealed class StatusBadgeGraphic : MaskableGraphic
{
    public int Symbol;
    static readonly Color[] colors={new Color(1,.35f,.1f),new Color(.3f,.9f,.45f),new Color(1,.85f,.2f),new Color(.3f,.65f,1),new Color(.8f,.85f,.95f)};
    void Polygon(VertexHelper vh,Vector2[] points,Color tint)
    {
        int start=vh.currentVertCount;Vector2 center=Vector2.zero;foreach(var p in points)center+=p;center/=points.Length;
        vh.AddVert(center,tint,Vector2.zero);foreach(var p in points)vh.AddVert(p,tint,Vector2.zero);
        for(int i=0;i<points.Length;i++)vh.AddTriangle(start,start+1+i,start+1+(i+1)%points.Length);
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();float radius=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;
        var rim=new Vector2[40];for(int i=0;i<40;i++)rim[i]=new Vector2(Mathf.Cos(i*Mathf.PI/20),Mathf.Sin(i*Mathf.PI/20))*radius;
        Polygon(vh,rim,colors[Mathf.Clamp(Symbol,0,4)]);
        for(int i=0;i<40;i++)rim[i]*=.87f;Polygon(vh,rim,new Color(.065f,.085f,.12f));
        Vector2[] shape=Symbol==0 ? new[]{new Vector2(-5,-5),new Vector2(-7,0),new Vector2(-3,5),new Vector2(-1,9),new Vector2(4,4),new Vector2(6,-2),new Vector2(3,-6)}
            : Symbol==2 ? new[]{new Vector2(1,9),new Vector2(-6,-1),new Vector2(-1,-1),new Vector2(-3,-8),new Vector2(6,3),new Vector2(1,3)}
            : new[]{new Vector2(-7,6),new Vector2(7,6),new Vector2(5,-3),new Vector2(0,-7),new Vector2(-5,-3)};
        if(Symbol==1)
        {
            Polygon(vh,new[]{new Vector2(-2,7),new Vector2(2,7),new Vector2(2,-6),new Vector2(-2,-6)},colors[1]);
            Polygon(vh,new[]{new Vector2(-7,3),new Vector2(7,3),new Vector2(7,-1),new Vector2(-7,-1)},colors[1]);
        }
        else Polygon(vh,shape,colors[Mathf.Clamp(Symbol,0,4)]);
    }
}
