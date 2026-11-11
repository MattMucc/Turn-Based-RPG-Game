using UnityEngine;
using UnityEngine.UI;

// Code-drawn node frames keep active/passive shapes legible without requiring placeholder art.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class SkillTreeNodeGraphic : MaskableGraphic
{
    public bool Passive;
    public bool Selected;
    public Color Border = Color.gray;
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();var rect=rectTransform.rect;Vector2 center=rect.center;
        float radius=Mathf.Min(rect.width,rect.height)*.5f;
        int sides=Passive?4:40;
        var outer=Selected?new Color(.4f,.9f,1):Border;
        Disc(mesh,center,radius,sides,outer);
        Disc(mesh,center,Mathf.Max(0,radius-(Selected?4:2)),sides,color);
    }
    static void Disc(VertexHelper mesh,Vector2 center,float radius,int sides,Color tint)
    {
        int start=mesh.currentVertCount;mesh.AddVert(center,tint,Vector2.zero);
        for(int i=0;i<sides;i++){float angle=(90+i*360f/sides)*Mathf.Deg2Rad;mesh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,tint,Vector2.zero);}
        for(int i=0;i<sides;i++)mesh.AddTriangle(start,start+1+i,start+1+(i+1)%sides);
    }
}
