using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Game.GridPrototype;
public static class ReactionUISetup {
 const string Folder="Assets/Resources/UI/Reactions";
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 [MenuItem("Tools/Game/UI/Create missing Defense and QTE templates")]
 public static void Create(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");Directory.CreateDirectory(Folder);
  DefenseUIView defense=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Defense.prefab")?.GetComponent<DefenseUIView>();
  QteUIView qte=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Qte.prefab")?.GetComponent<QteUIView>();
  if(!defense||!qte){
   // Only construct detached UI in Edit Mode; no encounter is bound and no Update is run.
   var root=new GameObject("Temporary reaction templates",typeof(RectTransform));
   try{
    var hud=root.AddComponent<BattleHud>();var type=typeof(BattleHud);type.GetField("root",Flags).SetValue(hud,(RectTransform)root.transform);type.GetField("font",Flags).SetValue(hud,TMP_Settings.defaultFontAsset);type.GetField("suppressReactionLayout",Flags).SetValue(hud,true);
    if(!defense){
     type.GetMethod("BuildDefense",Flags).Invoke(hud,null);var panel=(RectTransform)type.GetField("defensePanel",Flags).GetValue(hud);panel.gameObject.SetActive(true);
     var v=panel.gameObject.AddComponent<DefenseUIView>();v.Caption=Get<TMP_Text>(hud,"defenseCaption");v.Prompt=Get<TMP_Text>(hud,"defensePrompt");v.Result=Get<TMP_Text>(hud,"defenseResult");v.Dodge=Get<RectTransform>(hud,"dodgeWindow");v.Parry=Get<RectTransform>(hud,"parryWindow");v.Impact=Get<RectTransform>(hud,"impactLine");v.Marker=Get<RectTransform>(hud,"defenseMarker");defense=PrefabUtility.SaveAsPrefabAsset(panel.gameObject,Folder+"/Defense.prefab").GetComponent<DefenseUIView>();
    }
    if(!qte){
     type.GetMethod("BuildSignature",Flags).Invoke(hud,null);var panel=Get<RectTransform>(hud,"qteLayer");panel.gameObject.SetActive(true);var v=panel.gameObject.AddComponent<QteUIView>();
     v.Caption=Get<TMP_Text>(hud,"qteCaption");v.Countdown=Get<TMP_Text>(hud,"qteCountdown");v.Key=Get<TMP_Text>(hud,"qteKey");v.Fill=Get<RectTransform>(hud,"qteFill");v.Prompt=Get<RectTransform>(hud,"qtePrompt");v.Secondary=Get<RectTransform>(hud,"dualPrompt");v.Icon=Get<Image>(hud,"qteIcon");v.SecondaryIcon=Get<Image>(hud,"dualIcon");v.Ring=Get<QteProgressRing>(hud,"tapRing");v.Triangle=Get<QteTriangleCue>(hud,"triangleCue");
     v.LeftLane=Lane(panel,QteLane.Left);v.RightLane=Lane(panel,QteLane.Right);v.BottomLane=Lane(panel,QteLane.Bottom);
     // Generated ring segments are instance data, never saved into the reusable prefab.
     for(int i=v.Ring.transform.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(v.Ring.transform.GetChild(i).gameObject);
     qte=PrefabUtility.SaveAsPrefabAsset(panel.gameObject,Folder+"/Qte.prefab").GetComponent<QteUIView>();
    }
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  var step=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/SequenceStep.prefab")?.GetComponent<QteSequenceView>();
  if(!step){var r=Rect("SequenceStep",null);r.sizeDelta=new Vector2(46,46);try{var v=r.gameObject.AddComponent<QteSequenceView>();v.Frame=r.gameObject.AddComponent<Image>();v.Frame.color=Color.clear;v.Frame.raycastTarget=false;
   var bar=Rect("Step progress",r);bar.anchorMin=Vector2.zero;bar.anchorMax=new Vector2(1,0);bar.sizeDelta=new Vector2(0,2);bar.anchoredPosition=new Vector2(0,-2);v.Progress=bar.gameObject.AddComponent<Image>();v.Progress.raycastTarget=false;
   var icon=Rect("Button icon",r);icon.anchorMin=Vector2.zero;icon.anchorMax=Vector2.one;icon.offsetMin=new Vector2(4,4);icon.offsetMax=new Vector2(-4,-4);v.Icon=icon.gameObject.AddComponent<Image>();v.Icon.preserveAspect=true;v.Icon.raycastTarget=false;step=PrefabUtility.SaveAsPrefabAsset(r.gameObject,Folder+"/SequenceStep.prefab").GetComponent<QteSequenceView>();
  }finally{UnityEngine.Object.DestroyImmediate(r.gameObject);}}
  string path=Folder+"/Reactions.asset";if(!AssetDatabase.LoadAssetAtPath<ReactionUILayout>(path)){var l=ScriptableObject.CreateInstance<ReactionUILayout>();l.Defense=defense;l.Qte=qte;l.SequenceStep=step;AssetDatabase.CreateAsset(l,path);}AssetDatabase.SaveAssets();AssetDatabase.Refresh();
 }
 static T Get<T>(BattleHud hud,string name)=>(T)typeof(BattleHud).GetField(name,Flags).GetValue(hud);
 static RectTransform Rect(string name,Transform parent){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();if(parent)r.SetParent(parent,false);return r;}
 static RectTransform Lane(Transform parent,QteLane lane){var r=Rect(lane+" lane",parent);r.anchorMin=r.anchorMax=QteLayout.Anchor(lane);r.anchoredPosition=QteLayout.Offset(lane);r.sizeDelta=new Vector2(104,48);r.gameObject.SetActive(false);return r;}
}
[CustomEditor(typeof(ReactionUILayout))]
public sealed class ReactionUILayoutEditor:Editor {public override void OnInspectorGUI(){if(GUILayout.Button("Open live preview"))DetailsPanelPreviewWindow.Open((ReactionUILayout)target);DrawDefaultInspector();EditorGUILayout.HelpBox("Edit Defense, Qte and SequenceStep prefabs. Lane markers place prompts consistently. Timing values remain on DefensePrototypeSettings; these templates change presentation only.",MessageType.Info);}}
