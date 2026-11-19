using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class TooltipHintsSetup {
 const string Folder="Assets/Resources/UI/Hints";
 [MenuItem("Tools/Game/UI/Create missing Tooltip and Controller Hint templates")]
 public static void Create(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");Directory.CreateDirectory(Folder);
  var battle=Tooltip("BattleAction",new Vector2(280,260),false);var inventory=Tooltip("InventoryHover",new Vector2(300,600),true);
  var cursor=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Cursor.prefab")?.GetComponent<RectTransform>();
  if(!cursor){var r=Rect("Controller cursor",null,new Vector2(22,22));try{var text=Text(r,30);text.text="+";text.alignment=TextAlignmentOptions.Center;text.outlineWidth=.25f;text.outlineColor=Color.black;cursor=PrefabUtility.SaveAsPrefabAsset(r.gameObject,Folder+"/Cursor.prefab").GetComponent<RectTransform>();}finally{UnityEngine.Object.DestroyImmediate(r.gameObject);}}
  var guide=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/CameraGuide.prefab")?.GetComponent<ControllerGuideView>();
  if(!guide){var r=Rect("Controller navigation mode",null,new Vector2(280,150));try{r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(12,-12);var v=r.gameObject.AddComponent<ControllerGuideView>();v.Text=Text(r,13);v.Text.outlineWidth=.2f;v.Text.outlineColor=Color.black;v.Present(0,"Commands");guide=PrefabUtility.SaveAsPrefabAsset(r.gameObject,Folder+"/CameraGuide.prefab").GetComponent<ControllerGuideView>();}finally{UnityEngine.Object.DestroyImmediate(r.gameObject);}}
  if(!AssetDatabase.LoadAssetAtPath<TooltipHintsLayout>(Folder+"/Hints.asset")){var l=ScriptableObject.CreateInstance<TooltipHintsLayout>();l.BattleAction=battle;l.InventoryHover=inventory;l.Cursor=cursor;l.Guide=guide;AssetDatabase.CreateAsset(l,Folder+"/Hints.asset");}AssetDatabase.SaveAssets();AssetDatabase.Refresh();
 }
 static ModularTooltipView Tooltip(string name,Vector2 max,bool inventory){
  var existing=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/"+name+".prefab")?.GetComponent<ModularTooltipView>();if(existing)return existing;
  var r=Rect(name,null,max);try{r.anchorMin=r.anchorMax=r.pivot=inventory?new Vector2(0,1):Vector2.zero;r.anchoredPosition=inventory?new Vector2(400,-150):new Vector2(202,12);
   var image=r.gameObject.AddComponent<Image>();image.color=new Color(.035f,.055f,.08f,.98f);image.raycastTarget=!inventory;r.gameObject.AddComponent<RectMask2D>();
   var v=r.gameObject.AddComponent<ModularTooltipView>();v.MaximumSize=max;v.MinimumHeight=inventory?80:40;v.Scroll=r.gameObject.AddComponent<ScrollRect>();v.Scroll.viewport=r;v.Scroll.horizontal=false;v.Scroll.scrollSensitivity=25;v.Scroll.movementType=ScrollRect.MovementType.Clamped;
   var content=Rect("Description",r,new Vector2(-24,100));content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.anchoredPosition=new Vector2(0,-10);v.Text=Text(content,inventory?14:13);v.Scroll.content=content;
   return PrefabUtility.SaveAsPrefabAsset(r.gameObject,Folder+"/"+name+".prefab").GetComponent<ModularTooltipView>();
  }finally{UnityEngine.Object.DestroyImmediate(r.gameObject);}
 }
 static RectTransform Rect(string name,Transform parent,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();if(parent)r.SetParent(parent,false);r.sizeDelta=size;return r;}
 static TMP_Text Text(RectTransform r,int size){var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.fontSize=size;t.color=Color.white;t.alignment=TextAlignmentOptions.TopLeft;t.raycastTarget=false;return t;}
}
[CustomEditor(typeof(TooltipHintsLayout))]
public sealed class TooltipHintsLayoutEditor:Editor {public override void OnInspectorGUI(){if(GUILayout.Button("Open live preview"))DetailsPanelPreviewWindow.Open((TooltipHintsLayout)target);DrawDefaultInspector();}}
