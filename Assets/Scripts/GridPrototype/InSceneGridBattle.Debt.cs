using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Game.GridPrototype {
 public sealed partial class InSceneGridBattle {
  readonly List<string> selectedContracts=new();
  bool debtAssignments;
  public bool CanChooseAction(BattleActionOption option)=>PlayerReady&&option!=null&&(Active.ActionAvailable||option.IsQuick)&&CanAfford(option);
  public bool HasQuickSkill=>PlayerReady&&Loadout.Options.Any(a=>a.IsQuick&&CanChooseAction(a));
  public IReadOnlyList<string> SelectedContracts=>selectedContracts;
  public bool ChoosingContracts=>ChosenAction!=null&&(ChosenAction.DebtSkill==DebtSkill.Settle||ChosenAction.DebtSkill==DebtSkill.Extend||(ChosenAction.DebtSkill==DebtSkill.Redistribute&&!debtAssignments)||(ChosenAction.DebtSkill==DebtSkill.Transfer&&selectedContracts.Count==0));
  public bool ToggleContract(string id){
   if(!CanChooseAction(ChosenAction)||ChosenAction==null||!Active.Contracts.Any(c=>c.Id==id))return false;
   if(ChosenAction.DebtSkill==DebtSkill.Transfer||ChosenAction.DebtSkill==DebtSkill.Extend){selectedContracts.Clear();selectedContracts.Add(id);return true;}
   if(ChosenAction.DebtSkill!=DebtSkill.Settle&&ChosenAction.DebtSkill!=DebtSkill.Redistribute)return false;
   if(!selectedContracts.Remove(id)){if(ChosenAction.DebtSkill==DebtSkill.Redistribute&&selectedContracts.Count>=3)return false;selectedContracts.Add(id);}return true;
  }
  public bool CanSettleContracts=>ActionReady&&ChosenAction?.DebtSkill==DebtSkill.Settle&&Active.CanSettle(selectedContracts,ChosenAction.EffectiveActionPointCost);
  public bool SettleContracts(){
   if(!CanSettleContracts)return false;
   if(!Active.Settle(selectedContracts,ChosenAction.EffectiveActionPointCost))return false;
   RecentHitFeedback=Active.LastDebtNotice;RecentHitFeedbackUntil=Time.unscaledTime+3;
   CancelAction();ActionResolved?.Invoke();Refresh();return true;
  }
  public string DebtAssignmentDetails=>string.Join("\n",selectedContracts.Select((id,i)=>{var c=Active.Contracts.FirstOrDefault(c=>c.Id==id);var target=i<targets.Count?DebtRecipient(targets[i]):null;return (i+1)+". "+(c==null?"Missing contract":c.Terms.Kind+" · "+c.Source)+" → "+(target==null?"Unassigned":target.Name); }));
  public bool StartDebtAssignments(){if(!ActionReady||ChosenAction?.DebtSkill!=DebtSkill.Redistribute||selectedContracts.Count==0)return false;debtAssignments=true;return true;}
  public bool ConfirmRedistribution(){if(ChosenAction?.DebtSkill!=DebtSkill.Redistribute||!ReadyToResolve)return false;if(!TryPrepareAction(out var sequence))return false;StartCoroutine(sequence);return true;}
  public bool UseQuickSkill(){
   var action=ChosenAction;if(!CanChooseAction(action)||!action.IsQuick)return false;
   bool used=action.DebtSkill==DebtSkill.Loan?Active.TakeLoan(action.EffectiveActionPointCost,action.ActionPointRecovery,action.Contract):selectedContracts.Count==1&&Active.ExtendContract(selectedContracts[0],action.EffectiveActionPointCost);
   if(!used)return false;RecentHitFeedback=action.Name+" used · Quick Skill spent";RecentHitFeedbackUntil=UnityEngine.Time.unscaledTime+3;CancelAction();ActionResolved?.Invoke();Refresh();return true;
  }
  GridUnit DebtRecipient(Vector2Int cell)=>battleTargets.FirstOrDefault(t=>t.Unit!=null&&t.Alive&&t.ActorIndex>=allyCount&&t.Unit.Position==cell)?.Unit;
  bool ValidDebtTarget(BattleActionOption option,Vector2Int cell)=>option==null||(option.DebtSkill!=DebtSkill.Transfer&&option.DebtSkill!=DebtSkill.Redistribute)||DebtRecipient(cell)!=null;
 }
}
