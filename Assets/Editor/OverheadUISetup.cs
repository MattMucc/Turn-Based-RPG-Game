using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class OverheadUISetup {
 const string Folder="Assets/Resources/UI/Overheads";
 [MenuItem("Tools/Game/UI/Create missing Overhead Character templates")]
 public static void Create(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");Directory.CreateDirectory(Folder);
  var view=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Character.prefab")?.GetComponent<OverheadCharacterView>();
  if(!view){var r=Rect("Combatant overhead",null,Vector2.zero,new Vector2(200,80));
   try{var v=r.gameObject.AddComponent<OverheadCharacterView>();
    v.Name=Text(Rect("Name",r,new Vector2(-24,24),new Vector2(150,24)),18);v.Name.enableAutoSizing=true;v.Name.fontSizeMin=11;v.Name.fontSizeMax=18;
    v.Level=Text(Rect("Level",r,new Vector2(78,24),new Vector2(44,24)),14);
    var hp=Rect("Health",r,new Vector2(0,-4),new Vector2(184,14));Background(hp,new Color(.12f,.025f,.025f,.95f));
    v.HealthFill=Fill("Health fill",hp,new Color(.8f,.14f,.12f));v.Health=Text(Rect("Current and maximum",hp,Vector2.zero,new Vector2(184,14)),12);v.Health.enableAutoSizing=true;v.Health.fontSizeMin=8;v.Health.fontSizeMax=12;
    v.Barrier=Rect("Barrier",r,new Vector2(0,8),new Vector2(184,6));Background(v.Barrier,new Color(.05f,.13f,.24f));v.BarrierFill=Fill("Barrier fill",v.Barrier,new Color(.25f,.65f,1));
    var status=Rect("Status effects",r,new Vector2(0,-27),new Vector2(184,22));v.Statuses=status.gameObject.AddComponent<BattleStatusRow>();v.Statuses.IconSize=18;v.Statuses.Spacing=4;v.Statuses.Layout=Resources.Load<TargetUILayout>("UI/Targets/Targets");
    view=PrefabUtility.SaveAsPrefabAsset(r.gameObject,Folder+"/Character.prefab").GetComponent<OverheadCharacterView>();
   }finally{UnityEngine.Object.DestroyImmediate(r.gameObject);}
  }
  if(!AssetDatabase.LoadAssetAtPath<OverheadUILayout>(Folder+"/Overheads.asset")){var layout=ScriptableObject.CreateInstance<OverheadUILayout>();layout.Character=view;AssetDatabase.CreateAsset(layout,Folder+"/Overheads.asset");}AssetDatabase.SaveAssets();AssetDatabase.Refresh();
 }
 static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();if(parent)r.SetParent(parent,false);r.anchoredPosition=pos;r.sizeDelta=size;return r;}
 static void Background(RectTransform r,Color color){var image=r.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;}
 static RectTransform Fill(string name,RectTransform parent,Color color){var r=Rect(name,parent,Vector2.zero,Vector2.zero);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;Background(r,color);return r;}
 static TMP_Text Text(RectTransform r,int size){var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.fontSize=size;t.color=Color.white;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;t.outlineWidth=.22f;t.outlineColor=Color.black;t.textWrappingMode=TextWrappingModes.NoWrap;t.overflowMode=TextOverflowModes.Ellipsis;return t;}
}
[CustomEditor(typeof(OverheadUILayout))]
public sealed class OverheadUILayoutEditor:Editor {
 public override void OnInspectorGUI(){if(GUILayout.Button("Open live preview"))DetailsPanelPreviewWindow.Open((OverheadUILayout)target);DrawDefaultInspector();EditorGUILayout.HelpBox("Arrange the Character prefab inside its root rectangle. Runtime fits that rectangle inside the existing overhead camera-framing area. Status icons are display-only; target details and party cards retain hover tooltips.",MessageType.Info);}
}
