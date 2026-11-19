using System;
using System.IO;
using System.Collections.Generic;
using Game.GridPrototype;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class GrowthPageSetup {
    const string Folder="Assets/Resources/UI/Growth";
    static Color Ink=new Color(.88f,.94f,1),Panel=new Color(.035f,.075f,.12f);
    [MenuItem("Tools/Game/UI/Create missing Growth templates")]
    public static void Create(){
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
        Directory.CreateDirectory(Folder);
        var node=Make<GrowthNodeView>("SkillNode",r=>{
            r.sizeDelta=new Vector2(116,96);var v=r.GetComponent<GrowthNodeView>();v.Select=Button(r);v.Select.image.color=Color.clear;
            v.Frame=Rect("Node frame",r,30,0,56,56).gameObject.AddComponent<SkillTreeNodeGraphic>();v.Frame.color=new Color(.07f,.12f,.18f);v.Select.targetGraphic=v.Frame;
            v.Icon=Rect("Icon",v.Frame.transform,14,14,28,28).gameObject.AddComponent<RawImage>();v.Icon.raycastTarget=false;v.Icon.gameObject.SetActive(false);
            v.Type=Text("Type",v.Frame.transform,"S",18,0,0,56,56);v.Type.alignment=TextAlignmentOptions.Center;
            v.Name=Text("Name",r,"Skill node",13,2,60,112,34);v.Name.alignment=TextAlignmentOptions.Top;v.Name.enableAutoSizing=true;v.Name.fontSizeMin=10;v.Name.fontSizeMax=13;
        });
        var connection=Make<Image>("Connection",r=>{r.sizeDelta=new Vector2(100,2);r.GetComponent<Image>().color=new Color(.25f,.31f,.37f);r.GetComponent<Image>().raycastTarget=false;});
        var workspace=Make<GrowthWorkspaceView>("Workspace",r=>{
            r.sizeDelta=new Vector2(590,562);var v=r.GetComponent<GrowthWorkspaceView>();
            v.Title=Text("Tree title",r,"CHARACTER · SKILL TREE",20,0,0,590,34);Width(v.Title.rectTransform);
            v.Legend=Text("Tree legend",r,"Circle: Skill    Diamond: Passive\nGold: learned    Gray: locked / placeholder    Blue: selected",13,0,36,590,46);Width(v.Legend.rectTransform);
            v.Tree=Scroll("Skill tree",r,0,92,590,470);v.Tree.horizontal=true;var vp=v.Tree.viewport;vp.anchorMin=Vector2.zero;vp.anchorMax=Vector2.one;vp.offsetMin=Vector2.zero;vp.offsetMax=new Vector2(0,-92);
        });
        var arrangement=Make<GrowthTreeArrangement>("TreeArrangement",r=>{
            r.sizeDelta=new Vector2(500,450);r.pivot=Vector2.zero;
            var method=typeof(ProgressionMenu).GetMethod("PrototypeGrowthNodes",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            foreach(var data in (List<MenuSkillNode>)method.Invoke(null,null)){
                var marker=Rect(data.Id,r,0,0,116,96);marker.anchorMin=marker.anchorMax=Vector2.zero;marker.pivot=new Vector2(.5f,.5f);marker.anchoredPosition=data.Position;
                marker.gameObject.AddComponent<GrowthNodePlacement>().Example=data;
                var art=UnityEngine.Object.Instantiate(node,marker,false);var rect=(RectTransform)art.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;
                art.Name.text=data.Name;art.Frame.Passive=data.Passive;art.Type.text=data.Passive?"P":"S";
            }
        });
        string path=Folder+"/Growth.asset";
        if(!AssetDatabase.LoadAssetAtPath<GrowthPageLayout>(path)){var a=ScriptableObject.CreateInstance<GrowthPageLayout>();a.Workspace=workspace;a.Node=node;a.Connection=connection;a.Arrangement=arrangement;AssetDatabase.CreateAsset(a,path);}
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
[CustomEditor(typeof(GrowthPageLayout))]
public sealed class GrowthPageLayoutEditor:Editor {
    public override void OnInspectorGUI(){if(GUILayout.Button("Open live preview"))DetailsPanelPreviewWindow.Open((GrowthPageLayout)target);DrawDefaultInspector();EditorGUILayout.HelpBox("Move node markers in TreeArrangement Prefab Mode. Their Example prerequisite IDs connect them automatically. Real character records retain their requirements; matching IDs only override layout positions. Edit SkillNode and Connection prefabs for styling.",MessageType.Info);}
}
