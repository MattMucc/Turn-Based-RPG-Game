using NUnit.Framework;
using UnityEngine;
using Game.GridPrototype;

public class SkillPointTests
{
    [Test] public void FirstTurnAddsOneToStartingThreeAndRecoveryCapsAtNine()
    {
        var unit = new GridUnit("Ally", Vector2Int.zero);
        Assert.AreEqual(3, unit.SkillPoints);
        unit.BeginTurn(); Assert.AreEqual(4, unit.SkillPoints);
        unit.RestoreSkillPoints(int.MaxValue); Assert.AreEqual(9, unit.SkillPoints);
        unit.RestoreSkillPoints(-1); Assert.AreEqual(9, unit.SkillPoints);
    }
    [Test] public void UnaffordableSkillDoesNotSpendActionOrMovementAndRefundCannotFinanceItsCost()
    {
        var unit = new GridUnit("Ally", Vector2Int.zero); unit.BeginTurn();
        var skill = new BattleActionOption { Category = BattleActionCategory.Skill, SkillPointCost = 5, SkillPointRecovery = 9 };
        Assert.IsFalse(skill.CanUse(unit));
        Assert.IsFalse(unit.SpendActionWithSkillPoints(skill.EffectiveSkillPointCost));
        Assert.IsTrue(unit.ActionAvailable); Assert.AreEqual(3, unit.Movement); Assert.AreEqual(4, unit.SkillPoints);
        skill.SkillPointCost = 4;
        Assert.IsTrue(unit.SpendActionWithSkillPoints(skill.EffectiveSkillPointCost));
        Assert.AreEqual(0, unit.SkillPoints); Assert.AreEqual(3, unit.Movement);
        Assert.IsFalse(unit.SpendActionWithSkillPoints(0));
        skill.CompleteResourceRecovery(unit); Assert.AreEqual(9, unit.SkillPoints);
    }
    [Test] public void SwapPreservesIndividualBalancesWithoutStartGrantAndDeadUnitsDoNotGain()
    {
        var outgoing = new GridUnit("A", Vector2Int.zero); outgoing.BeginTurn();
        var incoming = new GridUnit("B", Vector2Int.one); incoming.InitializeSkillPoints(2);
        Assert.IsTrue(outgoing.HandOffTurnTo(incoming));
        Assert.AreEqual(4, outgoing.SkillPoints); Assert.AreEqual(2, incoming.SkillPoints);
        incoming.EndTurn(); incoming.BeginTurn(); Assert.AreEqual(3, incoming.SkillPoints);
        incoming.EndTurn(); incoming.TurnSkillPointGain = 0; incoming.BeginTurn(); Assert.AreEqual(3, incoming.SkillPoints);
        incoming.EndTurn(); incoming.Health = 0; incoming.TurnSkillPointGain = 1; incoming.BeginTurn(); Assert.AreEqual(3, incoming.SkillPoints);
    }
    [Test] public void SkillsClampCostsAndOnlyBasicDamageAttacksGenerateDefaultRecovery()
    {
        var unit = new GridUnit("Ally", Vector2Int.zero); unit.InitializeSkillPoints(0);
        var action = new BattleActionOption { Category = BattleActionCategory.Skill, SkillPointCost = 0 };
        Assert.AreEqual(1, action.EffectiveSkillPointCost);
        action.SkillPointCost = 100; Assert.AreEqual(9, action.EffectiveSkillPointCost);
        action.CompleteResourceRecovery(unit); Assert.AreEqual(0, unit.SkillPoints);
        action.Category = BattleActionCategory.Item; action.CompleteResourceRecovery(unit); Assert.AreEqual(0, unit.SkillPoints);
        action.Category = BattleActionCategory.Attack; action.Effect = ActionEffect.Push;
        action.CompleteResourceRecovery(unit); Assert.AreEqual(0, unit.SkillPoints);
        action.Effect = ActionEffect.Damage; action.CompleteResourceRecovery(unit); Assert.AreEqual(1, unit.SkillPoints);
    }
}
