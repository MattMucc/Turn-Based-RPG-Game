using NUnit.Framework;
using UnityEngine;
using Game.GridPrototype;

public class DebtCombatTests
{
    GridUnit Unit() => new GridUnit("Debt test", Vector2Int.zero) { UsesDebt = true };
    [Test] public void NewDebtWaitsForNextBearerTurnAndCollectionsRemoveOnlyDueStacks()
    {
        var u = Unit(); u.BeginTurn(); u.Borrow(DebtKind.Blood,2); u.EndTurn();
        Assert.AreEqual(100,u.Health); Assert.AreEqual(2,u.DebtCount);
        u.BeginTurn(); u.Borrow(DebtKind.Blood,1); u.EndTurn();
        Assert.AreEqual(90,u.Health); Assert.AreEqual(1,u.DebtCount);
        u.BeginTurn();u.EndTurn();Assert.AreEqual(85,u.Health);Assert.AreEqual(0,u.DebtCount);
    }
    [Test] public void TransferGivesNewBearerAResponseButDoesNotRefreshOldDeadline()
    {
        var a=Unit();var b=Unit();a.BeginTurn();a.Borrow(DebtKind.Blood,1);
        var debt=a.Debts[0];a.RemoveDebt(debt);b.ReceiveDebt(debt);
        Assert.AreEqual(1,debt.DueOn(b));
        b.BeginTurn();b.RemoveDebt(debt);a.ReceiveDebt(debt);b.EndTurn();a.EndTurn();
        a.BeginTurn();a.RemoveDebt(debt);b.ReceiveDebt(debt);b.RemoveDebt(debt);a.ReceiveDebt(debt);a.EndTurn();
        Assert.AreEqual(95,a.Health);Assert.AreEqual(100,b.Health);
    }
    [Test] public void LiabilitiesCreateIndependentStatusesAndRespectBarrier()
    {
        var u=Unit();u.BeginTurn();u.AddBarrier(7);u.Borrow(DebtKind.Blood,2);u.Borrow(DebtKind.Might,1);u.Borrow(DebtKind.Guard,1);u.EndTurn();
        u.BeginTurn();u.EndTurn();Assert.AreEqual(97,u.Health);Assert.AreEqual(0,u.Barrier);
        Assert.AreEqual(1,u.Stacks(CombatStatus.Weakened));Assert.AreEqual(1,u.Stacks(CombatStatus.Exposed));
        u.BeginTurn();Assert.AreEqual(1,u.Stacks(CombatStatus.Weakened));u.EndTurn();Assert.AreEqual(0,u.Stacks(CombatStatus.Weakened));
    }
    [Test] public void StrengthBonusIsOnlyForSkillsAndThresholdsTrackCurrentDebt()
    {
        var u=Unit();u.Borrow(DebtKind.Blood,6);
        var skill=new BattleActionOption { Category=BattleActionCategory.Skill,Damage=100 };
        Assert.AreEqual(130,skill.CalculateDamage(u,null,100));
        skill.Category=BattleActionCategory.Attack;Assert.AreEqual(100,skill.CalculateDamage(u,null,100));
        var transfer=new BattleActionOption { Effect=ActionEffect.TransferDebt };transfer.UpdateDebtShape(u);
        Assert.AreEqual(BattleTargetPattern.Multiple,transfer.Pattern);Assert.AreEqual(4,u.TransferCapacity);
        var cash=new BattleActionOption { Effect=ActionEffect.Foreclose };cash.UpdateDebtShape(u);Assert.AreEqual(BattleTargetPattern.Single,cash.Pattern);
        u.Borrow(DebtKind.Guard,3);cash.UpdateDebtShape(u);Assert.AreEqual(BattleTargetPattern.Area,cash.Pattern);
        u.SettleDebts();transfer.UpdateDebtShape(u);Assert.AreEqual(BattleTargetPattern.Single,transfer.Pattern);
    }
    [Test] public void WeaknessAndExposureUseSameDamageMathAsPreviews()
    {
        var source=Unit();var target=Unit();target.PhysicalDefense=100;
        source.ApplyStatus(CombatStatus.Weakened,1);target.ApplyStatus(CombatStatus.Exposed,1);
        var action=new BattleActionOption { Damage=100 };
        int preview=action.CalculateDamage(source,target,100);
        Assert.AreEqual(46,preview);action.ApplyHit(source,target,100,0,0,out int actual);Assert.AreEqual(preview,actual);
        target.Blocking=true;Assert.AreEqual(23,action.CalculateDamage(source,target,100));
    }
    [Test] public void DebtPresentationShowsSeparateDeadlinesAndStackCounts()
    {
        var u=Unit();u.BeginTurn();u.Borrow(DebtKind.Might,1);u.EndTurn();u.BeginTurn();u.Borrow(DebtKind.Might,2);
        var badge=StatusPresentation.For(u).Find(x=>x.Name=="Might Debt");
        Assert.AreEqual("3",badge.Count);StringAssert.Contains("1 due at the end of this turn",badge.Tooltip);StringAssert.Contains("2 due at the end of turn +1",badge.Tooltip);
    }
}
