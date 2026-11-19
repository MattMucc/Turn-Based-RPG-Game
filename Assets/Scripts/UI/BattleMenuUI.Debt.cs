using System.Linq;
using Game.GridPrototype;
public partial class BattleMenuUI {
 void ContractPage(){
  var action=battle.ChosenAction;if(action==null)return;
  browsing=true;moving=false;battle.GridInputEnabled=battle.KeyboardGridInputEnabled=false;BeginList();
  var unit=battle.Combatants[battle.CurrentActorIndex];
  foreach(var contract in unit.Contracts){
   var id=contract.Id;bool selected=battle.SelectedContracts.Contains(id);
   Row((selected?"✓ ":"")+contract.Terms.Kind+" · "+contract.Deadline(unit),()=>{
    if(!battle.ToggleContract(id))return;
    if(action.DebtSkill==DebtSkill.Transfer){Open(category);battle.GridInputEnabled=battle.KeyboardGridInputEnabled=true;}
    else ContractPage();
   },contract.Details(unit),()=>battle.ActionReady);
  }
  if(action.DebtSkill==DebtSkill.Settle){
   unit.QuoteSettlement(battle.SelectedContracts,action.EffectiveActionPointCost,out int ap,out int movement);
   string details="Selected: "+battle.SelectedContracts.Count+" contract(s)\nTotal: "+ap+" AP + "+movement+" movement\nRemaining: "+(unit.ActionPoints-ap)+" AP, "+(unit.Movement-movement)+" movement";
   if(ap>unit.ActionPoints||movement>unit.Movement)details="Selected: "+battle.SelectedContracts.Count+" contract(s)\nTotal: "+ap+" AP + "+movement+" movement\nInsufficient resources.";
   Row("Settle · "+ap+" AP + "+movement+" move",()=>battle.SettleContracts(),details,()=>battle.CanSettleContracts);
   tooltip.text=details;tooltipPanel.SetActive(true);
  }
  Row("Back",()=>{battle.CancelAction();Open(category);},"",()=>battle.PlayerReady,true);pool[rows-1].SetIcons(backKeyboardIcon,runControllerIcon);FinishList();
 }
}
