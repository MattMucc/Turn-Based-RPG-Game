using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Game.GridPrototype;
public class DebtTreeTests {
 GameObject go;BattleCharacterInfo info;CharacterMenuData data;
 [SetUp] public void Setup(){go=new GameObject("Debt tree");info=go.AddComponent<BattleCharacterInfo>();data=go.AddComponent<CharacterMenuData>();data.DebtTree=true;data.SkillTree=DebtSkillTree.Create();}
 [TearDown] public void Cleanup(){Object.DestroyImmediate(go);}
 MenuSkillNode Node(string id)=>data.SkillTree.Single(n=>n.Id=="debt-"+id);
 GridUnit Unit(){var u=new GridUnit("Debt",Vector2Int.zero);u.BeginTurn();return u;}
 [Test] public void PointsFollowLevelAndUnlocksSpendOnlyOnce(){
  info.Level=1;Assert.AreEqual(0,info.SkillPoints);Assert.IsFalse(info.LearnNode(Node("loan")));
  info.Level=3;Assert.AreEqual(2,info.SkillPoints);Assert.IsTrue(info.LearnNode(Node("loan")));Assert.AreEqual(0,info.SkillPoints);Assert.IsFalse(info.LearnNode(Node("loan")));
  info.Level=4;Assert.AreEqual(1,info.SkillPoints);Assert.IsFalse(info.LearnNode(Node("extension")));
 }
 [Test] public void NegotiatedChoiceIsRequiredPermanentAndSaved(){
  info.Level=12;Assert.IsFalse(info.LearnNode(Node("negotiated"),"On Credit"));
  Assert.IsTrue(info.LearnNode(Node("extension")));int before=info.SkillPoints;
  Assert.IsFalse(info.LearnNode(Node("negotiated")));Assert.IsFalse(info.LearnNode(Node("negotiated"),"Loan"));Assert.AreEqual(before,info.SkillPoints);
  Assert.IsTrue(info.LearnNode(Node("negotiated"),"On Credit"));Assert.IsFalse(info.LearnNode(Node("negotiated"),"Unfair Terms"));Assert.AreEqual("On Credit",info.NegotiatedSkill);
  var saved=new SavedCharacter();info.CaptureProgress(saved);var copy=new GameObject("Saved");
  try{var restored=copy.AddComponent<BattleCharacterInfo>();restored.RestoreProgress(JsonUtility.FromJson<SavedCharacter>(JsonUtility.ToJson(saved)));Assert.AreEqual(6,restored.SkillPoints);Assert.AreEqual("On Credit",restored.NegotiatedSkill);Assert.IsTrue(restored.KnowsNode("debt-negotiated"));}finally{Object.DestroyImmediate(copy);}
 }
 [Test] public void PlaceholderUnavailableAndRedistributionDoesNotRequireLongReach(){
  info.Level=20;Assert.IsTrue(info.LearnNode(Node("redistribute")));Assert.IsFalse(info.KnowsNode("debt-reach"));Assert.IsTrue(info.LearnNode(Node("loan")));Assert.IsFalse(info.LearnNode(Node("outstanding")));
 }
 [Test] public void LoadoutUnlocksAndReachRefreshWithoutMutatingLibrary(){
  var kit=ScriptableObject.CreateInstance<SharedBattleActionLibrary>();
  try{kit.Actions=DebtStartingKit.Create().Concat(DebtSkillTree.Actions()).ToList();var loadout=go.AddComponent<BattleActionLoadout>();loadout.CharacterLibrary=kit;
   Assert.IsFalse(loadout.Options.Any(a=>a.Name=="Loan"));info.Level=20;Assert.IsTrue(info.LearnNode(Node("loan")));Assert.IsTrue(loadout.Options.Any(a=>a.Name=="Loan"));
   Assert.IsTrue(info.LearnNode(Node("reach")));Assert.AreEqual(4,loadout.Options.Single(a=>a.Name=="Assign Liability").Reach);Assert.AreEqual(3,kit.Actions.Single(a=>a.Name=="Assign Liability").Reach);
  }finally{Object.DestroyImmediate(kit);}
 }
 [Test] public void LoanAndExtensionShareQuickUseAndDoNotSpendMainAction(){
  var u=Unit();var old=u.Borrow(new DebtTerms{DueInTurns=3},"Old");var loan=DebtSkillTree.Actions()[0];
  Assert.IsTrue(u.TakeLoan(1,4,loan.Contract));Assert.AreEqual(7,u.ActionPoints);Assert.IsTrue(u.ActionAvailable);Assert.IsFalse(u.ExtendContract(old.Id,1));
  Assert.IsTrue(u.SpendActionWithActionPoints(1));u.EndTurn();u.BeginTurn();Assert.IsTrue(u.SpendActionWithActionPoints(0));Assert.IsTrue(u.ExtendContract(old.Id,1));Assert.IsFalse(u.ActionAvailable);Assert.IsTrue(old.Extended);
 }
 [Test] public void TransferredLoanStillBlocksBorrowingUntilPaidAndExtensionStaysUsed(){
  var u=Unit();var e=Unit();Assert.IsTrue(u.TakeLoan(1,4,DebtSkillTree.Actions()[0].Contract));var loan=u.Contracts.Single();
  Assert.IsTrue(u.TransferDebt(loan.Id,e,2));Assert.IsTrue(u.HasOutstandingLoan);u.EndTurn();u.BeginTurn();Assert.IsFalse(u.TakeLoan(1,4,new DebtTerms()));
  Assert.IsTrue(e.ExtendContract(loan.Id,1));e.InitializeActionPoints(9);Assert.IsTrue(e.Settle(new[]{loan.Id},1));Assert.IsFalse(u.HasOutstandingLoan);Assert.IsTrue(u.TakeLoan(1,4,new DebtTerms()));
 }
 [Test] public void TransferManyIsAtomicAllowsRepeatedRecipientAndPreservesExtension(){
  var u=Unit();u.InitializeActionPoints(9);var a=u.Borrow(new DebtTerms{DueInTurns=3},"A");var b=u.Borrow(new DebtTerms{DueInTurns=2},"B");var e=Unit();
  Assert.IsTrue(u.ExtendContract(a.Id,1));Assert.IsFalse(u.TransferDebts(new[]{a.Id,"missing"},new[]{e,e},3));Assert.AreEqual(8,u.ActionPoints);Assert.AreEqual(2,u.Contracts.Count);
  Assert.IsTrue(u.TransferDebts(new[]{a.Id,b.Id},new[]{e,e},3));Assert.AreEqual(5,u.ActionPoints);Assert.AreEqual(2,e.Contracts.Count);Assert.AreEqual(4,a.TurnsRemaining(e));Assert.IsFalse(e.ExtendContract(a.Id,1));Assert.IsTrue(e.QuickSkillAvailable);
 }
 [Test] public void ConsolidatedWaivesOnlyActivationAndCanOpenWithZeroAP(){
  var u=Unit();u.ConsolidatedPayment=true;u.GainMovement(3);u.InitializeActionPoints(0);
  var a=u.Borrow(new DebtTerms{Kind=DebtKind.Footing,RepayAP=0,RepayMovement=2},"A");var b=u.Borrow(new DebtTerms{Kind=DebtKind.Footing,RepayAP=0,RepayMovement=2},"B");
  Assert.IsTrue(DebtStartingKit.Create().Single(s=>s.DebtSkill==DebtSkill.Settle).CanUse(u));Assert.IsFalse(u.CanSettle(new[]{a.Id},1));Assert.IsTrue(u.Settle(new[]{a.Id,b.Id},1));Assert.AreEqual(2,u.Movement);Assert.AreEqual(0,u.ActionPoints);
 }
 [Test] public void NegotiatedAffectsOnlyNewContractsFromChosenSkill(){
  var u=Unit();var actions=DebtStartingKit.Create();actions[0].CommitDebt(u);u.NegotiatedSkill="On Credit";actions[0].CommitDebt(u);actions[1].CommitDebt(u);
  Assert.AreEqual(1,u.Contracts[0].TurnsRemaining(u));Assert.AreEqual(2,u.Contracts[1].TurnsRemaining(u));Assert.AreEqual(2,u.Contracts[2].TurnsRemaining(u));Assert.AreEqual(1,actions[0].Contract.DueInTurns);
 }
}
