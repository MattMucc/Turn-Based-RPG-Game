using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class PlayerMenuShellSetup {
 const string Folder="Assets/Resources/UI/Shell";
 static Color Ink=new Color(.88f,.94f,1),Panel=new Color(.035f,.075f,.12f);
 [MenuItem("Tools/Game/UI/Create missing Player Menu shell templates")]
 public static void Create(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");Directory.CreateDirectory(Folder);
  var command=Make<PlayerMenuCommandView>("Command",r=>{r.sizeDelta=new Vector2(170,38);var c=r.GetComponent<PlayerMenuCommandView>();c.Button=r.gameObject.AddComponent<InventoryButtonView>();BuildButton(c.Button,r,"Party");var l=r.gameObject.AddComponent<LayoutElement>();l.preferredHeight=38;});
  var character=Make<PlayerMenuCharacterView>("CharacterSelector",r=>{
   r.sizeDelta=new Vector2(420,40);var c=r.GetComponent<PlayerMenuCharacterView>();
   c.Previous=Choice(r,"<",0,0,40,38);c.Next=Choice(r,">",380,0,40,38);
   c.Name=Text("Selected member",r,"Example character",18,50,2,320,38);c.Name.enableAutoSizing=true;c.Name.fontSizeMin=12;c.Name.fontSizeMax=18;
  });
  var prefab=Make<PlayerMenuShellView>("PlayerMenuShell",r=>{
   r.sizeDelta=new Vector2(1280,720);r.gameObject.AddComponent<Image>().color=Panel;var v=r.GetComponent<PlayerMenuShellView>();
   v.Brand=Text("Brand",r,"PARTY MENU",23,24,20,180,40);v.Brand.color=new Color(.3f,.8f,1);
   v.Title=Text("Page title",r,"PARTY",26,220,20,460,42);
   v.Character=UnityEngine.Object.Instantiate(character,r,false);v.Character.name="Character selector";var selector=(RectTransform)v.Character.transform;selector.anchorMin=selector.anchorMax=Vector2.one;selector.anchoredPosition=new Vector2(-456,-22);
   v.CommandRail=Rect("Command rail",r,24,100,170,214);var group=v.CommandRail.gameObject.AddComponent<VerticalLayoutGroup>();group.spacing=6;group.childControlHeight=group.childControlWidth=true;group.childForceExpandHeight=false;group.childForceExpandWidth=true;
   foreach(var page in new[]{ProgressionMenu.Page.Party,ProgressionMenu.Page.Inventory,ProgressionMenu.Page.Abilities,ProgressionMenu.Page.SkillTree,ProgressionMenu.Page.System}){
    var entry=UnityEngine.Object.Instantiate(command,v.CommandRail,false);entry.Page=page;entry.name=page==ProgressionMenu.Page.SkillTree?"Growth":page.ToString();entry.Button.Label.text=entry.name;
   }
   v.Exit=Choice(r,"Return to world",24,0,170,38);var exit=(RectTransform)v.Exit.transform;exit.anchorMin=exit.anchorMax=new Vector2(0,0);exit.anchoredPosition=new Vector2(24,104);
   v.Body=Rect("Page content",r,0,0,0,0);v.Body.anchorMin=Vector2.zero;v.Body.anchorMax=Vector2.one;v.Body.offsetMin=new Vector2(220,70);v.Body.offsetMax=new Vector2(-470,-88);
   v.Details=Rect("Details",r,0,0,0,0);v.Details.anchorMin=new Vector2(1,0);v.Details.anchorMax=Vector2.one;v.Details.offsetMin=new Vector2(-450,70);v.Details.offsetMax=new Vector2(-24,-88);
   v.Footer=Text("Controls",r,"Arrow keys: navigate    Enter: select    Esc: back    [ / ]: character    Tab: close",13,24,0,1232,30);var f=v.Footer.rectTransform;f.anchorMin=new Vector2(0,0);f.anchorMax=new Vector2(1,0);f.anchoredPosition=new Vector2(24,44);f.sizeDelta=new Vector2(-48,30);
   v.ServiceBody=Rect("Service content placement",r,0,0,0,0);v.ServiceBody.anchorMin=Vector2.zero;v.ServiceBody.anchorMax=Vector2.one;v.ServiceBody.offsetMin=new Vector2(24,70);v.ServiceBody.offsetMax=new Vector2(-470,-88);v.ServiceBody.gameObject.SetActive(false);
   v.ServiceTitle=Rect("Service title placement",r,24,20,460,42);v.ServiceTitle.gameObject.SetActive(false);
  });
  string path=Folder+"/PlayerMenu.asset";if(!AssetDatabase.LoadAssetAtPath<PlayerMenuShellLayout>(path)){var l=ScriptableObject.CreateInstance<PlayerMenuShellLayout>();l.Prefab=prefab;AssetDatabase.CreateAsset(l,path);}AssetDatabase.SaveAssets();AssetDatabase.Refresh();
 }
 static InventoryButtonView Choice(RectTransform parent,string name,float x,float y,float w,float h){var r=Rect(name,parent,x,y,w,h);var v=r.gameObject.AddComponent<InventoryButtonView>();BuildButton(v,r,name);return v;}
 static void BuildButton(InventoryButtonView v,RectTransform r,string name){v.Select=Button(r);v.Label=Text("Label",r,name,16,12,6,r.sizeDelta.x-24,28);Width(v.Label.rectTransform,12,12);}
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
[CustomEditor(typeof(PlayerMenuShellLayout))]
public sealed class PlayerMenuShellLayoutEditor:Editor {public override void OnInspectorGUI(){if(GUILayout.Button("Open live preview"))DetailsPanelPreviewWindow.Open((PlayerMenuShellLayout)target);DrawDefaultInspector();EditorGUILayout.HelpBox("Edit PlayerMenuShell in Prefab Mode. Reorder command children, move the character selector, and arrange title/content regions. Keep bound references assigned. Service placement markers control Shop/Blacksmith layouts.",MessageType.Info);}}
