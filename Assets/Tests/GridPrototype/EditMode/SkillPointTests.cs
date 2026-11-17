using NUnit.Framework;
using UnityEngine;
using Game.GridPrototype;

public class SkillPointTests
{
    [Test] public void FirstTurnAddsOneToStartingThreeAndRecoveryCapsAtNine()
    {
        var unit = new GridUnit("Ally", Vector2Int.zero);
        Assert.AreEqual(3, unit.ActionPoints);
        unit.BeginTurn(); Assert.AreEqual(4, unit.ActionPoints);
        unit.RestoreActionPoints(int.MaxValue); Assert.AreEqual(9, unit.ActionPoints);
        unit.RestoreActionPoints(-1); Assert.AreEqual(9, unit.ActionPoints);
    }
    [Test] public void UnaffordableSkillDoesNotSpendActionOrMovementAndRefundCannotFinanceItsCost()
    {
        var unit = new GridUnit("Ally", Vector2Int.zero); unit.BeginTurn();
        var skill = new BattleActionOption { Category = BattleActionCategory.Skill, ActionPointCost = 5, ActionPointRecovery = 9 };
        Assert.IsFalse(skill.CanUse(unit));
        Assert.IsFalse(unit.SpendActionWithActionPoints(skill.EffectiveActionPointCost));
        Assert.IsTrue(unit.ActionAvailable); Assert.AreEqual(3, unit.Movement); Assert.AreEqual(4, unit.ActionPoints);
        skill.ActionPointCost = 4;
        Assert.IsTrue(unit.SpendActionWithActionPoints(skill.EffectiveActionPointCost));
        Assert.AreEqual(0, unit.ActionPoints); Assert.AreEqual(3, unit.Movement);
        Assert.IsFalse(unit.SpendActionWithActionPoints(0));
        skill.CompleteResourceRecovery(unit); Assert.AreEqual(9, unit.ActionPoints);
    }
    [Test] public void SwapPreservesIndividualBalancesWithoutStartGrantAndDeadUnitsDoNotGain()
    {
        var outgoing = new GridUnit("A", Vector2Int.zero); outgoing.BeginTurn();
        var incoming = new GridUnit("B", Vector2Int.one); incoming.InitializeActionPoints(2);
        Assert.IsTrue(outgoing.HandOffTurnTo(incoming));
        Assert.AreEqual(4, outgoing.ActionPoints); Assert.AreEqual(2, incoming.ActionPoints);
        incoming.EndTurn(); incoming.BeginTurn(); Assert.AreEqual(3, incoming.ActionPoints);
        incoming.EndTurn(); incoming.TurnActionPointGain = 0; incoming.BeginTurn(); Assert.AreEqual(3, incoming.ActionPoints);
        incoming.EndTurn(); incoming.Health = 0; incoming.TurnActionPointGain = 1; incoming.BeginTurn(); Assert.AreEqual(3, incoming.ActionPoints);
    }
    [Test] public void SkillsClampCostsAndOnlyBasicDamageAttacksGenerateDefaultRecovery()
    {
        var unit = new GridUnit("Ally", Vector2Int.zero); unit.InitializeActionPoints(0);
        var action = new BattleActionOption { Category = BattleActionCategory.Skill, ActionPointCost = 0 };
        Assert.AreEqual(1, action.EffectiveActionPointCost);
        action.ActionPointCost = 100; Assert.AreEqual(9, action.EffectiveActionPointCost);
        action.CompleteResourceRecovery(unit); Assert.AreEqual(0, unit.ActionPoints);
        action.Category = BattleActionCategory.Item; action.CompleteResourceRecovery(unit); Assert.AreEqual(0, unit.ActionPoints);
        action.Category = BattleActionCategory.Attack; action.Effect = ActionEffect.Push;
        action.CompleteResourceRecovery(unit); Assert.AreEqual(0, unit.ActionPoints);
        action.Effect = ActionEffect.Damage; action.CompleteResourceRecovery(unit); Assert.AreEqual(1, unit.ActionPoints);
    }
}
