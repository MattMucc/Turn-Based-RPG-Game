using System.Collections.Generic;
using Game.GridPrototype;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class PartyInventoryTests
{
    readonly List<GameObject> owned = new();
    BattleActionLoadout Member(PartyInventory inventory)
    {
        var obj = new GameObject("Inventory test member"); owned.Add(obj);
        var loadout = obj.AddComponent<BattleActionLoadout>(); loadout.BindInventory(inventory); return loadout;
    }
    [TearDown] public void Cleanup() { foreach(var obj in owned) Object.DestroyImmediate(obj); owned.Clear(); }
    [Test] public void AllMembersAndReservesUseOneStockButKeepTheirOwnShortcuts()
    {
        var inventory = new PartyInventory(); var a = Member(inventory); var b = Member(inventory);
        var one = a.Options.Find(x=>x.Category==BattleActionCategory.Item);
        var two = b.Options.Find(x=>x.Category==BattleActionCategory.Item);
        Assert.AreNotSame(one,two); a.AssignFavorite(one,3); b.InitializeFavorites(); Assert.AreEqual(0,two.FavoriteSlot);
        one.Quantity--;Assert.AreEqual(2,two.Quantity);Assert.AreEqual(2,inventory.Count("health-potion"));
        var reserve=Member(inventory);Assert.AreEqual(2,reserve.Options.Find(x=>x.Category==BattleActionCategory.Item).Quantity);
        two.Quantity=0;Assert.IsFalse(one.Available);Assert.IsFalse(two.Available);
        inventory.Add(PartyInventory.HealthPotion(),4);Assert.AreEqual(4,one.Quantity);Assert.IsTrue(two.Available);Assert.AreEqual(3,one.FavoriteSlot);
    }
    [Test] public void RebindingForLaterEncounterDoesNotRestockAndInventorySerializes()
    {
        var inventory=new PartyInventory();var member=Member(inventory);
        member.Options.Find(x=>x.Category==BattleActionCategory.Item).Quantity--;
        member.BindInventory(null);member.BindInventory(inventory);
        Assert.AreEqual(2,member.Options.Find(x=>x.Category==BattleActionCategory.Item).Quantity);
        var copy=JsonUtility.FromJson<PartyInventory>(JsonUtility.ToJson(inventory));Assert.AreEqual(2,copy.Count("health-potion"));
        inventory.Items.Clear();inventory.NotifyChanged();Assert.IsFalse(member.Options.Exists(x=>x.Category==BattleActionCategory.Item));
    }
    [Test] public void PotionHealsUpToMaximumWithoutRevivingOrUsingDamageStats()
    {
        var potion=PartyInventory.HealthPotion();var source=new GridUnit("Source",Vector2Int.zero){Strength=999};
        var target=new GridUnit("Target",Vector2Int.zero){Health=20,PhysicalDefense=999,MagicalDefense=999};target.AddBarrier(10);
        potion.ApplyHit(source,target,0,0,0,out int delta);Assert.AreEqual(70,target.Health);Assert.AreEqual(-50,delta);Assert.AreEqual(10,target.Barrier);
        potion.ApplyHit(source,target,0,0,0,out delta);Assert.AreEqual(100,target.Health);Assert.AreEqual(-30,delta);
        target.Health=0;Assert.IsFalse(potion.ApplyHit(source,target,0,0,0,out delta));Assert.AreEqual(0,target.Health);
    }
    [Test] public void PlayablePrefabsStartWithoutSpellsOrPersonalItems()
    {
        Assert.IsFalse(Member(new PartyInventory()).PersonalOptions.Exists(x=>x.Category==BattleActionCategory.Magic || x.Category==BattleActionCategory.Item));
        foreach(var guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/Player"})) {
            var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));var loadout=prefab.GetComponent<BattleActionLoadout>();if(!loadout)continue;
            Assert.IsFalse(loadout.PersonalOptions.Exists(x=>x.Category==BattleActionCategory.Magic || x.Category==BattleActionCategory.Item),prefab.name);
            Assert.IsFalse(loadout.Options.Exists(x=>x.Category==BattleActionCategory.Magic),prefab.name);
        }
    }
}
