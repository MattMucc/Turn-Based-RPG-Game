using UnityEngine;
using UnityEngine.UI;
public static class InventoryModularUI
{
    public static T Fill<T>(T prefab,Transform parent) where T:Component
    {
        var view=Object.Instantiate(prefab,parent,false);var rect=(RectTransform)view.transform;
        rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;return view;
    }
    public static InventoryButtonView Choice(InventoryButtonView prefab,Transform parent,string text)
    {
        var view=Object.Instantiate(prefab,parent,false);view.name=text;view.Select.name=text;view.Label.text=text;return view;
    }
    public static void FitFilters(InventoryWorkspaceView view){
        if(!view.FitFilterColumns)return;
        var grid=view.Filters;int columns=Mathf.Max(1,grid.constraintCount);
        float width=((RectTransform)grid.transform).rect.width;
        grid.cellSize=new Vector2(Mathf.Max(20,(width-grid.padding.horizontal-grid.spacing.x*(columns-1))/columns),grid.cellSize.y);
    }
    public static int Columns(GridLayoutGroup grid,float width)
        =>grid.constraint==GridLayoutGroup.Constraint.FixedColumnCount?Mathf.Max(1,grid.constraintCount):Mathf.Max(1,Mathf.FloorToInt((width-grid.padding.horizontal+grid.spacing.x)/(grid.cellSize.x+grid.spacing.x)));
    public static void SizeGrid(GridLayoutGroup grid,int count,float width)
    {
        int columns=Columns(grid,width),rows=Mathf.CeilToInt(count/(float)columns);
        var rect=(RectTransform)grid.transform;rect.sizeDelta=new Vector2(rect.sizeDelta.x,grid.padding.vertical+rows*grid.cellSize.y+Mathf.Max(0,rows-1)*grid.spacing.y);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }
    public static void SizeContext(InventoryContextView view)
    {
        var root=(RectTransform)view.transform;float top=-view.Choices.anchoredPosition.y;
        LayoutRebuilder.ForceRebuildLayoutImmediate(view.Choices);
        float height=LayoutUtility.GetPreferredHeight(view.Choices);
        view.Choices.sizeDelta=new Vector2(view.Choices.sizeDelta.x,height);
        root.sizeDelta=new Vector2(root.sizeDelta.x,top+height+8);
    }
    public static void SizeDescription(InventoryDetailsView view)
    {
        Canvas.ForceUpdateCanvases();
        var text=view.Description;float h=Mathf.Max(view.Scroll.viewport.rect.height,text.GetPreferredValues(text.text,text.rectTransform.rect.width,0).y+16);
        view.Scroll.content.sizeDelta=new Vector2(view.Scroll.content.sizeDelta.x,h);
    }
}
