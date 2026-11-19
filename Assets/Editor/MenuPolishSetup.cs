using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class MenuPolishSetup {
 [MenuItem("Tools/Game/UI/Apply currency prices and scrollbars")]
 public static void Apply(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode first.");
  System.IO.Directory.CreateDirectory("Assets/Resources/UI/Common");
  string barPath="Assets/Resources/UI/Common/Scrollbar.prefab";
  if(!AssetDatabase.LoadAssetAtPath<GameObject>(barPath)){
   var root=new GameObject("Temporary scroll",typeof(RectTransform),typeof(ScrollRect));
   try{VisibleScrollbars.Ensure(root.GetComponent<ScrollRect>());var bar=root.GetComponent<ScrollRect>().verticalScrollbar;PrefabUtility.SaveAsPrefabAsset(bar.gameObject,barPath);}finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Resources/UI"})){
   string path=AssetDatabase.GUIDToAssetPath(guid);var root=PrefabUtility.LoadPrefabContents(path);bool changed=false;
   try{
    foreach(var scroll in root.GetComponentsInChildren<ScrollRect>(true))if(!scroll.verticalScrollbar||!scroll.horizontalScrollbar){VisibleScrollbars.Ensure(scroll);changed=true;}
    var shell=root.GetComponent<PlayerMenuShellView>();if(shell&&!shell.Currency){
     var go=new GameObject("Currency",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(root.transform,false);var t=go.GetComponent<TextMeshProUGUI>();shell.Currency=t;t.font=TMP_Settings.defaultFontAsset;t.fontSize=14;t.fontStyle=FontStyles.Normal;t.alignment=TextAlignmentOptions.MidlineRight;t.color=new Color(1,.8f,.35f);t.text="300 coins";t.raycastTarget=false;
     var r=t.rectTransform;r.anchorMin=r.anchorMax=r.pivot=Vector2.one;r.anchoredPosition=new Vector2(-24,-62);r.sizeDelta=new Vector2(210,22);changed=true;
    }
    var slot=root.GetComponent<MerchantSlotView>();if(slot){var sr=(RectTransform)slot.transform;sr.sizeDelta=new Vector2(sr.sizeDelta.x,sr.sizeDelta.x);changed=true;}if(slot&&!slot.Select.transform.Find("Price strip")){
     var go=new GameObject("Price strip",typeof(RectTransform),typeof(Image));go.transform.SetParent(slot.Select.transform,false);var r=(RectTransform)go.transform;r.anchorMin=Vector2.zero;r.anchorMax=new Vector2(1,0);r.pivot=Vector2.zero;r.offsetMin=Vector2.zero;r.offsetMax=new Vector2(0,20);var bg=go.GetComponent<Image>();bg.color=new Color(.01f,.02f,.03f,.9f);bg.raycastTarget=false;
     slot.Caption.transform.SetParent(r,false);var c=slot.Caption.rectTransform;c.anchorMin=Vector2.zero;c.anchorMax=Vector2.one;c.offsetMin=new Vector2(2,0);c.offsetMax=new Vector2(-2,0);slot.Caption.color=new Color(1,.83f,.45f);slot.Caption.raycastTarget=false;
     var select=(RectTransform)slot.Select.transform;select.offsetMin=Vector2.zero;changed=true;
    }
    if(path=="Assets/Resources/UI/Merchants/Action.prefab"){
     var v=root.GetComponent<InventoryButtonView>();v.Select.image.color=new Color(.22f,.53f,.4f);v.Label.text="Buy · 50 coins";v.Label.color=Color.white;var colors=v.Select.colors;colors.highlightedColor=new Color(1,1,.8f);colors.selectedColor=colors.highlightedColor;v.Select.colors=colors;changed=true;
    }
    var workspace=root.GetComponent<MerchantWorkspaceView>();if(workspace){workspace.Grid.cellSize=new Vector2(workspace.Grid.cellSize.x,workspace.Grid.cellSize.x);changed=true;}if(workspace&&workspace.Heading.text.Contains("coins")){workspace.Heading.text=path.Contains("Blacksmith")?"Blacksmith":"Shop";changed=true;}
    if(changed)PrefabUtility.SaveAsPrefabAsset(root,path);
   }finally{PrefabUtility.UnloadPrefabContents(root);}
  }
  AssetDatabase.SaveAssets();AssetDatabase.Refresh();
 }
}
