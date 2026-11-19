using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class SystemPageSetup {
    const string Folder="Assets/Resources/UI/System";
    static Color Ink=new Color(.88f,.94f,1),Panel=new Color(.035f,.075f,.12f);
    [MenuItem("Tools/Game/UI/Create missing System templates")]
    public static void Create(){
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
        Directory.CreateDirectory(Folder);
        var tab=Make<InventoryButtonView>("Tab",r=>{r.sizeDelta=new Vector2(110,38);var v=r.GetComponent<InventoryButtonView>();v.Select=Button(r);v.Label=Text("Label",r,"Save / Load",13,8,6,94,26);Width(v.Label.rectTransform,8,8);v.Label.enableAutoSizing=true;v.Label.fontSizeMin=10;v.Label.fontSizeMax=13;var le=r.gameObject.AddComponent<LayoutElement>();le.flexibleWidth=1;le.preferredWidth=110;});
        var workspace=Make<SystemWorkspaceView>("Workspace",r=>{
            r.sizeDelta=new Vector2(590,562);var v=r.GetComponent<SystemWorkspaceView>();v.Tabs=Rect("Tabs",r,0,0,590,38);Width(v.Tabs);var group=v.Tabs.gameObject.AddComponent<HorizontalLayoutGroup>();group.spacing=6;group.childControlWidth=group.childControlHeight=true;group.childForceExpandWidth=true;
            v.Content=Rect("Tab content",r,0,54,590,508);v.Content.anchorMin=Vector2.zero;v.Content.anchorMax=Vector2.one;v.Content.offsetMin=Vector2.zero;v.Content.offsetMax=new Vector2(0,-54);
        });
        var home=Make<SystemPanelView>("SaveLoad",r=>{PanelSize(r);Label(r,"Save title","SAVE / LOAD",0,32,22);Choice(r,"Save Game",60);Choice(r,"Load Game",110);Choice(r,"Load Last Checkpoint",160);});
        var battle=Make<SystemPanelView>("Battle",r=>{PanelSize(r);Choice(r,"Defense timing",12,"Defense timing: Standard");Choice(r,"Automatic battle camera",64,"Automatic battle camera: On");Choice(r,"Restore defaults",128);});
        var graphics=Make<SystemPanelView>("Graphics",r=>{PanelSize(r);Label(r,"Mode label","Display mode",12);Dropdown(r,"Display mode",48,new[]{"Windowed","Borderless","Fullscreen"});Choice(r,"Apply display change",104);});
        var ui=Make<SystemPanelView>("UI",r=>{PanelSize(r);Label(r,"Icon label","Input icons",12);Dropdown(r,"Controller icons",44,new[]{"Auto","Xbox","PlayStation","Keyboard"});Label(r,"Order label","Turn order display",96);Dropdown(r,"Turn order",128,new[]{"Top","Right"});Choice(r,"Restore defaults",188);});
        var audio=Make<SystemPanelView>("Audio",r=>{
            PanelSize(r);Label(r,"Volume readout","Master Volume: 75%",12,32,19);
            var go=DefaultControls.CreateSlider(new DefaultControls.Resources());go.name="Master Volume";go.transform.SetParent(r,false);var rect=(RectTransform)go.transform;rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(0,1);rect.sizeDelta=new Vector2(-24,28);rect.anchoredPosition=new Vector2(12,-56);
            var c=go.AddComponent<SystemControlView>();c.Key="Master Volume";c.Slider=go.GetComponent<Slider>();c.Slider.maxValue=100;c.Slider.wholeNumbers=true;c.Slider.value=75;Choice(r,"Restore defaults",110);
        });
        var confirm=Make<SystemPanelView>("Confirmation",r=>{PanelSize(r);Label(r,"Save title","CONFIRMATION",0,32,22);Label(r,"Save confirmation|Reset confirmation|Display countdown","Confirm this change?",60,100,19);Choice(r,"Confirm load|Confirm overwrite|Confirm defaults|Keep display settings",176,"Confirm");Choice(r,"Cancel save/load|Cancel defaults|Revert display settings",226,"Cancel");});
        var slots=Make<SystemPanelView>("SaveSlots",r=>{
            PanelSize(r);Label(r,"Save title","SAVE / LOAD",0,32,22);
            var v=r.GetComponent<SystemPanelView>();v.Scroll=Scroll("Save slots",r,0,52,590,398);var vp=v.Scroll.viewport;vp.anchorMin=Vector2.zero;vp.anchorMax=Vector2.one;vp.offsetMin=new Vector2(0,58);vp.offsetMax=new Vector2(0,-52);
            var group=v.Scroll.content.gameObject.AddComponent<VerticalLayoutGroup>();group.spacing=8;group.padding=new RectOffset(8,8,8,8);group.childControlWidth=group.childControlHeight=true;group.childForceExpandWidth=true;group.childForceExpandHeight=false;
            var back=Choice(r,"Back",458);var rect=(RectTransform)back.transform;rect.anchorMin=new Vector2(0,0);rect.anchorMax=new Vector2(1,0);rect.anchoredPosition=new Vector2(12,44);
        });
        var slot=Make<SystemSaveSlotView>("SaveSlot",r=>{r.sizeDelta=new Vector2(560,86);var v=r.GetComponent<SystemSaveSlotView>();v.Select=Button(r);v.Name=Text("Name",r,"Save slot 1",16,12,6,536,28);Width(v.Name.rectTransform,12,12);v.Details=Text("Details",r,"Example captain · 11/19/2026",13,12,38,536,42);Width(v.Details.rectTransform,12,12);r.gameObject.AddComponent<LayoutElement>().preferredHeight=86;});
        string path=Folder+"/System.asset";if(!AssetDatabase.LoadAssetAtPath<SystemPageLayout>(path)){var l=ScriptableObject.CreateInstance<SystemPageLayout>();l.Workspace=workspace;l.Tab=tab;l.SaveLoad=home;l.Battle=battle;l.Graphics=graphics;l.UI=ui;l.Audio=audio;l.Confirmation=confirm;l.SaveSlots=slots;l.SaveSlot=slot;AssetDatabase.CreateAsset(l,path);}AssetDatabase.SaveAssets();AssetDatabase.Refresh();
    }
    static void PanelSize(RectTransform r){r.sizeDelta=new Vector2(590,508);}
    static void Label(RectTransform r,string key,string text,float y,float height=24,int size=17){var t=Text(key,r,text,size,12,y,566,height);Width(t.rectTransform,12,12);var c=t.gameObject.AddComponent<SystemControlView>();c.Key=key;c.Text=t;}
    static SystemControlView Choice(RectTransform r,string key,float y,string label=null){var rect=Rect(key,r,12,y,566,38);Width(rect,12,12);var c=rect.gameObject.AddComponent<SystemControlView>();c.Key=key;c.Button=Button(rect);c.Text=Text("Label",rect,label??key,16,12,6,542,26);Width(c.Text.rectTransform,12,12);return c;}
    static void Dropdown(RectTransform parent,string key,float y,string[] choices){
        var go=TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());go.name=key;go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=new Vector2(0,1);r.anchorMax=Vector2.one;r.pivot=new Vector2(0,1);r.sizeDelta=new Vector2(-24,36);r.anchoredPosition=new Vector2(12,-y);
        var c=go.AddComponent<SystemControlView>();c.Key=key;c.Dropdown=go.GetComponent<TMP_Dropdown>();c.Dropdown.ClearOptions();c.Dropdown.AddOptions(new System.Collections.Generic.List<string>(choices));
        foreach(var text in go.GetComponentsInChildren<TMP_Text>(true)){text.font=TMP_Settings.defaultFontAsset;text.fontSize=15;text.color=Color.white;}
        foreach(var image in go.GetComponentsInChildren<Image>(true))image.color=Panel;
        var arrow=go.transform.Find("Arrow");arrow.GetComponent<Image>().enabled=false;var a=Rect("Direction",arrow,0,0,20,20);a.anchorMin=Vector2.zero;a.anchorMax=Vector2.one;a.offsetMin=a.offsetMax=Vector2.zero;a.gameObject.AddComponent<DropdownArrow>().Bind(c.Dropdown);
        foreach(var toggle in go.GetComponentsInChildren<Toggle>(true))if(toggle.graphic)toggle.graphic.color=new Color(.3f,.8f,1);
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
[CustomEditor(typeof(SystemPageLayout))]
public sealed class SystemPageLayoutEditor:Editor {public override void OnInspectorGUI(){if(GUILayout.Button("Open live preview"))DetailsPanelPreviewWindow.Open((SystemPageLayout)target);DrawDefaultInspector();EditorGUILayout.HelpBox("Edit each tab prefab in Prefab Mode. Move and style its controls while retaining SystemControlView keys and references. Save rows and confirmation screens have separate templates.",MessageType.Info);}}
