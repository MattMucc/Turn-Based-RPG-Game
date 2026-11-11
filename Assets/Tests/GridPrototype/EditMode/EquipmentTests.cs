using System.Collections.Generic;
using System.Linq;
using Game.GridPrototype;
using NUnit.Framework;
using UnityEngine;

public class EquipmentTests
{
    readonly List<Object> cleanup=new();
    EquipmentDefinition Item(string id,WeaponType type=WeaponType.Shortsword){var d=ScriptableObject.CreateInstance<EquipmentDefinition>();cleanup.Add(d);d.Id=id;d.WeaponType=type;d.Attacks.Add(new BattleActionOption{Name=id+" slash",Damage=10,Scaling=ScalingStat.Strength,StatScaling=1});return d;}
    CharacterEquipment Character(PartyEquipment inventory){var go=new GameObject("Equipment test");cleanup.Add(go);go.AddComponent<BattleCharacterInfo>();go.AddComponent<BattleActionLoadout>();var c=go.AddComponent<CharacterEquipment>();c.AllowedWeaponTypes=new[]{WeaponType.Shortsword,WeaponType.Bow};c.Bind(inventory);return c;}
    [TearDown]public void Clear(){for(int i=cleanup.Count-1;i>=0;i--)if(cleanup[i])Object.DestroyImmediate(cleanup[i]);cleanup.Clear();}
    [Test]public void UpgradesAggregateCostsAndChangeOnlyOneCopyWithoutChangingMastery()
    {
        var sword=Item("sword");sword.Upgrades.Add(new EquipmentUpgradeStep{Materials=new(){new(){MaterialId="iron",Quantity=2},new(){MaterialId="iron",Quantity=3}},AddedStats=new StatValues{Strength=4}});
        var one=new OwnedEquipment{InstanceId="one",Definition=sword,MasteryExperience=12};var two=new OwnedEquipment{InstanceId="two",Definition=sword};
        var inventory=new PartyEquipment{Coins=100,Owned=new(){one,two},Materials=new(){new(){Id="iron",Quantity=4}}};
        Assert.IsFalse(EquipmentUpgrades.TryApply(inventory,one,0,out _));Assert.AreEqual(4,inventory.Materials[0].Quantity);Assert.AreEqual(0,one.UpgradeLevel);
        inventory.Materials[0].Quantity=5;Assert.IsTrue(EquipmentUpgrades.TryApply(inventory,one,0,out _));
        Assert.AreEqual(0,inventory.Materials[0].Quantity);Assert.AreEqual(4,EquipmentUpgrades.Bonus(one,CharacterStat.Strength));Assert.AreEqual(0,EquipmentUpgrades.Bonus(two,CharacterStat.Strength));Assert.AreEqual(12,one.MasteryExperience);
        inventory.Materials[0].Quantity=5;Assert.IsFalse(EquipmentUpgrades.TryApply(inventory,one,0,out _));Assert.IsFalse(EquipmentUpgrades.TryApply(inventory,one,1,out _));Assert.AreEqual(5,inventory.Materials[0].Quantity);
        var profile=Character(inventory);profile.RestoreSlots(new[]{"one",null,null,null});Assert.AreEqual(4,profile.GetComponent<BattleCharacterInfo>().Stat(CharacterStat.Strength));
        profile.ApplySlot(EquipmentSlot.Weapon,"two");Assert.AreEqual(0,profile.GetComponent<BattleCharacterInfo>().Stat(CharacterStat.Strength));
    }
    [Test]public void AccessoriesRequireFeeAndInvalidRecipesCannotSpendMaterials()
    {
        var item=Item("accessory");item.Kind=EquipmentKind.Accessory;item.Upgrades.Add(new EquipmentUpgradeStep{Materials=new(){new(){MaterialId="iron",Quantity=1}}});
        var owned=new OwnedEquipment{Definition=item};var inventory=new PartyEquipment{Owned=new(){owned},Materials=new(){new(){Id="iron",Quantity=9}}};
        Assert.IsFalse(EquipmentUpgrades.TryApply(inventory,owned,0,out _));Assert.AreEqual(9,inventory.Materials[0].Quantity);inventory.Coins=25;Assert.IsTrue(EquipmentUpgrades.TryApply(inventory,owned,0,out _));Assert.AreEqual(0,inventory.Coins);Assert.AreEqual(8,inventory.Materials[0].Quantity);owned.UpgradeLevel=0;inventory.Coins=25;item.Kind=EquipmentKind.Armor;item.Upgrades[0].Materials[0].Quantity=-1;
        Assert.IsFalse(EquipmentUpgrades.TryApply(inventory,owned,0,out _));Assert.AreEqual(8,inventory.Materials[0].Quantity);Assert.AreEqual(25,inventory.Coins);
    }
    [Test]public void MasteryTravelsWithCopyUnlocksWithoutRefillingShortcutsAndLeavesDuplicatesUntrained()
    {
        var sword=Item("sword");sword.Mastery.Add(new WeaponMasteryMilestone{RequiredExperience=1,Attacks=new(){new BattleActionOption{Name="Master cut"}}});
        var trained=new OwnedEquipment{InstanceId="one",Definition=sword};var duplicate=new OwnedEquipment{InstanceId="two",Definition=sword};
        var inventory=new PartyEquipment{Owned=new(){trained,duplicate}};var first=Character(inventory);var second=Character(inventory);
        first.RestoreSlots(new[]{"one",null,null,null});second.RestoreSlots(new[]{"two",null,null,null});
        var loadout=first.GetComponent<BattleActionLoadout>();var attack=loadout.Options.Single(a=>a.Name=="sword slash");loadout.AssignFavorite(attack,0);
        Assert.IsFalse(loadout.Options.Any(a=>a.Name=="Master cut"));var use=new WeaponMasteryUse(trained,attack);use.Record(true,true,false);Assert.AreEqual(1,use.Complete(null));
        Assert.IsTrue(loadout.Options.Any(a=>a.Name=="Master cut"));Assert.AreEqual("sword slash",loadout.Favorite(BattleActionCategory.Attack,0).Name);
        Assert.AreEqual(-1,loadout.Options.Single(a=>a.Name=="Master cut").FavoriteSlot);
        Assert.IsFalse(second.GetComponent<BattleActionLoadout>().Options.Any(a=>a.Name=="Master cut"));
        first.ApplySlot(EquipmentSlot.Weapon,null);second.ApplySlot(EquipmentSlot.Weapon,"one");
        Assert.IsTrue(second.GetComponent<BattleActionLoadout>().Options.Any(a=>a.Name=="Master cut"));Assert.AreEqual(0,duplicate.MasteryExperience);
    }
    [Test]public void MasteryCountsOneSuccessfulHostileActionAndSnapshotsFinisherBeforeUnlock()
    {
        var sword=Item("sword");sword.Mastery.Add(new WeaponMasteryMilestone{RequiredExperience=1,FinisherSkillPoints=1});sword.Mastery.Add(new WeaponMasteryMilestone{RequiredExperience=3});
        var owned=new OwnedEquipment{Definition=sword};var attack=sword.Attacks[0];attack.EquipmentActionId="sword:"+attack.Name;
        var source=new GridUnit("source",Vector2Int.zero);source.InitializeSkillPoints(0);
        var miss=new WeaponMasteryUse(owned,attack);miss.Record(false,true,false);miss.Record(true,false,true);Assert.AreEqual(0,miss.Complete(source));
        var hit=new WeaponMasteryUse(owned,attack);for(int i=0;i<3;i++)hit.Record(true,true,true);
        Assert.AreEqual(1,hit.Complete(source));Assert.AreEqual(0,hit.Complete(source));Assert.AreEqual(0,source.SkillPoints);
        var next=new WeaponMasteryUse(owned,attack);next.Record(true,true,true);next.Complete(source);Assert.AreEqual(1,source.SkillPoints);
        attack.Category=BattleActionCategory.Skill;var skill=new WeaponMasteryUse(owned,attack);skill.Record(true,true,true);Assert.AreEqual(0,skill.Complete(source));
        Assert.AreEqual(2,owned.MasteryExperience);
    }
    [Test]public void WeaponChangesReplaceAttacksAndClearOnlyRemovedShortcuts()
    {
        var sword=Item("sword");var bow=Item("bow",WeaponType.Bow);
        var inventory=new PartyEquipment();inventory.Owned.Add(new OwnedEquipment{InstanceId="s",Definition=sword});inventory.Owned.Add(new OwnedEquipment{InstanceId="b",Definition=bow});
        var c=Character(inventory);c.RestoreSlots(new[]{"s",null,null,null});var loadout=c.GetComponent<BattleActionLoadout>();loadout.InitializeFavorites();
        var attack=loadout.Options.Single(a=>a.Name=="sword slash");loadout.AssignFavorite(attack,0);
        var dash=loadout.Options.Single(a=>a.Effect==ActionEffect.Dash);loadout.AssignFavorite(dash,2);
        c.ApplySlot(EquipmentSlot.Weapon,"b");
        Assert.IsFalse(loadout.Options.Any(a=>a.Name=="sword slash"||a.Name=="Strike"||a.Name=="Piercing Attack"));
        Assert.IsTrue(loadout.Options.Any(a=>a.Name=="bow slash"));Assert.IsNull(loadout.Favorite(BattleActionCategory.Attack,0));
        Assert.AreSame(dash,loadout.Favorite(BattleActionCategory.Skill,2));Assert.IsTrue(loadout.Options.Any(a=>a.Effect==ActionEffect.Push));
        c.ApplySlot(EquipmentSlot.Weapon,null);Assert.IsTrue(loadout.Options.Any(a=>a.Name=="Punch"));Assert.IsFalse(loadout.Options.Any(a=>a.Name=="bow slash"));
        c.ApplySlot(EquipmentSlot.Weapon,"s");Assert.AreEqual(-1,loadout.Options.Single(a=>a.Name=="sword slash").FavoriteSlot);
    }
    [Test]public void BonusesAffectCombatAndEquipmentNeverHealsOrRevives()
    {
        var item=Item("charm");item.Kind=EquipmentKind.Accessory;item.Bonuses=new StatValues{Vitality=2,Magic=3,Strength=4,PhysicalDefense=5,MagicalDefense=6,Initiative=7};
        var inv=new PartyEquipment();inv.Owned.Add(new OwnedEquipment{InstanceId="c",Definition=item});var profile=Character(inv);var info=profile.GetComponent<BattleCharacterInfo>();
        info.Level=1;info.Vitality=info.Strength=info.Magic=info.PhysicalDefense=info.MagicalDefense=info.Initiative=0;
        profile.ApplySlot(EquipmentSlot.AccessoryOne,"c");var unit=new GridUnit("test",Vector2Int.zero);info.Configure(unit);
        Assert.AreEqual(120,unit.MaxHealth);Assert.AreEqual(100,info.StoredHealth);Assert.AreEqual(30,unit.MaxMagicPoints);Assert.AreEqual(24,info.StoredMagicPoints);
        Assert.AreEqual(4,unit.Strength);Assert.AreEqual(3,unit.Magic);Assert.AreEqual(5,unit.PhysicalDefense);Assert.AreEqual(6,unit.MagicalDefense);Assert.AreEqual(7,info.EffectiveInitiative);
        Assert.AreEqual(14,new BattleActionOption{Damage=10,Scaling=ScalingStat.Strength,StatScaling=1}.CalculateDamage(unit,null,10));
        info.HealOutsideCombat(100);info.RestoreMagicOutsideCombat(100);profile.ApplySlot(EquipmentSlot.AccessoryOne,null);
        Assert.AreEqual(100,info.StoredHealth);Assert.AreEqual(24,info.StoredMagicPoints);
        unit.Health=0;info.StoreHealth(unit);profile.ApplySlot(EquipmentSlot.AccessoryOne,"c");Assert.AreEqual(0,info.StoredHealth);
    }
    [Test]public void WeaponRestrictionsAndSlotKindsAreIndependentOfStats()
    {
        var profile=Character(new PartyEquipment());var sword=Item("sword");var heavy=Item("heavy",WeaponType.Greatsword);
        Assert.IsTrue(profile.Accepts(EquipmentSlot.Weapon,sword));Assert.IsFalse(profile.Accepts(EquipmentSlot.Weapon,heavy));Assert.IsFalse(profile.Accepts(EquipmentSlot.Armor,sword));
        heavy.Kind=EquipmentKind.Armor;Assert.IsTrue(profile.Accepts(EquipmentSlot.Armor,heavy));Assert.IsFalse(profile.Accepts(EquipmentSlot.AccessoryTwo,heavy));
        heavy.Kind=EquipmentKind.Accessory;Assert.IsTrue(profile.Accepts(EquipmentSlot.AccessoryOne,heavy));Assert.IsTrue(profile.Accepts(EquipmentSlot.AccessoryTwo,heavy));
    }
}
