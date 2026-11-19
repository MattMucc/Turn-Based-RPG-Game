using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class WorldUISetup {
 const string Folder="Assets/Resources/UI/World";
 [MenuItem("Tools/Game/UI/Create missing World Prompt and Notice templates")]
 public static void Create(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");Directory.CreateDirectory(Folder);
  var merchant=Prompt("Merchant");var blacksmith=Prompt("Blacksmith");var checkpoint=Prompt("Checkpoint");
  var save=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/SaveIndicator.prefab")?.GetComponent<SaveIndicatorView>();
  if(!save){var r=Rect("Save indicator",null,Vector2.zero,Vector2.zero);Stretch(r,Vector2.zero,Vector2.zero);try{var v=r.gameObject.AddComponent<SaveIndicatorView>();
   var icon=Rect("Save icon",r,new Vector2(-232,-22),new Vector2(20,20));icon.anchorMin=icon.anchorMax=icon.pivot=Vector2.one;v.Icon=Image(icon,Color.white);
   Image(Rect("Disk label",icon,new Vector2(0,3),new Vector2(10,7)),new Color(.05f,.1f,.15f));
   var text=Rect("Save status",r,new Vector2(-20,-20),new Vector2(202,80));text.anchorMin=text.anchorMax=text.pivot=Vector2.one;v.Label=Text(text,15);v.Label.outlineWidth=.15f;v.Label.outlineColor=Color.black;
   save=PrefabUtility.SaveAsPrefabAsset(r.gameObject,Folder+"/SaveIndicator.prefab").GetComponent<SaveIndicatorView>();
  }finally{UnityEngine.Object.DestroyImmediate(r.gameObject);}}
  var notice=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/EncounterNotice.prefab")?.GetComponent<EncounterNoticeView>();
  if(!notice){var r=Rect("Encounter notice",null,new Vector2(20,20),new Vector2(720,60));r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;try{Image(r,new Color(.025f,.05f,.08f,.9f));var v=r.gameObject.AddComponent<EncounterNoticeView>();var text=Rect("Message",r,Vector2.zero,Vector2.zero);Stretch(text,new Vector2(12,8),new Vector2(-12,-8));v.Message=Text(text,15);v.Message.enableAutoSizing=true;v.Message.fontSizeMin=10;v.Message.fontSizeMax=15;notice=PrefabUtility.SaveAsPrefabAsset(r.gameObject,Folder+"/EncounterNotice.prefab").GetComponent<EncounterNoticeView>();}finally{UnityEngine.Object.DestroyImmediate(r.gameObject);}}
  if(!AssetDatabase.LoadAssetAtPath<WorldUILayout>(Folder+"/World.asset")){var l=ScriptableObject.CreateInstance<WorldUILayout>();l.Merchant=merchant;l.Blacksmith=blacksmith;l.Checkpoint=checkpoint;l.SaveIndicator=save;l.EncounterNotice=notice;AssetDatabase.CreateAsset(l,Folder+"/World.asset");}AssetDatabase.SaveAssets();AssetDatabase.Refresh();
 }
 static WorldPromptView Prompt(string name){var existing=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/"+name+".prefab")?.GetComponent<WorldPromptView>();if(existing)return existing;
  var r=Rect(name+" prompt",null,Vector2.zero,new Vector2(220,38));try{Image(r,new Color(.025f,.05f,.08f,.85f));var v=r.gameObject.AddComponent<WorldPromptView>();v.Icon=Image(Rect("Interact icon",r,new Vector2(-88,0),new Vector2(28,28)),Color.white);v.Icon.preserveAspect=true;v.Caption=Text(Rect("Caption",r,new Vector2(20,0),new Vector2(170,32)),15);v.Caption.alignment=TextAlignmentOptions.MidlineLeft;return PrefabUtility.SaveAsPrefabAsset(r.gameObject,Folder+"/"+name+".prefab").GetComponent<WorldPromptView>();}finally{UnityEngine.Object.DestroyImmediate(r.gameObject);}}
 static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();if(parent)r.SetParent(parent,false);r.anchoredPosition=pos;r.sizeDelta=size;return r;}
 static void Stretch(RectTransform r,Vector2 min,Vector2 max){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=min;r.offsetMax=max;}
 static Image Image(RectTransform r,Color color){var image=r.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
 static TMP_Text Text(RectTransform r,int size){var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.fontSize=size;t.color=Color.white;t.raycastTarget=false;return t;}
}
[CustomEditor(typeof(WorldUILayout))]
public sealed class WorldUILayoutEditor:Editor {public override void OnInspectorGUI(){if(GUILayout.Button("Open live preview"))DetailsPanelPreviewWindow.Open((WorldUILayout)target);DrawDefaultInspector();}}
