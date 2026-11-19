using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class CheckpointUISetup {
 const string Folder="Assets/Resources/UI/Checkpoint";
 [MenuItem("Tools/Game/UI/Create missing Checkpoint templates")]
 public static void Create(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");Directory.CreateDirectory(Folder);
  var menu=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Menu.prefab")?.GetComponent<CheckpointMenuView>();
  if(!menu){var r=Rect("Checkpoint menu",null,0,0,590,562);try{var v=r.gameObject.AddComponent<CheckpointMenuView>();v.Title=Text(Rect("Checkpoint title",r,0,0,560,50),23);
   var options=Rect("Options",r,0,70,560,180);v.Options=options.gameObject;v.Rest=Choice(options,"Rest",0);v.Save=Choice(options,"Save",54);v.Leave=Choice(options,"Leave",108);
   var confirmation=Rect("Rest confirmation",r,0,70,560,300);v.Confirmation=confirmation.gameObject;v.Warning=Text(Rect("Rest warning",confirmation,0,0,560,110),18);v.Warning.text="Restore everyone's HP and MP?\nRoaming enemies will return in new groups.\nYour checkpoint autosave will be updated.";
   v.Confirm=Choice(confirmation,"Confirm rest",140);v.Cancel=Choice(confirmation,"Cancel rest",194);v.Present("Checkpoint",false,true);
   menu=PrefabUtility.SaveAsPrefabAsset(r.gameObject,Folder+"/Menu.prefab").GetComponent<CheckpointMenuView>();
  }finally{UnityEngine.Object.DestroyImmediate(r.gameObject);}}
  var details=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Details.prefab")?.GetComponent<CheckpointDetailsView>();
  if(!details){var r=Rect("Checkpoint details",null,0,0,426,562);try{var v=r.gameObject.AddComponent<CheckpointDetailsView>();v.Title=Text(Rect("Title",r,12,0,400,54),22);v.Description=Text(Rect("Description",r,12,70,400,280),16);v.Notice=Text(Rect("Notice",r,12,366,400,130),14);details=PrefabUtility.SaveAsPrefabAsset(r.gameObject,Folder+"/Details.prefab").GetComponent<CheckpointDetailsView>();}finally{UnityEngine.Object.DestroyImmediate(r.gameObject);}}
  if(!AssetDatabase.LoadAssetAtPath<CheckpointUILayout>(Folder+"/Checkpoint.asset")){var l=ScriptableObject.CreateInstance<CheckpointUILayout>();l.Menu=menu;l.Details=details;AssetDatabase.CreateAsset(l,Folder+"/Checkpoint.asset");}AssetDatabase.SaveAssets();AssetDatabase.Refresh();
 }
 static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();if(parent)r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
 static TMP_Text Text(RectTransform r,int size){var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.fontSize=size;t.color=new Color(.88f,.94f,1);t.raycastTarget=false;return t;}
 static InventoryButtonView Choice(Transform parent,string name,float y){var r=Rect(name,parent,0,y,560,40);var v=r.gameObject.AddComponent<InventoryButtonView>();var image=r.gameObject.AddComponent<Image>();image.color=new Color(.08f,.17f,.25f);v.Select=r.gameObject.AddComponent<Button>();v.Select.targetGraphic=image;v.Label=Text(Rect("Label",r,12,6,536,28),16);v.Label.text=name;return v;}
}
[CustomEditor(typeof(CheckpointUILayout))]
public sealed class CheckpointUILayoutEditor:Editor {public override void OnInspectorGUI(){if(GUILayout.Button("Open live preview"))DetailsPanelPreviewWindow.Open((CheckpointUILayout)target);DrawDefaultInspector();}}
