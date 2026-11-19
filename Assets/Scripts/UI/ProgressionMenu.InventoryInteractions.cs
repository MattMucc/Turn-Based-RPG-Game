using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public sealed partial class ProgressionMenu {
 [SerializeField] InventoryInteractionLayout interactionLayout;
 InventoryInteractionLayout Interactions=>interactionLayout?interactionLayout:Resources.Load<InventoryInteractionLayout>("UI/InventoryInteractions/Interactions");
 RectTransform CreateInteractionOverlay(){
  if(inventoryStep>0&&Interactions&&Interactions.Overlay){var r=Instantiate(Interactions.Overlay,canvasObject.transform,false);r.name="Inventory interaction";return r;}
  var fallback=Rect("Inventory interaction",canvasObject.transform,0,0,0,0);fallback.anchorMin=Vector2.zero;fallback.anchorMax=Vector2.one;fallback.offsetMin=fallback.offsetMax=Vector2.zero;fallback.gameObject.AddComponent<Image>().color=new Color(0,0,0,.6f);return fallback;
 }
 void ComposeInventoryInteraction(RectTransform original,int start){
  var layout=Interactions;var template=layout?layout.For(inventoryStep,!string.IsNullOrEmpty(inventoryItem.LearnSpellId)):null;
  if(!template||!layout.Recipient)return;
  var view=Instantiate(template,inventoryModal,false);view.name="Item context";
  string Read(params string[] names)=>string.Join("\n",original.GetComponentsInChildren<TMP_Text>(true).Where(t=>names.Contains(t.name)).Select(t=>t.text));
  view.Title.text=Read("Item name");view.Lead.text=Read("Choose target","Choose equipment wearer","Choose accessory slot");
  view.Description.text=Read("Item description","Learning preview","Recovery preview","Equipment stat changes","Unavailable equipment","Unavailable wearer");
  view.Notice.text=Read("Equipment change notice");view.Confirm.gameObject.SetActive(false);
  int recipients=0;
  for(int i=start;i<buttons.Count;i++){
   var old=buttons[i];if(!old||!old.transform.IsChildOf(original))continue;
   string caption=old.GetComponentInChildren<TMP_Text>().text;
   InventoryButtonView replacement;
   if(caption=="Back")replacement=view.Back;
   else if(caption.StartsWith("Confirm")){replacement=view.Confirm;replacement.gameObject.SetActive(true);}
   else {replacement=InventoryModularUI.Choice(layout.Recipient,view.Choices,caption);recipients++;}
   replacement.name=old.name;replacement.Label.text=caption;replacement.Select.onClick=old.onClick;replacement.Select.interactable=old.interactable;
   var focus=replacement.GetComponent<PlayerMenuFocus>();if(!focus)focus=replacement.gameObject.AddComponent<PlayerMenuFocus>();
   buttons[i]=replacement.Select;
  }
  view.Choices.gameObject.SetActive(recipients>0);view.Description.gameObject.SetActive(recipients==0);
  view.Lead.gameObject.SetActive(!string.IsNullOrEmpty(view.Lead.text));view.Notice.gameObject.SetActive(!string.IsNullOrEmpty(view.Notice.text));
  view.SizeContent();original.gameObject.SetActive(false);if(Application.isPlaying)Destroy(original.gameObject);else DestroyImmediate(original.gameObject);
 }
}
