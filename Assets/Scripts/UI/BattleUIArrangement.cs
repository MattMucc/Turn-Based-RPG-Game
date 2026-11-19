using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public static class BattleUIArrangement {
 public static void Copy(RectTransform source,RectTransform target){target.anchorMin=source.anchorMin;target.anchorMax=source.anchorMax;target.pivot=source.pivot;target.anchoredPosition=source.anchoredPosition;target.sizeDelta=source.sizeDelta;}
 public static void Order(BattleUILayout layout,ScrollRect scroll,IList<RectTransform> cards,int count,bool top){
  var template=(RectTransform)(top?layout.TopOrder:layout.RightOrder).transform;var panel=(RectTransform)scroll.transform;Copy(template,panel);
  var size=((RectTransform)layout.Portrait.transform).sizeDelta;float pad=layout.OrderPadding;
  var total=top?new Vector2(count*(size.x+layout.OrderSpacing.x)-layout.OrderSpacing.x+2*pad,size.y+2*pad):new Vector2(size.x+2*pad,count*(size.y+layout.OrderSpacing.y)-layout.OrderSpacing.y+2*pad);
  total=Vector2.Max(total,Vector2.one);panel.sizeDelta=top?new Vector2(Mathf.Min(template.sizeDelta.x,total.x),total.y):new Vector2(total.x,Mathf.Min(template.sizeDelta.y,total.y));
  var content=scroll.content;content.anchorMin=content.anchorMax=content.pivot=new Vector2(0,1);content.sizeDelta=total;
  scroll.horizontal=top;scroll.vertical=!top;
  for(int i=0;i<cards.Count;i++){var r=cards[i];r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.sizeDelta=size;r.anchoredPosition=top?new Vector2(pad+i*(size.x+layout.OrderSpacing.x),-pad):new Vector2(pad,-pad-i*(size.y+layout.OrderSpacing.y));}
 }
 public static void Resource(BattleUILayout layout,BattleResourceView view,bool expanded,bool statuses,float bottom){
  var r=(RectTransform)view.transform;var size=((RectTransform)layout.ResourceCard.transform).sizeDelta;
  r.anchorMin=r.anchorMax=r.pivot=new Vector2(1,0);r.sizeDelta=new Vector2(expanded?layout.ExpandedWidth:size.x,size.y+(statuses?layout.StatusExtraHeight:0));r.anchoredPosition=new Vector2(0,bottom);
  view.Background.color=expanded?layout.ActiveCard:layout.ResourceCard.Background.color;view.Statuses.gameObject.SetActive(statuses);
 }
}
