using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class BattleUISetup {
 const string Folder="Assets/Resources/UI/Battle";
 static Color Ink=Color.white,Panel=new Color(.035f,.045f,.065f,.96f);
 [MenuItem("Tools/Game/UI/Create missing Battle UI templates")]
 public static void Create(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");Directory.CreateDirectory(Folder);
  var button=Make<BattleMenuButton>("CommandButton",r=>{
   r.sizeDelta=new Vector2(172,28);Button(r);r.GetComponent<Image>().color=Color.white;var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/MenuButton.prefab");if(original&&original.GetComponent<Image>())r.GetComponent<Image>().sprite=original.GetComponent<Image>().sprite;r.gameObject.AddComponent<Outline>();
   var group=r.gameObject.AddComponent<HorizontalLayoutGroup>();group.padding=new RectOffset(10,10,2,2);group.spacing=3;group.childControlWidth=group.childControlHeight=true;group.childForceExpandWidth=false;group.childForceExpandHeight=false;
   var label=Text("Label",r,"Attacks",14,0,0,120,24);label.color=new Color(.05f,.06f,.08f);label.alignment=TextAlignmentOptions.MidlineLeft;var le=label.gameObject.AddComponent<LayoutElement>();le.flexibleWidth=1;le.minWidth=0;
   var icon=Rect("Icon",r,0,0,16.5f,16.5f);icon.gameObject.AddComponent<Image>().raycastTarget=false;var il=icon.gameObject.AddComponent<LayoutElement>();il.preferredWidth=il.preferredHeight=16.5f;
   r.gameObject.AddComponent<LayoutElement>().preferredHeight=28;
   var so=new SerializedObject(r.GetComponent<BattleMenuButton>());so.FindProperty("preserveTemplateStyle").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
  });
  var commands=Make<BattleCommandView>("Commands",r=>{
   r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;r.anchoredPosition=new Vector2(12,12);r.sizeDelta=new Vector2(180,260);var v=r.GetComponent<BattleCommandView>();
   v.Scroll=Scroll("Battle command viewport",r,0,0,180,260);var vp=v.Scroll.viewport;vp.anchorMin=vp.anchorMax=vp.pivot=Vector2.zero;vp.anchoredPosition=Vector2.zero;
   var group=v.Scroll.content.gameObject.AddComponent<VerticalLayoutGroup>();group.padding=new RectOffset(4,4,4,4);group.spacing=2;group.childControlWidth=group.childControlHeight=true;group.childForceExpandWidth=true;group.childForceExpandHeight=false;
   v.Scroll.content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
   var counter=Rect("Target selection count",r,0,0,180,30);counter.anchorMin=counter.anchorMax=counter.pivot=Vector2.zero;counter.anchoredPosition=new Vector2(0,264);counter.gameObject.AddComponent<Image>().color=Panel;v.Counter=Text("Text",counter,"0 / 3 targets selected",13,4,4,172,22);v.Counter.alignment=TextAlignmentOptions.Center;
  });
  var end=Make<RectTransform>("RemainingTurn",r=>{r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;r.anchoredPosition=new Vector2(12,12);r.sizeDelta=new Vector2(180,28);});
  var top=Make<ScrollRect>("TopTurnOrder",r=>{BuildOrder(r);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,1);r.anchoredPosition=new Vector2(0,-6);r.sizeDelta=new Vector2(600,70);});
  var right=Make<ScrollRect>("RightTurnOrder",r=>{BuildOrder(r);r.anchorMin=r.anchorMax=r.pivot=Vector2.one;r.anchoredPosition=new Vector2(-12,-6);r.sizeDelta=new Vector2(60,290);});
  var portrait=Make<BattleTurnCardView>("TurnPortrait",r=>{
   r.sizeDelta=new Vector2(52,62);var v=r.GetComponent<BattleTurnCardView>();v.Border=r.gameObject.AddComponent<Image>();v.Border.color=Panel;
   var picture=Rect("Portrait",r,3,3,46,56);Fill(picture,3,3,3,3);v.Portrait=picture.gameObject.AddComponent<RawImage>();v.Portrait.raycastTarget=false;
   v.HealthFill=Bar(r,"HP overlay",3,3,7.5f,new Color(.75f,.12f,.1f),out v.Health);v.ShieldFill=Bar(r,"Barrier shield",3,12.5f,7.5f,new Color(.12f,.5f,1),out v.ShieldText);v.Shield=(RectTransform)v.ShieldFill.parent;
   v.Hits=Text("Hits",r,"",14,2,2,48,58);Fill(v.Hits.rectTransform,2,2,2,2);v.Hits.alignment=TextAlignmentOptions.TopRight;v.Hits.color=new Color(1,.8f,.25f);
  });
  var stack=Make<RectTransform>("PartyStack",r=>{r.anchorMin=r.anchorMax=r.pivot=new Vector2(1,0);r.anchoredPosition=new Vector2(-12,12);r.sizeDelta=new Vector2(220,250);});
  var resource=Make<BattleResourceView>("PartyResource",r=>{
   r.sizeDelta=new Vector2(190,66);var v=r.GetComponent<BattleResourceView>();v.Background=r.gameObject.AddComponent<Image>();v.Background.color=Panel;
   v.Name=Text("Name and level",r,"Example · Lv 10",10,6,6,178,12);Width(v.Name.rectTransform,6,6);v.Name.overflowMode=TextOverflowModes.Ellipsis;v.Name.textWrappingMode=TextWrappingModes.NoWrap;
   v.Health=TopBar(r,"Health",22,new Color(.75f,.12f,.1f),out v.HealthText);v.Mana=TopBar(r,"Mana",34,new Color(.2f,.4f,.9f),out v.ManaText);
   v.Resources=Text("AP and movement",r,"AP 4 / 9    Move 3 / 10",10,6,46,178,14);Width(v.Resources.rectTransform,6,6);v.Resources.alignment=TextAlignmentOptions.Center;
   v.Statuses=Rect("Statuses",r,6,62,178,16);Width(v.Statuses,6,6);
  });
  var dimmer=Make<Image>("ShortcutDimmer",r=>{Fill(r,0,0,0,0);r.GetComponent<Image>().color=new Color(0,0,0,.45f);r.GetComponent<Image>().raycastTarget=false;});
  string path=Folder+"/Battle.asset";if(!AssetDatabase.LoadAssetAtPath<BattleUILayout>(path)){var l=ScriptableObject.CreateInstance<BattleUILayout>();l.Commands=commands;l.CommandButton=button;l.RemainingTurn=end;l.TopOrder=top;l.RightOrder=right;l.Portrait=portrait;l.PartyStack=stack;l.ResourceCard=resource;AssetDatabase.CreateAsset(l,path);}var layout=AssetDatabase.LoadAssetAtPath<BattleUILayout>(path);if(!layout.ShortcutDimmer){layout.ShortcutDimmer=dimmer;EditorUtility.SetDirty(layout);}AssetDatabase.SaveAssets();AssetDatabase.Refresh();
 }
 static RectTransform TopBar(RectTransform parent,string name,float y,Color tint,out TMP_Text label){var fill=Bar(parent,name,6,0,10,tint,out label);var r=(RectTransform)fill.parent;r.anchorMin=new Vector2(0,1);r.anchorMax=Vector2.one;r.offsetMin=new Vector2(6,-y-10);r.offsetMax=new Vector2(-6,-y);return fill;}
 static RectTransform Bar(RectTransform parent,string name,float inset,float bottom,float height,Color tint,out TMP_Text label){
  var track=Rect(name,parent,0,0,100,height);track.anchorMin=Vector2.zero;track.anchorMax=new Vector2(1,0);track.offsetMin=new Vector2(inset,bottom);track.offsetMax=new Vector2(-inset,bottom+height);track.gameObject.AddComponent<Image>().color=new Color(0,0,0,.8f);
  var fill=Rect("Fill",track,0,0,100,height);Fill(fill,0,0,0,0);fill.gameObject.AddComponent<Image>().color=tint;
  label=Text("Value",track,"80 / 100",9,0,0,100,height);Fill(label.rectTransform,1,0,1,0);label.alignment=TextAlignmentOptions.Center;label.enableAutoSizing=true;label.fontSizeMin=2;label.fontSizeMax=9;label.textWrappingMode=TextWrappingModes.NoWrap;return fill;
 }
 static void Fill(RectTransform r,float left,float bottom,float right,float top){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(left,bottom);r.offsetMax=new Vector2(-right,-top);}
 static void BuildOrder(RectTransform r){r.gameObject.AddComponent<Image>().color=Panel;r.gameObject.AddComponent<RectMask2D>();var s=r.GetComponent<ScrollRect>();s.viewport=r;s.content=Rect("Upcoming turns",r,0,0,600,70);s.movementType=ScrollRect.MovementType.Clamped;s.scrollSensitivity=30;}
    static T Make<T>(string name,Action<RectTransform> build)where T:Component{
        string path=Folder+"/"+name+".prefab";var old=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(old)return old.GetComponent<T>();
        var root=Rect(name,null,0,0,100,100);if(!root.GetComponent<T>())root.gameObject.AddComponent<T>();
        try{build(root);return PrefabUtility.SaveAsPrefabAsset(root.gameObject,path).GetComponent<T>();}finally{UnityEngine.Object.DestroyImmediate(root.gameObject);}
    }
    static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();if(parent)r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
    static void Width(RectTransform r,float left=0,float right=0){r.anchorMax=new Vector2(1,1);r.offsetMin=new Vector2(left,r.offsetMin.y);r.offsetMax=new Vector2(-right,r.offsetMax.y);}
    static TMP_Text Text(string name,Transform parent,string value,int size,float x,float y,float w,float h){var t=Rect(name,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.text=value;t.fontSize=size;t.color=Ink;t.raycastTarget=false;return t;}
    static Button Button(RectTransform r){var image=r.gameObject.AddComponent<Image>();image.color=new Color(.08f,.17f,.25f);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;return b;}
    static ScrollRect Scroll(string name,Transform parent,float x,float y,float w,float h){var vp=Rect(name,parent,x,y,w,h);vp.gameObject.AddComponent<Image>().color=new Color(.025f,.05f,.08f);vp.gameObject.AddComponent<RectMask2D>();var s=vp.gameObject.AddComponent<ScrollRect>();s.viewport=vp;s.content=Rect("Content",vp,0,0,w,h);Width(s.content);s.horizontal=false;s.scrollSensitivity=45;s.movementType=ScrollRect.MovementType.Clamped;return s;}
}
[CustomEditor(typeof(BattleUILayout))]
public sealed class BattleUILayoutEditor:Editor {public override void OnInspectorGUI(){if(GUILayout.Button("Open live preview"))DetailsPanelPreviewWindow.Open((BattleUILayout)target);DrawDefaultInspector();EditorGUILayout.HelpBox("Edit Commands, CommandButton, TurnPortrait and PartyResource in Prefab Mode. Top/Right order and PartyStack control placement. Keep bindings assigned. This preview does not enter battle.",MessageType.Info);}}
