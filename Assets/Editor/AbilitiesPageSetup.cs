using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class AbilitiesPageSetup
{
    const string Folder="Assets/Resources/UI/Abilities";
    static Color Ink=new Color(.88f,.94f,1),Panel=new Color(.035f,.075f,.12f);
    [MenuItem("Tools/Game/UI/Create missing Abilities templates")]
    public static void Create(){
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
        Directory.CreateDirectory(Folder);
        var category=Make<InventoryButtonView>("CategoryButton",r=>{
            r.sizeDelta=new Vector2(110,36);var v=r.GetComponent<InventoryButtonView>();v.Select=Button(r);v.Label=Text("Label",r,"Attacks",16,10,6,90,24);Width(v.Label.rectTransform,10,10);
            var l=r.gameObject.AddComponent<LayoutElement>();l.preferredWidth=110;l.preferredHeight=36;l.flexibleWidth=1;
        });
        var family=Make<InventoryButtonView>("SpellFamily",r=>{
            r.sizeDelta=new Vector2(200,42);var v=r.GetComponent<InventoryButtonView>();v.Select=Button(r);v.Label=Text("Label",r,"Fireball",16,10,8,180,26);Width(v.Label.rectTransform,10,10);r.gameObject.AddComponent<LayoutElement>().preferredHeight=42;
        });
        var row=Make<AbilityRowView>("AbilityRow",r=>{
            r.sizeDelta=new Vector2(300,66);var v=r.GetComponent<AbilityRowView>();v.Select=Button(r);
            v.Title=Text("Ability name",r,"Fireball II",16,10,6,280,28);Width(v.Title.rectTransform,10,10);
            v.Source=Text("Source or mastery",r,"Mastery level 2",12,10,36,280,24);Width(v.Source.rectTransform,10,10);
            r.gameObject.AddComponent<LayoutElement>().preferredHeight=66;
        });
        var favorite=Make<AbilityFavoriteView>("FavoriteSlot",r=>{
            r.sizeDelta=new Vector2(280,48);var v=r.GetComponent<AbilityFavoriteView>();v.Select=Button(r);
            v.Name=Text("Ability name",r,"Empty",14,10,8,160,32);Width(v.Name.rectTransform,10,108);v.Name.enableAutoSizing=true;v.Name.fontSizeMin=10;v.Name.fontSizeMax=14;
            var icons=Rect("Binding icons",r,0,0,92,30);icons.anchorMin=icons.anchorMax=icons.pivot=new Vector2(1,.5f);icons.anchoredPosition=new Vector2(-12,0);
            v.Trigger=Rect("Trigger",icons,0,0,30,30).gameObject.AddComponent<Image>();v.Trigger.raycastTarget=false;
            v.Plus=Text("Plus",icons,"+",14,33,0,14,30);
            v.Key=Rect("Key",icons,55,0,30,30).gameObject.AddComponent<Image>();v.Key.raycastTarget=false;
            v.Fallback=Text("Fallback",icons,"1",14,52,0,40,30);
        });
        var workspace=Make<AbilitiesWorkspaceView>("Workspace",r=>{
            r.sizeDelta=new Vector2(590,562);var v=r.GetComponent<AbilitiesWorkspaceView>();
            v.Categories=Rect("Category tabs",r,0,0,590,36);Width(v.Categories);var tabs=v.Categories.gameObject.AddComponent<HorizontalLayoutGroup>();tabs.spacing=6;tabs.childControlWidth=tabs.childControlHeight=true;tabs.childForceExpandWidth=true;
            var search=Rect("Search",r,0,48,590,36);Width(search);search.gameObject.AddComponent<Image>().color=Panel;v.Search=search.gameObject.AddComponent<TMP_InputField>();
            var text=Text("Search text",search,"",16,10,4,570,28);Width(text.rectTransform,10,10);
            var hint=Text("Placeholder",search,"Search abilities…",14,10,4,570,28);Width(hint.rectTransform,10,10);hint.color=new Color(.6f,.7f,.75f);
            v.Search.textViewport=search;v.Search.textComponent=text;v.Search.placeholder=hint;
            v.Browser=Rect("Browser",r,0,100,590,320);v.Browser.anchorMin=Vector2.zero;v.Browser.anchorMax=Vector2.one;v.Browser.offsetMin=new Vector2(0,142);v.Browser.offsetMax=new Vector2(0,-100);
            v.Families=Scroll("Spell families",v.Browser,0,0,240,320);var f=(RectTransform)v.Families.transform;f.anchorMin=Vector2.zero;f.anchorMax=new Vector2(.42f,1);f.offsetMin=Vector2.zero;f.offsetMax=new Vector2(-6,0);List(v.Families);
            v.Entries=Scroll("Ability entries",v.Browser,0,0,340,320);var e=(RectTransform)v.Entries.transform;e.anchorMin=new Vector2(.42f,0);e.anchorMax=Vector2.one;e.offsetMin=new Vector2(6,0);e.offsetMax=Vector2.zero;List(v.Entries);
            var heading=Text("Favorites heading",r,"FAVORITE SHORTCUTS",14,0,0,590,26).rectTransform;heading.anchorMin=new Vector2(0,0);heading.anchorMax=new Vector2(1,0);heading.anchoredPosition=new Vector2(0,130);heading.sizeDelta=new Vector2(0,26);
            v.Favorites=Rect("Favorite slots",r,0,0,590,102);v.Favorites.anchorMin=new Vector2(0,0);v.Favorites.anchorMax=new Vector2(1,0);v.Favorites.anchoredPosition=new Vector2(0,102);v.Favorites.sizeDelta=new Vector2(0,102);
            var grid=v.Favorites.gameObject.AddComponent<GridLayoutGroup>();grid.cellSize=new Vector2(292,48);grid.spacing=new Vector2(6,6);grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=2;
        });
        string path=Folder+"/Abilities.asset";
        if(!AssetDatabase.LoadAssetAtPath<AbilitiesPageLayout>(path)){var a=ScriptableObject.CreateInstance<AbilitiesPageLayout>();a.Workspace=workspace;a.CategoryButton=category;a.FamilyButton=family;a.AbilityRow=row;a.FavoriteSlot=favorite;AssetDatabase.CreateAsset(a,path);}
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
    }
    static void List(ScrollRect scroll){var group=scroll.content.gameObject.AddComponent<VerticalLayoutGroup>();group.spacing=6;group.padding=new RectOffset(4,4,4,4);group.childControlWidth=group.childControlHeight=true;group.childForceExpandWidth=true;group.childForceExpandHeight=false;}
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
[CustomEditor(typeof(AbilitiesPageLayout))]
public sealed class AbilitiesPageLayoutEditor:Editor {
    public override void OnInspectorGUI(){if(GUILayout.Button("Open live preview"))DetailsPanelPreviewWindow.Open((AbilitiesPageLayout)target);DrawDefaultInspector();EditorGUILayout.HelpBox("Edit the Workspace and reusable row, category, family and favorite prefabs in Prefab Mode. Keep binding references assigned. Categories can be reordered here.",MessageType.Info);}
}
