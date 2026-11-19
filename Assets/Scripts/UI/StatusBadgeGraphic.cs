using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(CanvasRenderer))]
public sealed class StatusBadgeGraphic : MaskableGraphic
{
    public int Symbol;
    static readonly Color[] colors={new Color(1,.35f,.1f),new Color(.3f,.9f,.45f),new Color(1,.85f,.2f),new Color(.3f,.65f,1),new Color(.8f,.85f,.95f),new Color(.65f,.45f,.85f),new Color(1,.65f,.3f),new Color(1,.8f,.25f),new Color(.3f,.8f,.9f),new Color(.85f,.4f,.65f)};
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
        Polygon(vh,rim,colors[Mathf.Clamp(Symbol,0,colors.Length-1)]);
        for(int i=0;i<40;i++)rim[i]*=.87f;Polygon(vh,rim,new Color(.065f,.085f,.12f));
        Vector2[] shape=Symbol==0 ? new[]{new Vector2(-5,-5),new Vector2(-7,0),new Vector2(-3,5),new Vector2(-1,9),new Vector2(4,4),new Vector2(6,-2),new Vector2(3,-6)}
            : Symbol==2 ? new[]{new Vector2(1,9),new Vector2(-6,-1),new Vector2(-1,-1),new Vector2(-3,-8),new Vector2(6,3),new Vector2(1,3)}
            : new[]{new Vector2(-7,6),new Vector2(7,6),new Vector2(5,-3),new Vector2(0,-7),new Vector2(-5,-3)};
        if(Symbol>=7){
            Polygon(vh,new[]{new Vector2(-5,7),new Vector2(5,7),new Vector2(5,-6),new Vector2(-5,-6)},colors[Mathf.Clamp(Symbol,0,colors.Length-1)]);
            Polygon(vh,new[]{new Vector2(-3,2),new Vector2(3,2),new Vector2(3,0),new Vector2(-3,0)},new Color(.065f,.085f,.12f));
        }
        else if(Symbol==1)
        {
            Polygon(vh,new[]{new Vector2(-2,7),new Vector2(2,7),new Vector2(2,-6),new Vector2(-2,-6)},colors[1]);
            Polygon(vh,new[]{new Vector2(-7,3),new Vector2(7,3),new Vector2(7,-1),new Vector2(-7,-1)},colors[1]);
        }
        else Polygon(vh,shape,colors[Mathf.Clamp(Symbol,0,colors.Length-1)]);
    }
}
