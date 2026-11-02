using NUnit.Framework;
using UnityEngine;
using Game.GridPrototype;

public class CombatEffectsTests
{
    GridUnit Unit() => new GridUnit("Test", Vector2Int.zero);
    [Test] public void BurningAndRegenerationTickOnlyAtStartAndConsumeOneStack()
    {
        var unit = Unit(); unit.Health = 60; unit.BurnTickDamage = 8; unit.RegenerationTickHealing = 3;
        unit.BeginTurn(); unit.ApplyStatus(CombatStatus.Burning, 3); unit.ApplyStatus(CombatStatus.Regeneration, 2);
        unit.EndTurn(); Assert.AreEqual(60, unit.Health);
        unit.BeginTurn(); Assert.AreEqual(55, unit.Health);
        Assert.AreEqual(2, unit.Stacks(CombatStatus.Burning)); Assert.AreEqual(1, unit.Stacks(CombatStatus.Regeneration));
        unit.EndTurn(); unit.BeginTurn(); Assert.AreEqual(50, unit.Health);
        Assert.AreEqual(0, unit.Stacks(CombatStatus.Regeneration));
    }
    [Test] public void EachLandedHitRollsIndependentlyAndAddsItsWholeStackAmountUpToTen()
    {
        var source = Unit(); var target = Unit();
        var action = new BattleActionOption { Damage = 0, HitChance = 70, StatusChance = 50, InflictedStatus = CombatStatus.Burning, StatusStacks = 2 };
        action.ApplyHit(source, target, 0, .1f, .1f, out _);
        action.ApplyHit(source, target, 0, .9f, .1f, out _); // missed hit never applies status
        action.ApplyHit(source, target, 0, .1f, .1f, out _);
        Assert.AreEqual(4, target.Stacks(CombatStatus.Burning));
        action.ApplyHit(source, target, 0, .1f, .9f, out _); Assert.AreEqual(4, target.Stacks(CombatStatus.Burning));
        target.ApplyStatus(CombatStatus.Burning, 9); Assert.AreEqual(10, target.Stacks(CombatStatus.Burning));
        target.BeginTurn(); Assert.AreEqual(95, target.Health); Assert.AreEqual(9, target.Stacks(CombatStatus.Burning));
    }
    [Test] public void BarrierAbsorbsAndSpillsIntoHealthWithoutChangingMaxHealth()
    {
        var unit = Unit(); unit.AddBarrier(30); Assert.AreEqual(0, unit.TakeDamage(20));
        Assert.AreEqual(10, unit.Barrier); Assert.AreEqual(30, unit.BarrierMaximum);
        Assert.AreEqual(15, unit.TakeDamage(25)); Assert.AreEqual(85, unit.Health); Assert.AreEqual(100, unit.MaxHealth);
        Assert.AreEqual(0, unit.Barrier); unit.AddBarrier(12); Assert.AreEqual(12, unit.BarrierMaximum);
    }
    [Test] public void ParalysisPreventsBlockAndReactionsButNotActionOrMovement()
    {
        var unit = Unit(); unit.BeginTurn(); unit.Blocking = true;
        unit.ApplyStatus(CombatStatus.Paralyzed, 1);
        Assert.IsFalse(unit.Blocking); Assert.IsFalse(unit.CanBlock); Assert.IsFalse(unit.CanReact);
        Assert.IsTrue(unit.ActionAvailable); Assert.IsTrue(unit.SpendMovement(1));
        unit.EndTurn(); Assert.IsFalse(unit.CanReact); // applied this turn survives
        unit.BeginTurn(); Assert.IsFalse(unit.CanBlock); unit.EndTurn(); Assert.IsTrue(unit.CanBlock);
    }
    [Test] public void LethalTickDoesNotGrantMovementOrResurrectThroughRegeneration()
    {
        var unit = Unit(); unit.Health = 3;
        unit.ApplyStatus(CombatStatus.Burning, 1); unit.ApplyStatus(CombatStatus.Regeneration, 1);
        unit.BeginTurn(); Assert.AreEqual(0, unit.Health); Assert.AreEqual(0, unit.Movement);
        Assert.IsFalse(unit.ActionAvailable); Assert.IsFalse(unit.TurnOpen);
    }
    [Test] public void PhysicalAndMagicalDefenseUseMatchingTypeIndependentOfScaling()
    {
        var source = Unit(); source.Strength = 10; var target = Unit(); target.PhysicalDefense = 100;
        Assert.AreEqual(15, CombatMath.Damage(source, target, 20, DamageKind.Physical, ScalingStat.Strength, 1));
        Assert.AreEqual(30, CombatMath.Damage(source, target, 20, DamageKind.Magical, ScalingStat.Strength, 1));
        target.Blocking = true; Assert.AreEqual(7, CombatMath.Damage(source, target, 20, DamageKind.Physical, ScalingStat.Strength, 1));
    }
    [Test] public void PushRequiresStrictlyHigherStrength()
    { Assert.IsFalse(CombatMath.CanPush(5, 5)); Assert.IsFalse(CombatMath.CanPush(4, 5)); Assert.IsTrue(CombatMath.CanPush(6, 5)); }
}
