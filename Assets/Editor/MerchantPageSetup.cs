using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class MerchantPageSetup {
 const string Folder="Assets/Resources/UI/Merchants";
 static Color Ink=new Color(.88f,.94f,1),Panel=new Color(.035f,.075f,.12f);
 [MenuItem("Tools/Game/UI/Create missing Merchant templates")]
 public static void Create(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");Directory.CreateDirectory(Folder);
  var filter=Make<InventoryButtonView>("Filter",r=>{BuildButton(r,"Weapons");var l=r.gameObject.AddComponent<LayoutElement>();l.preferredWidth=100;l.flexibleWidth=1;});
  var mode=Make<InventoryButtonView>("Mode",r=>{BuildButton(r,"Buy");var l=r.gameObject.AddComponent<LayoutElement>();l.preferredWidth=250;l.flexibleWidth=1;});
  var action=Make<InventoryButtonView>("Action",r=>{BuildButton(r,"Review purchase");Bottom(r,94);});
  var cancel=Make<InventoryButtonView>("Cancel",r=>{BuildButton(r,"Cancel trade");Bottom(r,48);});
  var attack=Make<InventoryButtonView>("AttackDetails",r=>{BuildButton(r,"Attack details");r.sizeDelta=new Vector2(350,34);});
  var slot=Make<MerchantSlotView>("ItemSlot",r=>{
   r.sizeDelta=new Vector2(88,118);var v=r.GetComponent<MerchantSlotView>();var button=Rect("Select",r,0,0,88,88);v.Select=Button(button);
   v.Artwork=Rect("Artwork",button,8,8,72,72);v.Caption=Text("Price or owner",r,"50 coins",11,0,90,88,26);v.Caption.alignment=TextAlignmentOptions.Center;v.Caption.enableAutoSizing=true;v.Caption.fontSizeMin=9;v.Caption.fontSizeMax=11;
   button.anchorMin=Vector2.zero;button.anchorMax=Vector2.one;button.offsetMin=new Vector2(0,30);button.offsetMax=Vector2.zero;
   v.Artwork.anchorMin=Vector2.zero;v.Artwork.anchorMax=Vector2.one;v.Artwork.offsetMin=new Vector2(8,8);v.Artwork.offsetMax=new Vector2(-8,-8);
   v.Caption.rectTransform.anchorMin=Vector2.zero;v.Caption.rectTransform.anchorMax=new Vector2(1,0);v.Caption.rectTransform.anchoredPosition=new Vector2(0,28);v.Caption.rectTransform.sizeDelta=new Vector2(0,26);
  });
  foreach(bool forge in new[]{false,true}){
   string name=forge?"Blacksmith":"Shop";
   var workspace=Make<MerchantWorkspaceView>(name+"Workspace",r=>{
    r.sizeDelta=new Vector2(590,562);var v=r.GetComponent<MerchantWorkspaceView>();v.Heading=Text("Heading",r,name+" · 300 coins",20,0,0,590,34);Width(v.Heading.rectTransform);
    v.Modes=Rect("Buy and Sell",r,0,40,590,38);Width(v.Modes);Group(v.Modes);v.Modes.gameObject.SetActive(!forge);
    v.Filters=Rect("Filters",r,0,forge?44:86,590,32);Width(v.Filters);Group(v.Filters);
    v.Scroll=Scroll("Item grid",r,0,forge?88:128,590,350);var vp=v.Scroll.viewport;vp.anchorMin=Vector2.zero;vp.anchorMax=Vector2.one;vp.offsetMin=new Vector2(0,70);vp.offsetMax=new Vector2(0,forge?-88:-128);
    v.Grid=v.Scroll.content.gameObject.AddComponent<GridLayoutGroup>();v.Grid.cellSize=new Vector2(88,118);v.Grid.spacing=new Vector2(8,8);v.Grid.padding=new RectOffset(4,4,4,4);
    v.Empty=Text("Empty category",vp,"No items in this category.",16,12,12,550,44);Width(v.Empty.rectTransform,12,12);v.Empty.gameObject.SetActive(false);
    var leave=Rect("Leave",r,0,0,112,38);leave.anchorMin=leave.anchorMax=new Vector2(0,0);leave.anchoredPosition=new Vector2(0,48);v.Leave=leave.gameObject.AddComponent<InventoryButtonView>();BuildButton(leave,"Leave");leave.sizeDelta=new Vector2(112,38);
    v.Notice=Text("Notice",r,"",14,120,0,470,56);var n=v.Notice.rectTransform;n.anchorMin=new Vector2(0,0);n.anchorMax=new Vector2(1,0);n.anchoredPosition=new Vector2(120,58);n.sizeDelta=new Vector2(-120,56);
   });
   string path=Folder+"/"+name+".asset";if(!AssetDatabase.LoadAssetAtPath<MerchantPageLayout>(path)){var l=ScriptableObject.CreateInstance<MerchantPageLayout>();l.Blacksmith=forge;l.Workspace=workspace;l.Filter=filter;l.Mode=mode;l.Action=action;l.Cancel=cancel;l.Slot=slot;l.AttackDetails=attack;AssetDatabase.CreateAsset(l,path);}else{var existing=AssetDatabase.LoadAssetAtPath<MerchantPageLayout>(path);if(!existing.AttackDetails){existing.AttackDetails=attack;EditorUtility.SetDirty(existing);}}
  }
  AssetDatabase.SaveAssets();AssetDatabase.Refresh();
 }
 static void Group(RectTransform rect){var g=rect.gameObject.AddComponent<HorizontalLayoutGroup>();g.spacing=6;g.childControlWidth=g.childControlHeight=true;g.childForceExpandWidth=true;}
 static void BuildButton(RectTransform r,string name){r.sizeDelta=new Vector2(160,38);var v=r.GetComponent<InventoryButtonView>();v.Select=Button(r);v.Label=Text("Label",r,name,14,10,6,140,26);Width(v.Label.rectTransform,10,10);v.Label.enableAutoSizing=true;v.Label.fontSizeMin=9;v.Label.fontSizeMax=14;}
 static void Bottom(RectTransform r,float y){r.anchorMin=new Vector2(0,0);r.anchorMax=new Vector2(1,0);r.anchoredPosition=new Vector2(12,y);r.sizeDelta=new Vector2(-24,38);}
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
[CustomEditor(typeof(MerchantPageLayout))]
public sealed class MerchantPageLayoutEditor:Editor {public override void OnInspectorGUI(){if(GUILayout.Button("Open live preview"))DetailsPanelPreviewWindow.Open((MerchantPageLayout)target);DrawDefaultInspector();EditorGUILayout.HelpBox("Edit Workspace, item slots, filters, mode tabs and action/cancel prefabs. GridLayoutGroup controls spacing and cell size. Keep binding references assigned.",MessageType.Info);}}
