using Game.GridPrototype;
using NUnit.Framework;
using UnityEngine;

public class MagicPointTests
{
    [Test] public void GrowthStartsAt24AndUsesConfigurableLevelAndAdditionalMagic()
    {
        var go = new GameObject("MP growth");
        try
        {
            var info = go.AddComponent<BattleCharacterInfo>(); info.Level = 1; info.Magic = 12;
            Assert.AreEqual(24, info.EffectiveMaxMagicPoints);
            info.Level = 3; info.AllocatedPoints.Magic = 3;
            Assert.AreEqual(34, info.EffectiveMaxMagicPoints);
            info.MagicPointsPerLevel = 3; info.MagicPointsPerMagic = 4; info.FocusMagicRecovery = 7;
            var unit = new GridUnit("A", Vector2Int.zero); info.Configure(unit);
            Assert.AreEqual(42, unit.MaxMagicPoints); Assert.AreEqual(7, unit.FocusRecovery);
            unit.BeginTurn(); Assert.IsTrue(unit.SpendActionWithSkillPoints(0, 8)); info.StoreMagicPoints(unit);
            var next = new GridUnit("A", Vector2Int.zero); info.Configure(next);
            Assert.AreEqual(34, next.MagicPoints);
            info.Level = 4; info.Configure(next);
            Assert.AreEqual(45, next.MaxMagicPoints); Assert.AreEqual(34, next.MagicPoints, "Growth must not refill spent MP.");
        }
        finally { Object.DestroyImmediate(go); }
    }
    [Test] public void FocusRestoresEightAndUsesOnlyActionWithoutRefillingOnTurnStart()
    {
        var unit = new GridUnit("A", Vector2Int.zero); unit.InitializeMagicPoints(24, 5); unit.BeginTurn();
        Assert.AreEqual(5, unit.MagicPoints);
        Assert.IsTrue(unit.Focus()); Assert.AreEqual(13, unit.MagicPoints);
        Assert.AreEqual(3, unit.Movement); Assert.AreEqual(4, unit.SkillPoints); Assert.IsFalse(unit.Focus());
        unit.EndTurn(); unit.BeginTurn(); unit.RestoreMagicPoints(int.MaxValue);
        Assert.AreEqual(24, unit.MagicPoints); Assert.IsFalse(unit.Focus()); Assert.IsTrue(unit.ActionAvailable);
        unit.InitializeMagicPoints(24, 0); unit.Health = 0;
        Assert.IsFalse(unit.Focus()); Assert.AreEqual(0, unit.RestoreMagicPoints(8));
    }
    [Test] public void ResourcePaymentIsAtomicAndSwapDoesNotRestoreMP()
    {
        var a = new GridUnit("A", Vector2Int.zero); a.InitializeMagicPoints(24, 3); a.BeginTurn();
        Assert.IsFalse(a.SpendActionWithSkillPoints(2, 4)); Assert.AreEqual(4, a.SkillPoints); Assert.AreEqual(3, a.MagicPoints); Assert.IsTrue(a.ActionAvailable);
        var b = new GridUnit("B", Vector2Int.one); b.InitializeMagicPoints(24, 0);
        Assert.IsTrue(a.HandOffTurnTo(b)); Assert.AreEqual(0, b.MagicPoints); Assert.AreEqual(3, a.MagicPoints);
        b.EndTurn(); b.BeginTurn(); Assert.AreEqual(0, b.MagicPoints);
    }
    [Test] public void SharedManaPotionRestoresEightClampsAndDoesNotHealOrRevive()
    {
        var inventory = new PartyInventory(); Assert.AreEqual(3, inventory.Count("mana-potion"));
        var potion = inventory.Items.Find(x => x.ItemId == "mana-potion");
        var unit = new GridUnit("A", Vector2Int.zero) { Health = 40 }; unit.InitializeMagicPoints(24, 10);
        potion.ApplyHit(unit, unit, 0, 0, 0, out int health);
        Assert.AreEqual(18, unit.MagicPoints); Assert.AreEqual(40, unit.Health); Assert.AreEqual(0, health);
        potion.ApplyHit(unit, unit, 0, 0, 0, out health); Assert.AreEqual(24, unit.MagicPoints);
        unit.InitializeMagicPoints(24, 0); unit.Health = 0;
        Assert.IsFalse(potion.ApplyHit(unit, unit, 0, 0, 0, out health)); Assert.AreEqual(0, unit.MagicPoints);
    }
}
