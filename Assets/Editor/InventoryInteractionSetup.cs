using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class InventoryInteractionSetup {
 const string Folder="Assets/Resources/UI/InventoryInteractions";
 [MenuItem("Tools/Game/UI/Create missing Inventory Interaction templates")]
 public static void Create(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");Directory.CreateDirectory(Folder);
  var recipient=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Recipient.prefab")?.GetComponent<InventoryButtonView>();
  if(!recipient){var v=Choice("Recipient",null,Vector2.zero,new Vector2(404,46));try{v.gameObject.AddComponent<LayoutElement>().preferredHeight=46;recipient=PrefabUtility.SaveAsPrefabAsset(v.gameObject,Folder+"/Recipient.prefab").GetComponent<InventoryButtonView>();}finally{UnityEngine.Object.DestroyImmediate(v.gameObject);}}
  var overlay=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Overlay.prefab")?.GetComponent<RectTransform>();
  if(!overlay){var r=Rect("Inventory interaction",null,Vector2.zero,Vector2.zero);try{Stretch(r,Vector2.zero,Vector2.zero);var image=Background(r,new Color(0,0,0,.6f));var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;b.transition=Selectable.Transition.None;b.navigation=new Navigation{mode=Navigation.Mode.None};overlay=PrefabUtility.SaveAsPrefabAsset(r.gameObject,Folder+"/Overlay.prefab").GetComponent<RectTransform>();}finally{UnityEngine.Object.DestroyImmediate(r.gameObject);}}
  var examination=Panel("Examination",330);var recipients=Panel("Recipients",460);var recovery=Panel("Recovery",330);var learning=Panel("Learning",330);var slots=Panel("AccessorySlots",330);var equipment=Panel("Equipment",500);
  if(!AssetDatabase.LoadAssetAtPath<InventoryInteractionLayout>(Folder+"/Interactions.asset")){var l=ScriptableObject.CreateInstance<InventoryInteractionLayout>();l.Overlay=overlay;l.Recipient=recipient;l.Examination=examination;l.Recipients=recipients;l.Recovery=recovery;l.Learning=learning;l.AccessorySlots=slots;l.Equipment=equipment;AssetDatabase.CreateAsset(l,Folder+"/Interactions.asset");}AssetDatabase.SaveAssets();AssetDatabase.Refresh();
 }
 static InventoryInteractionView Panel(string name,int height){
  var existing=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/"+name+".prefab")?.GetComponent<InventoryInteractionView>();if(existing)return existing;
  var r=Rect("Item context",null,Vector2.zero,new Vector2(440,height));try{
   var bg=Background(r,new Color(.035f,.075f,.12f,.98f));var blocker=r.gameObject.AddComponent<Button>();blocker.targetGraphic=bg;blocker.transition=Selectable.Transition.None;blocker.navigation=new Navigation{mode=Navigation.Mode.None};
   var v=r.gameObject.AddComponent<InventoryInteractionView>();
   var title=Rect("Item name",r,new Vector2(0,-34),new Vector2(404,38));title.anchorMin=title.anchorMax=new Vector2(.5f,1);v.Title=Text(title,22);
   var lead=Rect("Lead",r,new Vector2(0,-68),new Vector2(404,28));lead.anchorMin=lead.anchorMax=new Vector2(.5f,1);v.Lead=Text(lead,16);
   var viewport=Rect("Content viewport",r,Vector2.zero,Vector2.zero);Stretch(viewport,new Vector2(18,84),new Vector2(-18,-92));Background(viewport,Color.clear);viewport.gameObject.AddComponent<RectMask2D>();v.Scroll=viewport.gameObject.AddComponent<ScrollRect>();v.Scroll.viewport=viewport;v.Scroll.horizontal=false;v.Scroll.movementType=ScrollRect.MovementType.Clamped;v.Scroll.scrollSensitivity=45;
   var description=Content("Description",viewport);v.Description=Text(description,16);v.Description.alignment=TextAlignmentOptions.TopLeft;
   v.Choices=Content("Choices",viewport);var group=v.Choices.gameObject.AddComponent<VerticalLayoutGroup>();group.spacing=6;group.childControlWidth=true;group.childControlHeight=true;group.childForceExpandWidth=true;group.childForceExpandHeight=false;v.Scroll.content=description;
   var notice=Rect("Notice",r,new Vector2(0,67),new Vector2(404,26));notice.anchorMin=notice.anchorMax=new Vector2(.5f,0);v.Notice=Text(notice,12);
   v.Confirm=Choice("Confirm",r,new Vector2(-105,30),new Vector2(194,36));v.Back=Choice("Back",r,new Vector2(105,30),new Vector2(194,36));((RectTransform)v.Confirm.transform).anchorMin=((RectTransform)v.Confirm.transform).anchorMax=new Vector2(.5f,0);((RectTransform)v.Back.transform).anchorMin=((RectTransform)v.Back.transform).anchorMax=new Vector2(.5f,0);
   return PrefabUtility.SaveAsPrefabAsset(r.gameObject,Folder+"/"+name+".prefab").GetComponent<InventoryInteractionView>();
  }finally{UnityEngine.Object.DestroyImmediate(r.gameObject);}
 }
 static RectTransform Content(string name,Transform parent){var r=Rect(name,parent,Vector2.zero,new Vector2(0,100));r.anchorMin=new Vector2(0,1);r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,1);return r;}
 static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();if(parent)r.SetParent(parent,false);r.anchoredPosition=position;r.sizeDelta=size;return r;}
 static void Stretch(RectTransform r,Vector2 min,Vector2 max){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=min;r.offsetMax=max;}
 static Image Background(RectTransform r,Color c){var image=r.gameObject.AddComponent<Image>();image.color=c;return image;}
 static TMP_Text Text(RectTransform r,int size){var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.fontSize=size;t.color=new Color(.88f,.94f,1);t.raycastTarget=false;t.alignment=TextAlignmentOptions.MidlineLeft;return t;}
 static InventoryButtonView Choice(string name,Transform parent,Vector2 position,Vector2 size){var r=Rect(name,parent,position,size);var v=r.gameObject.AddComponent<InventoryButtonView>();v.Select=r.gameObject.AddComponent<Button>();v.Select.targetGraphic=Background(r,new Color(.08f,.17f,.25f));var colors=v.Select.colors;colors.highlightedColor=new Color(.35f,.65f,.8f);colors.selectedColor=colors.highlightedColor;colors.disabledColor=new Color(.3f,.35f,.4f);v.Select.colors=colors;var label=Rect("Label",r,Vector2.zero,Vector2.zero);Stretch(label,new Vector2(10,4),new Vector2(-10,-4));v.Label=Text(label,15);v.Label.text=name;return v;}
}
[CustomEditor(typeof(InventoryInteractionLayout))]
public sealed class InventoryInteractionLayoutEditor:Editor {public override void OnInspectorGUI(){if(GUILayout.Button("Open live preview"))DetailsPanelPreviewWindow.Open((InventoryInteractionLayout)target);DrawDefaultInspector();}}
