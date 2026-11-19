using UnityEngine;
using UnityEngine.UI;
public static class VisibleScrollbars {
 public static void EnsureAll(GameObject root){foreach(var scroll in root.GetComponentsInChildren<ScrollRect>(true))Ensure(scroll);}
 public static void Ensure(ScrollRect scroll){
  if(!scroll)return;
  if(!scroll.verticalScrollbar)scroll.verticalScrollbar=Create(scroll,true);
  if(!scroll.horizontalScrollbar)scroll.horizontalScrollbar=Create(scroll,false);
  scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
  scroll.horizontalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
 }
 static Scrollbar Create(ScrollRect scroll,bool vertical){
  var template=Resources.Load<Scrollbar>("UI/Common/Scrollbar");
  Scrollbar bar;
  if(template)bar=Object.Instantiate(template,scroll.transform,false);
  else{
   var go=new GameObject("Scrollbar",typeof(RectTransform),typeof(Image),typeof(Scrollbar));go.transform.SetParent(scroll.transform,false);
   var track=go.GetComponent<Image>();track.color=new Color(.02f,.035f,.05f,.85f);bar=go.GetComponent<Scrollbar>();
   var handle=new GameObject("Handle",typeof(RectTransform),typeof(Image));handle.transform.SetParent(go.transform,false);
   var rect=(RectTransform)handle.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
   var image=handle.GetComponent<Image>();image.color=new Color(.55f,.7f,.78f,.9f);bar.handleRect=rect;bar.targetGraphic=image;
  }
  bar.name=vertical?"Vertical scrollbar":"Horizontal scrollbar";
  var r=(RectTransform)bar.transform;r.pivot=new Vector2(1,0);
  r.anchorMin=vertical?new Vector2(1,0):Vector2.zero;r.anchorMax=vertical?Vector2.one:new Vector2(1,0);
  r.offsetMin=vertical?new Vector2(-7,9):new Vector2(2,2);r.offsetMax=vertical?new Vector2(-2,-2):new Vector2(-9,7);
  bar.direction=vertical?Scrollbar.Direction.BottomToTop:Scrollbar.Direction.LeftToRight;
  bar.navigation=new Navigation{mode=Navigation.Mode.None};return bar;
 }
}
