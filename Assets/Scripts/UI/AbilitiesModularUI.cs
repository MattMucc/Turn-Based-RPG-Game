using UnityEngine;
using UnityEngine.UI;
public static class AbilitiesModularUI
{
    public static void SizeList(ScrollRect scroll){
        LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
        scroll.content.sizeDelta=new Vector2(scroll.content.sizeDelta.x,Mathf.Max(scroll.viewport.rect.height,LayoutUtility.GetPreferredHeight(scroll.content)));
        LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
    }
    public static void FamilyMode(AbilitiesWorkspaceView view,AbilitiesWorkspaceView template,bool visible){
        view.Families.gameObject.SetActive(visible);
        var r=(RectTransform)view.Entries.transform;var t=(RectTransform)template.Entries.transform;
        if(visible){r.anchorMin=t.anchorMin;r.anchorMax=t.anchorMax;r.offsetMin=t.offsetMin;r.offsetMax=t.offsetMax;}
        else {r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    }
    public static void FitFavorites(RectTransform root){
        var grid=root.GetComponent<GridLayoutGroup>();if(!grid)return;
        int columns=Mathf.Max(1,grid.constraintCount);
        grid.cellSize=new Vector2(Mathf.Max(40,(root.rect.width-grid.padding.horizontal-(columns-1)*grid.spacing.x)/columns),grid.cellSize.y);
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }
}
