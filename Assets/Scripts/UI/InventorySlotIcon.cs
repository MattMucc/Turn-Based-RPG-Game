using UnityEngine;
using UnityEngine.UI;

// Lightweight vector placeholders until individual item artwork is authored.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class InventorySlotIcon : MaskableGraphic
{
    public string Kind;
    public bool Mana;
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();var r=rectTransform.rect;
        void Polygon(Color tint,params Vector2[] points) {
            int start=mesh.currentVertCount;
            foreach(var p in points)mesh.AddVert(new Vector3(r.xMin+p.x*r.width,r.yMin+p.y*r.height,0),tint,Vector2.zero);
            for(int i=1;i<points.Length-1;i++)mesh.AddTriangle(start,start+i,start+i+1);
        }
        void Box(float x,float y,float w,float h,Color tint)=>Polygon(tint,new(x,y),new(x,y+h),new(x+w,y+h),new(x+w,y));
        Color metal=new(.72f,.83f,.91f), gold=new(.95f,.73f,.28f), dark=new(.12f,.19f,.26f);
        if(Kind=="Weapons") {
            Polygon(metal,new(.43f,.35f),new(.43f,.78f),new(.5f,.94f),new(.57f,.78f),new(.57f,.35f));
            Box(.28f,.29f,.44f,.09f,gold);Box(.46f,.08f,.08f,.21f,new Color(.5f,.29f,.18f));Box(.42f,.04f,.16f,.07f,gold);
        } else if(Kind=="Armor") {
            Polygon(metal,new(.16f,.73f),new(.3f,.89f),new(.4f,.81f),new(.6f,.81f),new(.7f,.89f),new(.84f,.73f),new(.72f,.58f),new(.67f,.15f),new(.33f,.15f),new(.28f,.58f));
            Box(.47f,.22f,.06f,.52f,dark);Box(.32f,.24f,.36f,.07f,gold);
        } else if(Kind=="Accessories") {
            for(int i=0;i<24;i++) {
                float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;
                Polygon(gold,new(.5f+Mathf.Cos(a)*.31f,.43f+Mathf.Sin(a)*.31f),new(.5f+Mathf.Cos(b)*.31f,.43f+Mathf.Sin(b)*.31f),new(.5f+Mathf.Cos(b)*.22f,.43f+Mathf.Sin(b)*.22f),new(.5f+Mathf.Cos(a)*.22f,.43f+Mathf.Sin(a)*.22f));
            }
            Polygon(new Color(.35f,.85f,1),new(.35f,.77f),new(.5f,.94f),new(.65f,.77f),new(.5f,.62f));
        } else if(Kind=="Spell Books") {
            Box(.19f,.14f,.62f,.7f,new Color(.4f,.26f,.66f));Box(.26f,.2f,.51f,.56f,new Color(.87f,.84f,.7f));Box(.19f,.14f,.1f,.7f,gold);
            Polygon(new Color(.85f,.42f,.17f),new(.43f,.36f),new(.63f,.65f),new(.66f,.35f));
        } else if(Kind=="Materials") {
            Polygon(metal,new(.12f,.27f),new(.27f,.62f),new(.7f,.77f),new(.89f,.43f),new(.7f,.18f));
            Polygon(new Color(.38f,.57f,.7f),new(.12f,.27f),new(.7f,.18f),new(.89f,.43f),new(.41f,.4f));
        } else if(Kind=="Consumables") {
            Box(.39f,.63f,.22f,.2f,metal);Box(.36f,.81f,.28f,.08f,gold);
            Polygon(metal,new(.37f,.65f),new(.63f,.65f),new(.8f,.23f),new(.68f,.12f),new(.32f,.12f),new(.2f,.23f));
            Polygon(Mana?new Color(.2f,.5f,1):new Color(.92f,.22f,.27f),new(.34f,.48f),new(.66f,.48f),new(.74f,.24f),new(.65f,.18f),new(.35f,.18f),new(.26f,.24f));
            Box(.34f,.25f,.055f,.17f,new Color(1,1,1,.7f));
        } else {
            Box(.19f,.2f,.62f,.55f,new Color(.55f,.37f,.2f));Box(.45f,.2f,.1f,.55f,gold);Box(.19f,.64f,.62f,.08f,gold);
        }
    }
}
