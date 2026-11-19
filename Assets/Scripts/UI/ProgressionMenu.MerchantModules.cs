using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public sealed partial class ProgressionMenu {
 [SerializeField] MerchantPageLayout shopLayout,blacksmithLayout;
 MerchantPageLayout activeMerchantLayout;
 MerchantWorkspaceView modularMerchant;
 MerchantSlotView lastMerchantSlot;
 void BeginMerchantLayout(bool forge){
  modularMerchant=null;lastMerchantSlot=null;activeMerchantLayout=forge?(blacksmithLayout?blacksmithLayout:Resources.Load<MerchantPageLayout>("UI/Merchants/Blacksmith")):(shopLayout?shopLayout:Resources.Load<MerchantPageLayout>("UI/Merchants/Shop"));
  if(activeMerchantLayout&&activeMerchantLayout.Workspace)modularMerchant=InventoryModularUI.Fill(activeMerchantLayout.Workspace,body);
 }
 Button MerchantAttackButton(Transform parent,string name,float y,float width,Action click){
  if(!modularMerchant||!activeMerchantLayout.AttackDetails)return Button(parent,name,12,y,width,click,null,34);
  var view=Instantiate(activeMerchantLayout.AttackDetails,parent,false);view.Label.text=name;view.Select.name=name;
  var rect=(RectTransform)view.transform;rect.anchoredPosition=new Vector2(12,-y);rect.sizeDelta=new Vector2(width,rect.sizeDelta.y);
  buttons.Add(view.Select);view.Select.onClick.AddListener(()=>click());view.gameObject.AddComponent<PlayerMenuFocus>();return view.Select;
 }
 Button MerchantButton(Transform parent,string name,float x,float y,float width,Action click,Action preview=null,float height=38){
  if(!modularMerchant)return Button(parent,name,x,y,width,click,preview,height);
  Button button;TMP_Text label=null;
  if(parent==modularMerchant.Scroll.content){lastMerchantSlot=Instantiate(activeMerchantLayout.Slot,parent,false);button=lastMerchantSlot.Select;}
  else {
   InventoryButtonView view;
   if(parent==detailRoot)view=Instantiate(name.StartsWith("Cancel")?activeMerchantLayout.Cancel:activeMerchantLayout.Action,parent,false);
   else if(name=="Leave"||name=="Back")view=modularMerchant.Leave;
   else if(name=="Buy"||name=="Sell")view=InventoryModularUI.Choice(activeMerchantLayout.Mode,modularMerchant.Modes,name);
   else view=InventoryModularUI.Choice(activeMerchantLayout.Filter,modularMerchant.Filters,name);
   button=view.Select;label=view.Label;
  }
  button.name=name;if(label)label.text=name;buttons.Add(button);button.onClick.AddListener(()=>click?.Invoke());
  var focus=button.GetComponent<PlayerMenuFocus>();if(!focus)focus=button.gameObject.AddComponent<PlayerMenuFocus>();focus.Preview=preview;return button;
 }
 RectTransform MerchantList(Transform parent,string name,float x,float y,float width,float height,float contentHeight){
  if(!modularMerchant)return List(parent,name,x,y,width,height,contentHeight);
  modularMerchant.Scroll.name=name;return modularMerchant.Scroll.content;
 }
 TMP_Text MerchantLabel(RectTransform temporary,string value,int size=17){
  if(!modularMerchant)return Label(temporary,value,size);
  TMP_Text target=temporary.name=="Merchant"||temporary.name=="Upgrade heading"?modularMerchant.Heading:
   temporary.name=="Trade notice"||temporary.name=="Upgrade notice"?modularMerchant.Notice:
   temporary.name=="Price"||temporary.name=="Equipped owner"?lastMerchantSlot?.Caption:modularMerchant.Empty;
  temporary.gameObject.SetActive(false);if(Application.isPlaying)Destroy(temporary.gameObject);else DestroyImmediate(temporary.gameObject);
  target.text=value;return target;
 }
 void MerchantIcon(Button button,Game.GridPrototype.BattleActionOption item,float size){
  if(!modularMerchant){button.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);DrawInventoryIcon(button.transform,item,8,8,size-16);return;}
  var slot=button.GetComponentInParent<MerchantSlotView>();Canvas.ForceUpdateCanvases();DrawInventoryIcon(slot.Artwork,item,0,0,Mathf.Min(slot.Artwork.rect.width,slot.Artwork.rect.height));
 }
 void FinishMerchantLayout(int count,bool pending){
  if(!modularMerchant)return;
  modularMerchant.Empty.gameObject.SetActive(count==0);modularMerchant.Leave.gameObject.SetActive(true);modularMerchant.Leave.Select.interactable=!pending;
  Canvas.ForceUpdateCanvases();InventoryModularUI.SizeGrid(modularMerchant.Grid,count,modularMerchant.Scroll.viewport.rect.width);
 }
}
