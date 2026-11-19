using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class TargetUISetup {
 const string Folder="Assets/Resources/UI/Targets";
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 [MenuItem("Tools/Game/UI/Create missing Target and Examine templates")]
 public static void Create(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");Directory.CreateDirectory(Folder);
  var target=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Target.prefab")?.GetComponent<TargetDetailsView>();
  var inspection=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Inspection.prefab")?.GetComponent<TargetInspectionView>();
  if(!target||!inspection){
   var root=new GameObject("Temporary target templates",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(1280,720);
   try{var hud=root.AddComponent<BattleHud>();var type=typeof(BattleHud);type.GetField("root",Flags).SetValue(hud,(RectTransform)root.transform);type.GetField("font",Flags).SetValue(hud,TMP_Settings.defaultFontAsset);type.GetField("suppressTargetLayout",Flags).SetValue(hud,true);
    if(!target){type.GetMethod("BuildTargetDetails",Flags).Invoke(hud,null);var panel=Get<RectTransform>(hud,"targetPanel");panel.gameObject.SetActive(true);panel.anchorMin=panel.anchorMax=new Vector2(.5f,1);panel.pivot=new Vector2(.5f,1);panel.anchoredPosition=new Vector2(0,-80);
     var v=panel.gameObject.AddComponent<TargetDetailsView>();v.Name=Get<TMP_Text>(hud,"targetName");v.Health=Get<TMP_Text>(hud,"targetHealth");v.HealthFill=Get<RectTransform>(hud,"targetFill");v.Mana=Get<RectTransform>(hud,"targetMana");v.ManaFill=Get<RectTransform>(hud,"targetManaFill");v.ManaText=Get<TMP_Text>(hud,"targetManaText");v.Shield=Get<RectTransform>(hud,"targetShield");v.ShieldFill=Get<RectTransform>(hud,"targetShieldFill");v.ShieldText=Get<TMP_Text>(hud,"targetShieldText");v.Resources=Get<TMP_Text>(hud,"targetResources");v.DamagePreview=Get<TMP_Text>(hud,"damagePreview");v.Statuses=Get<BattleStatusRow>(hud,"targetStatuses");
     target=PrefabUtility.SaveAsPrefabAsset(panel.gameObject,Folder+"/Target.prefab").GetComponent<TargetDetailsView>();
    }
    if(!inspection){type.GetMethod("BuildInspection",Flags).Invoke(hud,null);var panel=Get<RectTransform>(hud,"inspectionLayer");panel.gameObject.SetActive(true);var v=panel.gameObject.AddComponent<TargetInspectionView>();
     v.Context=Get<RectTransform>(hud,"contextPanel");v.Use=Get<RectTransform>(hud,"contextUse");v.Panel=Get<RectTransform>(hud,"examinePanel");v.Content=Get<RectTransform>(hud,"examineContent");v.Details=Get<TMP_Text>(hud,"examineText");v.Statuses=Get<BattleStatusRow>(hud,"examineStatuses");
     v.Dismiss=panel.GetComponent<Button>();v.Examine=v.Context.Find("Examine").GetComponent<Button>();v.UseButton=v.Use.GetComponent<Button>();v.Close=v.Panel.Find("Close examine").GetComponent<Button>();
     inspection=PrefabUtility.SaveAsPrefabAsset(panel.gameObject,Folder+"/Inspection.prefab").GetComponent<TargetInspectionView>();
    }
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  var badge=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/StatusBadge.prefab")?.GetComponent<StatusBadgeView>();
  if(!badge){var r=Rect("Status badge",null,new Vector2(30,30));try{var v=r.gameObject.AddComponent<StatusBadgeView>();v.Graphic=r.gameObject.AddComponent<StatusBadgeGraphic>();v.Count=Text(Rect("Remaining",r,new Vector2(32,13)),11);v.Count.rectTransform.anchoredPosition=new Vector2(0,-9);v.Count.fontStyle=FontStyles.Bold;v.Count.outlineWidth=.25f;v.Count.outlineColor=Color.black;v.Count.enableAutoSizing=true;v.Count.fontSizeMin=7;v.Count.fontSizeMax=11;
   badge=PrefabUtility.SaveAsPrefabAsset(r.gameObject,Folder+"/StatusBadge.prefab").GetComponent<StatusBadgeView>();
  }finally{UnityEngine.Object.DestroyImmediate(r.gameObject);}}
  var tip=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/StatusTooltip.prefab")?.GetComponent<StatusTooltipView>();
  if(!tip){var r=Rect("Status tooltip",null,new Vector2(280,130));try{r.pivot=new Vector2(.5f,1);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.025f,.035f,.05f,.98f);image.raycastTarget=false;var v=r.gameObject.AddComponent<StatusTooltipView>();v.Details=Text(Rect("Details",r,Vector2.zero),13);var t=v.Details.rectTransform;t.anchorMin=Vector2.zero;t.anchorMax=Vector2.one;t.offsetMin=new Vector2(12,10);t.offsetMax=new Vector2(-12,-10);v.Details.alignment=TextAlignmentOptions.TopLeft;
   tip=PrefabUtility.SaveAsPrefabAsset(r.gameObject,Folder+"/StatusTooltip.prefab").GetComponent<StatusTooltipView>();
  }finally{UnityEngine.Object.DestroyImmediate(r.gameObject);}}
  if(!AssetDatabase.LoadAssetAtPath<TargetUILayout>(Folder+"/Targets.asset")){var l=ScriptableObject.CreateInstance<TargetUILayout>();l.Target=target;l.Inspection=inspection;l.StatusBadge=badge;l.StatusTooltip=tip;AssetDatabase.CreateAsset(l,Folder+"/Targets.asset");}AssetDatabase.SaveAssets();AssetDatabase.Refresh();
 }
 static T Get<T>(BattleHud hud,string name)=>(T)typeof(BattleHud).GetField(name,Flags).GetValue(hud);
 static RectTransform Rect(string name,Transform parent,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();if(parent)r.SetParent(parent,false);r.sizeDelta=size;return r;}
 static TMP_Text Text(RectTransform r,int size){var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.fontSize=size;t.color=Color.white;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;}
}
[CustomEditor(typeof(TargetUILayout))]
public sealed class TargetUILayoutEditor:Editor {public override void OnInspectorGUI(){if(GUILayout.Button("Open live preview"))DetailsPanelPreviewWindow.Open((TargetUILayout)target);DrawDefaultInspector();}}
