using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class InventoryPageSetup
{
    const string Folder="Assets/Resources/UI/Inventory";
    static Color Ink=new Color(.88f,.94f,1),Panel=new Color(.035f,.075f,.12f);
    [MenuItem("Tools/Game/UI/Create missing Inventory templates")]
    public static void Create()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
        Directory.CreateDirectory(Folder);
        var filter=Make<InventoryButtonView>("FilterButton",r=>{var v=r.GetComponent<InventoryButtonView>();v.Select=Button(r);v.Label=Text("Label",r,"Weapons",13,8,4,108,22);r.sizeDelta=new Vector2(124,30);});
        var choice=Make<InventoryButtonView>("ContextButton",r=>{var v=r.GetComponent<InventoryButtonView>();v.Select=Button(r);v.Label=Text("Label",r,"Examine",16,10,4,208,26);Width(v.Label.rectTransform,10,10);r.sizeDelta=new Vector2(228,34);r.gameObject.AddComponent<LayoutElement>().preferredHeight=34;});
        var slot=Make<InventorySlotView>("ItemSlot",r=>{
            r.sizeDelta=new Vector2(72,72);var v=r.GetComponent<InventorySlotView>();v.Select=Button(r);v.DropOutline=r.gameObject.AddComponent<Outline>();v.DropOutline.effectColor=Color.cyan;v.DropOutline.enabled=false;
            v.Icon=Rect("Icon",r,8,8,56,56);v.Quantity=Text("Quantity or upgrade",r,"3",13,4,49,64,20);v.Quantity.alignment=TextAlignmentOptions.BottomRight;
            v.Equipped=Text("Equipped marker",r,"E",13,5,3,22,20);v.Equipped.color=Color.cyan;
        });
        var workspace=Make<InventoryWorkspaceView>("Workspace",r=>{
            r.sizeDelta=new Vector2(590,562);var v=r.GetComponent<InventoryWorkspaceView>();
            v.Heading=Text("Inventory heading",r,"SHARED INVENTORY",18,0,0,590,30);Width(v.Heading.rectTransform);
            v.Count=Text("Inventory count",r,"12 stacks / equipment copies",13,0,108,590,24);Width(v.Count.rectTransform);
            var filters=Rect("Filters",r,0,40,590,66);Width(filters);v.Filters=filters.gameObject.AddComponent<GridLayoutGroup>();v.Filters.cellSize=new Vector2(138,30);v.Filters.spacing=new Vector2(6,6);v.Filters.constraint=GridLayoutGroup.Constraint.FixedColumnCount;v.Filters.constraintCount=4;
            v.Scroll=Scroll("Items",r,0,138,590,424);var viewport=v.Scroll.viewport;viewport.anchorMin=Vector2.zero;viewport.anchorMax=Vector2.one;viewport.offsetMin=Vector2.zero;viewport.offsetMax=new Vector2(0,-138);
            v.Grid=v.Scroll.content.gameObject.AddComponent<GridLayoutGroup>();v.Grid.cellSize=new Vector2(72,72);v.Grid.spacing=new Vector2(8,8);
        });
        var details=Make<InventoryDetailsView>("Details",r=>{
            r.sizeDelta=new Vector2(426,562);var v=r.GetComponent<InventoryDetailsView>();
            v.Name=Text("Detail name",r,"Health Potion",23,12,0,402,58);Width(v.Name.rectTransform,12,12);
            var frame=Rect("Selected item image",r,12,68,96,96);frame.gameObject.AddComponent<Image>().color=new Color(.06f,.12f,.17f);
            v.Artwork=Rect("Artwork",frame,10,10,76,76);
            v.Category=Text("Item category",r,"Consumables",14,126,72,286,30);Width(v.Category.rectTransform,126,14);
            v.Stock=Text("Item stock",r,"Carried: 3",16,126,108,286,52);Width(v.Stock.rectTransform,126,14);
            v.Scroll=Scroll("Inventory details",r,12,184,402,366);var vp=v.Scroll.viewport;vp.anchorMin=Vector2.zero;vp.anchorMax=Vector2.one;vp.offsetMin=new Vector2(12,12);vp.offsetMax=new Vector2(-12,-184);
            v.Description=Text("Description",v.Scroll.content,"Restore health to a party member.",16,8,8,386,350);var t=v.Description.rectTransform;t.anchorMin=Vector2.zero;t.anchorMax=Vector2.one;t.offsetMin=t.offsetMax=new Vector2(8,-8);t.offsetMax=new Vector2(-8,-8);
        });
        var context=Make<InventoryContextView>("ContextMenu",r=>{
            r.sizeDelta=new Vector2(244,100);r.gameObject.AddComponent<Image>().color=Panel;var v=r.GetComponent<InventoryContextView>();
            v.Name=Text("Item name",r,"Health Potion",16,12,8,220,30);Width(v.Name.rectTransform,12,12);v.Name.enableAutoSizing=true;v.Name.fontSizeMin=11;v.Name.fontSizeMax=16;
            v.Choices=Rect("Choices",r,8,44,228,48);Width(v.Choices,8,8);v.Layout=v.Choices.gameObject.AddComponent<VerticalLayoutGroup>();v.Layout.spacing=4;v.Layout.childControlWidth=v.Layout.childControlHeight=true;v.Layout.childForceExpandWidth=true;v.Layout.childForceExpandHeight=false;
        });
        string path=Folder+"/Inventory.asset";
        if(!AssetDatabase.LoadAssetAtPath<InventoryPageLayout>(path)){var a=ScriptableObject.CreateInstance<InventoryPageLayout>();a.Workspace=workspace;a.Slot=slot;a.FilterButton=filter;a.ContextButton=choice;a.Details=details;a.Context=context;AssetDatabase.CreateAsset(a,path);}
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
    }
    static T Make<T>(string name,Action<RectTransform> build)where T:Component{
        string path=Folder+"/"+name+".prefab";var old=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(old)return old.GetComponent<T>();
        var root=Rect(name,null,0,0,100,100);root.gameObject.AddComponent<T>();
        try{build(root);return PrefabUtility.SaveAsPrefabAsset(root.gameObject,path).GetComponent<T>();}finally{UnityEngine.Object.DestroyImmediate(root.gameObject);}
    }
    static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();if(parent)r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
    static void Width(RectTransform r,float left=0,float right=0){r.anchorMax=new Vector2(1,1);r.offsetMin=new Vector2(left,r.offsetMin.y);r.offsetMax=new Vector2(-right,r.offsetMax.y);}
    static TMP_Text Text(string name,Transform parent,string value,int size,float x,float y,float w,float h){var t=Rect(name,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.text=value;t.fontSize=size;t.color=Ink;t.raycastTarget=false;return t;}
    static Button Button(RectTransform r){var image=r.gameObject.AddComponent<Image>();image.color=new Color(.08f,.17f,.25f);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;return b;}
    static ScrollRect Scroll(string name,Transform parent,float x,float y,float w,float h){var vp=Rect(name,parent,x,y,w,h);vp.gameObject.AddComponent<Image>().color=new Color(.025f,.05f,.08f);vp.gameObject.AddComponent<RectMask2D>();var s=vp.gameObject.AddComponent<ScrollRect>();s.viewport=vp;s.content=Rect("Content",vp,0,0,w,h);Width(s.content);s.horizontal=false;s.scrollSensitivity=45;s.movementType=ScrollRect.MovementType.Clamped;return s;}
}
[CustomEditor(typeof(InventoryPageLayout))]
public sealed class InventoryPageLayoutEditor:Editor
{
    public override void OnInspectorGUI(){
        if(GUILayout.Button("Open live preview"))DetailsPanelPreviewWindow.Open((InventoryPageLayout)target);
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("Reorder Filters here. Edit Workspace, ItemSlot, FilterButton, Details, ContextMenu and ContextButton in Prefab Mode. Grid/filters use GridLayoutGroup; popup choices use VerticalLayoutGroup. Keep binding references assigned.",MessageType.Info);
    }
}
