using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class DropdownArrow : MaskableGraphic
{
    TMP_Dropdown dropdown;
    public void Bind(TMP_Dropdown source){dropdown=source;color=Color.white;raycastTarget=false;Refresh();}
    public void Refresh(){rectTransform.localRotation=Quaternion.Euler(0,0,dropdown&&dropdown.IsExpanded?-90:0);}
    void LateUpdate()=>Refresh();
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var r=rectTransform.rect;float s=Mathf.Min(r.width,r.height)*.3f;
        var c=r.center;vh.AddVert(c+new Vector2(-s,-s),color,Vector2.zero);
        vh.AddVert(c+new Vector2(-s,s),color,Vector2.zero);
        vh.AddVert(c+new Vector2(s,0),color,Vector2.zero);vh.AddTriangle(0,1,2);
    }
}
