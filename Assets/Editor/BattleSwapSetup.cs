using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class BattleSwapSetup {
 const string Folder="Assets/Resources/UI/Swap";
 [MenuItem("Tools/Game/UI/Create missing Battle Swap templates")]
 public static void Create(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");Directory.CreateDirectory(Folder);
  var card=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Portrait.prefab")?.GetComponent<BattleSwapCardView>();
  if(!card){var r=Rect("Swap portrait",null,Vector2.zero,new Vector2(100,134));try{
   var v=r.gameObject.AddComponent<BattleSwapCardView>();v.Highlight=Background(r,new Color(.18f,.23f,.3f));v.Highlight.raycastTarget=true;v.Button=r.gameObject.AddComponent<Button>();v.Button.targetGraphic=v.Highlight;v.Button.transition=Selectable.Transition.None;v.Button.navigation=new Navigation{mode=Navigation.Mode.None};
   var image=Rect("Portrait image",r,Vector2.zero,Vector2.zero);Stretch(image,new Vector2(3,25),new Vector2(-3,-3));v.Portrait=image.gameObject.AddComponent<RawImage>();v.Portrait.raycastTarget=false;
   var name=Rect("Name",r,Vector2.zero,Vector2.zero);name.anchorMin=Vector2.zero;name.anchorMax=new Vector2(1,0);name.offsetMin=new Vector2(3,2);name.offsetMax=new Vector2(-3,23);v.Name=Text(name,13);v.Name.enableAutoSizing=true;v.Name.fontSizeMin=8;v.Name.fontSizeMax=13;
   var hp=Rect("Health",r,Vector2.zero,Vector2.zero);hp.anchorMin=Vector2.zero;hp.anchorMax=new Vector2(1,0);hp.offsetMin=new Vector2(3,25);hp.offsetMax=new Vector2(-3,40);Background(hp,new Color(.12f,.025f,.025f));
   v.HealthFill=Rect("Remaining",hp,Vector2.zero,Vector2.zero);Stretch(v.HealthFill,Vector2.zero,Vector2.zero);Background(v.HealthFill,new Color(.75f,.12f,.1f));
   var text=Rect("Current and maximum",hp,Vector2.zero,Vector2.zero);Stretch(text,Vector2.zero,Vector2.zero);v.Health=Text(text,11);v.Health.enableAutoSizing=true;v.Health.fontSizeMin=7;v.Health.fontSizeMax=11;
   card=PrefabUtility.SaveAsPrefabAsset(r.gameObject,Folder+"/Portrait.prefab").GetComponent<BattleSwapCardView>();
  }finally{UnityEngine.Object.DestroyImmediate(r.gameObject);}}
  var window=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Window.prefab")?.GetComponent<BattleSwapView>();
  if(!window){var r=Rect("Swap party member",null,Vector2.zero,Vector2.zero);Stretch(r,Vector2.zero,Vector2.zero);try{
   Background(r,new Color(0,0,0,.65f)).raycastTarget=true;var v=r.gameObject.AddComponent<BattleSwapView>();
   var row=Rect("Reserve choices",r,new Vector2(0,130),new Vector2(520,220));row.anchorMin=row.anchorMax=new Vector2(.5f,0);
   v.Prompt=Text(Rect("Prompt",row,new Vector2(0,94),new Vector2(510,40)),15);
   v.Current=Rect("Current character slot",row,new Vector2(-195,0),new Vector2(100,134));v.Reserves=new RectTransform[3];for(int i=0;i<3;i++)v.Reserves[i]=Rect("Reserve slot "+(i+1),row,new Vector2(-65+i*120,0),new Vector2(100,134));
   v.Confirm=Button(Rect("Confirm swap",row,new Vector2(-70,-92),new Vector2(130,28)),"Confirm swap");v.Cancel=Button(Rect("Cancel",row,new Vector2(80,-92),new Vector2(130,28)),"Cancel");
   window=PrefabUtility.SaveAsPrefabAsset(r.gameObject,Folder+"/Window.prefab").GetComponent<BattleSwapView>();
  }finally{UnityEngine.Object.DestroyImmediate(r.gameObject);}}
  if(!AssetDatabase.LoadAssetAtPath<BattleSwapLayout>(Folder+"/Swap.asset")){var l=ScriptableObject.CreateInstance<BattleSwapLayout>();l.Window=window;l.Portrait=card;AssetDatabase.CreateAsset(l,Folder+"/Swap.asset");}AssetDatabase.SaveAssets();AssetDatabase.Refresh();
 }
 static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();if(parent)r.SetParent(parent,false);r.anchoredPosition=position;r.sizeDelta=size;return r;}
 static void Stretch(RectTransform r,Vector2 min,Vector2 max){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=min;r.offsetMax=max;}
 static Image Background(RectTransform r,Color c){var image=r.gameObject.AddComponent<Image>();image.color=c;image.raycastTarget=false;return image;}
 static TMP_Text Text(RectTransform r,int size){var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.fontSize=size;t.color=Color.white;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;}
 static Button Button(RectTransform r,string name){var image=Background(r,new Color(.15f,.18f,.23f));image.raycastTarget=true;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;b.navigation=new Navigation{mode=Navigation.Mode.None};var label=Rect("Label",r,Vector2.zero,Vector2.zero);Stretch(label,Vector2.zero,Vector2.zero);Text(label,13).text=name;return b;}
}
[CustomEditor(typeof(BattleSwapLayout))]
public sealed class BattleSwapLayoutEditor:Editor {public override void OnInspectorGUI(){if(GUILayout.Button("Open live preview"))DetailsPanelPreviewWindow.Open((BattleSwapLayout)target);DrawDefaultInspector();}}
