using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Game.GridPrototype;
public class DebtContractTests {
 GridUnit Unit(){var u=new GridUnit("Debt",Vector2Int.zero);u.BeginTurn();return u;}
 [Test] public void CreationTurnDoesNotCountAndCollectionIsIdempotent(){
  var u=Unit();u.Borrow(new DebtTerms(),"On Credit");u.EndTurn();Assert.AreEqual(1,u.Contracts.Count);
  u.BeginTurn();Assert.AreEqual(0,u.Contracts[0].TurnsRemaining(u));Assert.AreEqual(5,u.ActionPoints);u.EndTurn();Assert.AreEqual(2,u.ActionPoints);Assert.AreEqual(0,u.Contracts.Count);u.EndTurn();Assert.AreEqual(2,u.ActionPoints);
 }
 [Test] public void CreditShortfallWithholdsFutureGainsIncludingAttackRecovery(){
  var u=Unit();u.Borrow(new DebtTerms(),"Credit");u.EndTurn();u.BeginTurn();u.SpendActionWithActionPoints(5);u.EndTurn();Assert.AreEqual(3,u.APCollection);u.BeginTurn();Assert.AreEqual(0,u.ActionPoints);Assert.AreEqual(2,u.APCollection);u.RestoreActionPoints(3);Assert.AreEqual(1,u.ActionPoints);Assert.AreEqual(0,u.APCollection);
 }
 [Test] public void FootingOnlyWithholdsFutureGrantsNotBankedMovement(){
  var u=Unit();u.Borrow(new DebtTerms{Kind=DebtKind.Footing,DueInTurns=2},"Footing");u.EndTurn();u.BeginTurn();u.EndTurn();Assert.AreEqual(1,u.Contracts.Count);u.BeginTurn();u.EndTurn();Assert.AreEqual(9,u.Movement);Assert.AreEqual(3,u.MovementCollection);u.BeginTurn();Assert.AreEqual(9,u.Movement);Assert.AreEqual(0,u.MovementCollection);
 }
 [Test] public void VigorStacksSurviveDefaultTurnThenTickNormally(){
  var u=Unit();u.Borrow(new DebtTerms{Kind=DebtKind.Vigor,DefaultAmount=2},"Vigor");u.EndTurn();u.BeginTurn();u.EndTurn();Assert.AreEqual(2,u.Stacks(CombatStatus.Weakened));u.BeginTurn();u.EndTurn();Assert.AreEqual(1,u.Stacks(CombatStatus.Weakened));
 }
 [Test] public void SettlementPaysOneFeeAndAllSelectedPricesAtomically(){
  var u=Unit();u.InitializeActionPoints(9);var a=u.Borrow(new DebtTerms(),"A");var b=u.Borrow(new DebtTerms{Kind=DebtKind.Footing,RepayAP=0,RepayMovement=2},"B");var c=u.Borrow(new DebtTerms(),"C");
  Assert.IsTrue(u.Settle(new[]{a.Id,b.Id},1));Assert.AreEqual(6,u.ActionPoints);Assert.AreEqual(1,u.Movement);Assert.IsFalse(u.ActionAvailable);Assert.AreEqual(c,u.Contracts.Single());Assert.IsFalse(u.Settle(new[]{c.Id},1));
 }
 [Test] public void UnaffordableStaleDuplicateAndEmptySelectionsSpendNothing(){
  var u=Unit();var a=u.Borrow(new DebtTerms{RepayAP=9},"A");
  Assert.IsFalse(u.Settle(new[]{a.Id},1));Assert.IsFalse(u.Settle(new[]{"missing"},1));Assert.IsFalse(u.Settle(new[]{a.Id,a.Id},1));Assert.IsFalse(u.Settle(new string[0],1));Assert.AreEqual(4,u.ActionPoints);Assert.AreEqual(3,u.Movement);Assert.IsTrue(u.ActionAvailable);Assert.AreEqual(1,u.Contracts.Count);
 }
 [Test] public void TransferPreservesRemainingTurnsAndDueDebtGetsRecipientOpportunity(){
  var u=Unit();var enemy=new GridUnit("Enemy",Vector2Int.one);var c=u.Borrow(new DebtTerms{DueInTurns=3},"Long");u.EndTurn();u.BeginTurn();Assert.IsTrue(u.TransferDebt(c.Id,enemy,2));Assert.AreEqual(2,c.TurnsRemaining(enemy));enemy.BeginTurn();enemy.EndTurn();Assert.AreEqual(1,enemy.Contracts.Count);enemy.BeginTurn();Assert.IsTrue(enemy.TryEnemySettlement());Assert.AreEqual(0,enemy.Contracts.Count);Assert.IsFalse(enemy.ActionAvailable);
  u.EndTurn();u.BeginTurn();var due=u.Borrow(new DebtTerms(),"Due");u.EndTurn();u.BeginTurn();Assert.IsTrue(u.TransferDebt(due.Id,enemy,2));Assert.AreEqual(1,due.TurnsRemaining(enemy));
 }
 [Test] public void EnemyCannotSettleWithoutResourcesAndDefaultsInstead(){
  var u=Unit();u.Borrow(new DebtTerms{Kind=DebtKind.Vigor,RepayMovement=9,DefaultAmount=2},"Too costly");u.EndTurn();u.BeginTurn();Assert.IsFalse(u.TryEnemySettlement());Assert.IsTrue(u.ActionAvailable);u.EndTurn();Assert.AreEqual(2,u.Stacks(CombatStatus.Weakened));
 }
 [Test] public void CommitmentBorrowsOnMissAndMovementIsCapped(){
  var u=Unit();u.GainMovement(6);var skill=DebtStartingKit.Create()[1];skill.HitChance=0;Assert.IsTrue(u.SpendActionWithActionPoints(skill.EffectiveActionPointCost));skill.CommitDebt(u);Assert.AreEqual(10,u.Movement);Assert.IsFalse(skill.ApplyHit(u,new GridUnit("Enemy",Vector2Int.one),skill.Damage,1,1,out _));Assert.AreEqual(1,u.Contracts.Count);
 }
 [Test] public void DefaultSnapshotsTermsAndStatusesRemainSafeToInspect(){
  var u=Unit();var terms=new DebtTerms();u.Borrow(terms,"A");terms.RepayAP=99;Assert.AreEqual(2,u.Contracts[0].Terms.RepayAP);Assert.DoesNotThrow(()=>StatusPresentation.For(u));Assert.IsTrue(u.EffectDetails().Contains("Repayment"));
 }
 [Test] public void SeparateContractsKeepSeparateDeadlines(){
  var u=Unit();u.Borrow(new DebtTerms(),"First");u.EndTurn();u.BeginTurn();u.Borrow(new DebtTerms(),"Second");u.EndTurn();Assert.AreEqual("Second",u.Contracts.Single().Source);
 }
 [Test] public void CharacterLibraryRemainsDistinctFromSharedSkillsAndSurvivesSavedPersonalActions(){
  var go=new GameObject("Debt");var kit=ScriptableObject.CreateInstance<SharedBattleActionLibrary>();
  try{kit.Actions=DebtStartingKit.Create();var loadout=go.AddComponent<BattleActionLoadout>();loadout.CharacterLibrary=kit;Assert.AreEqual(5,loadout.Options.Count(a=>a.DebtSkill!=DebtSkill.None));kit.Actions[0].Damage=77;kit.NotifyChanged();Assert.AreEqual(77,loadout.Options.First(a=>a.Name=="On Credit").Damage);loadout.RestoreSavedActions(new SavedCharacter(),new PartyInventory());Assert.AreEqual(5,loadout.Options.Count(a=>a.DebtSkill!=DebtSkill.None));}
  finally{Object.DestroyImmediate(go);Object.DestroyImmediate(kit);}
 }
}
