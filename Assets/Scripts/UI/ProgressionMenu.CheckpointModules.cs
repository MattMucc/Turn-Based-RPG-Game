using UnityEngine;
public sealed partial class ProgressionMenu {
 [SerializeField] CheckpointUILayout checkpointLayout;
 CheckpointUILayout CheckpointLayout=>checkpointLayout?checkpointLayout:Resources.Load<CheckpointUILayout>("UI/Checkpoint/Checkpoint");
 bool ModularCheckpoint(){
  var l=CheckpointLayout;if(!l||!l.Menu||!l.Details)return false;
  var view=InventoryModularUI.Fill(l.Menu,body);Clear(detailRoot);var details=InventoryModularUI.Fill(l.Details,detailRoot);
  bool available=checkpointContext;string name=available?checkpointContext.DisplayName:"Checkpoint";
  view.Present(name,restConfirmation,available);
  if(available&&!menuSaveService){menuSaveService=Manager.Saves;menuSaveService.Changed+=SaveMenuChanged;}
  details.Title.text=name;details.Description.text=available?"Rest restores HP and MP for the whole party, including reserves and knocked-out members.\n\nRoaming groups return. One-off and story encounters stay cleared.":"This checkpoint is no longer available.";
  details.Notice.text=available?Manager.Saves.Notice:"";
  void Bind(InventoryButtonView button,string label,System.Action action,System.Action hover=null){
   button.name=label;button.Label.text=label;button.Select.onClick.AddListener(()=>action());if(button.gameObject.activeInHierarchy)buttons.Add(button.Select);
   var focus=button.GetComponent<PlayerMenuFocus>();if(!focus)focus=button.gameObject.AddComponent<PlayerMenuFocus>();focus.Preview=hover;
  }
  Bind(view.Rest,"Rest",()=>{restConfirmation=true;Rebuild();},()=>{details.Title.text="Rest";details.Description.text="Recover the whole party and reset roaming groups, then update the checkpoint autosave. One-off and story encounters stay cleared.";});
  Bind(view.Save,"Save",()=>ShowPage(Page.Saves),()=>{details.Title.text="Save";details.Description.text="Save to a manual slot. Party recovery and roaming encounters are unchanged.";});
  Bind(view.Leave,"Leave",Close);
  Bind(view.Confirm,"Confirm rest",()=>{restConfirmation=false;Manager.Saves.Rest(checkpointContext);});
  Bind(view.Cancel,"Cancel rest",()=>{restConfirmation=false;Rebuild();});return true;
 }
}
