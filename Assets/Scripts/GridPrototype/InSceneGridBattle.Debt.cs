using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Game.GridPrototype {
 public sealed partial class InSceneGridBattle {
  readonly List<string> selectedContracts=new();
  public IReadOnlyList<string> SelectedContracts=>selectedContracts;
  public bool ChoosingContracts=>ChosenAction!=null&&(ChosenAction.DebtSkill==DebtSkill.Settle||(ChosenAction.DebtSkill==DebtSkill.Transfer&&selectedContracts.Count==0));
  public bool ToggleContract(string id){
   if(!ActionReady||ChosenAction==null||!Active.Contracts.Any(c=>c.Id==id))return false;
   if(ChosenAction.DebtSkill==DebtSkill.Transfer){selectedContracts.Clear();selectedContracts.Add(id);return true;}
   if(ChosenAction.DebtSkill!=DebtSkill.Settle)return false;
   if(!selectedContracts.Remove(id))selectedContracts.Add(id);return true;
  }
  public bool CanSettleContracts=>ActionReady&&ChosenAction?.DebtSkill==DebtSkill.Settle&&Active.CanSettle(selectedContracts,ChosenAction.EffectiveActionPointCost);
  public bool SettleContracts(){
   if(!CanSettleContracts)return false;
   if(!Active.Settle(selectedContracts,ChosenAction.EffectiveActionPointCost))return false;
   RecentHitFeedback=Active.LastDebtNotice;RecentHitFeedbackUntil=Time.unscaledTime+3;
   CancelAction();ActionResolved?.Invoke();Refresh();return true;
  }
  GridUnit DebtRecipient(Vector2Int cell)=>battleTargets.FirstOrDefault(t=>t.Unit!=null&&t.Alive&&t.ActorIndex>=allyCount&&t.Unit.Position==cell)?.Unit;
  bool ValidDebtTarget(BattleActionOption option,Vector2Int cell)=>option==null||option.DebtSkill!=DebtSkill.Transfer||DebtRecipient(cell)!=null;
 }
}
