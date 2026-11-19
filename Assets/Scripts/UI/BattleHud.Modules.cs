using System.Linq;
using UnityEngine;
public sealed partial class BattleHud {
 public BattleUILayout Layout {get;set;}
 BattleUILayout UI=>Layout?Layout:Resources.Load<BattleUILayout>("UI/Battle/Battle");
 RectTransform partyStack;
 bool BuildModularOrder(){if(!UI||!UI.TopOrder)return false;orderScroll=Instantiate(UI.TopOrder,root,false);orderPanel=(RectTransform)orderScroll.transform;orderContent=orderScroll.content;return true;}
 Card ModularPortrait(){
  var v=Instantiate(UI.Portrait,orderContent,false);var card=new Card{Rect=(RectTransform)v.transform,Border=v.Border,Portrait=v.Portrait,Health=v.Health,HealthFill=v.HealthFill,Shield=v.Shield,ShieldFill=v.ShieldFill,ShieldText=v.ShieldText,Hits=v.Hits};
  var click=v.gameObject.AddComponent<BattlePortrait>();click.LeftClick=()=>{battle.FocusCharacter(card.Index);battle.TargetFocus.Pin(battle.Targetables[card.Index]);};click.RightClick=p=>ShowContext(card.Index,p);return card;
 }
 bool LayoutModularOrder(int count){if(!UI)return false;BattleUIArrangement.Order(UI,orderScroll,cards.Select(c=>c.Rect).ToList(),count,top);return true;}
}
