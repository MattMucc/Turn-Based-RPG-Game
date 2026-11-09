using System.Collections.Generic;
using Game.GridPrototype;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

public class SharedActionStatusTests
{
    readonly List<Object> owned=new();
    GameObject Make(){var o=new GameObject("Action/status test");owned.Add(o);return o;}
    [TearDown] public void Cleanup(){for(int i=owned.Count-1;i>=0;i--)if(owned[i])Object.DestroyImmediate(owned[i]);owned.Clear();}
    SharedBattleActionLibrary Library()
    {
        var lib=ScriptableObject.CreateInstance<SharedBattleActionLibrary>();owned.Add(lib);
        lib.Actions.Add(new BattleActionOption{Name="Shared",Damage=10,Scaling=ScalingStat.Strength,StatScaling=2,QteSequence=new[]{QteButton.South}});return lib;
    }
    BattleActionLoadout Loadout(SharedBattleActionLibrary lib)
    {var l=Make().AddComponent<BattleActionLoadout>();l.PersonalOptions.Clear();l.SharedLibrary=lib;return l;}
    [Test] public void SharedDefinitionsUseEachCasterStatsWithoutSharingFavoritesOrArrays()
    {
        var lib=Library();var a=Loadout(lib);var b=Loadout(lib);
        var one=a.Options[0];var two=b.Options[0];Assert.AreNotSame(one,two);Assert.AreNotSame(one.QteSequence,two.QteSequence);
        a.AssignFavorite(one,3);Assert.AreNotEqual(3,two.FavoriteSlot);Assert.AreEqual(-1,lib.Actions[0].FavoriteSlot);
        var source=new GridUnit("Caster",Vector2Int.zero){Strength=5};var victim=new GridUnit("Target",Vector2Int.zero){PhysicalDefense=100};
        Assert.IsTrue(one.ApplyHit(source,victim,one.Damage,0,0,out int low));Assert.AreEqual(10,low);
        source.Strength=15;Assert.IsTrue(two.ApplyHit(source,victim,two.Damage,0,0,out int high));Assert.AreEqual(20,high);
    }
    [Test] public void LibraryRemovalDoesNotGetReinsertedAndPersonalItemsStayIndependent()
    {
        var lib=Library();var a=Loadout(lib);var b=Loadout(lib);
        var item=new BattleActionOption{Name="Potion",Category=BattleActionCategory.Item,Quantity=3};a.PersonalOptions.Add(item);
        Assert.AreEqual(2,a.Options.Count);Assert.AreEqual(1,b.Options.Count);
        a.Options[1].Quantity--;Assert.AreEqual(2,item.Quantity);
        lib.Actions.Clear();lib.NotifyChanged();a.EnsureUniversalAttacks();b.EnsureUniversalAttacks();
        Assert.AreEqual(1,a.Options.Count);Assert.AreSame(item,a.Options[0]);Assert.IsEmpty(b.Options);
        lib.Actions.Add(new BattleActionOption{Name="New common skill",Category=BattleActionCategory.Skill});lib.NotifyChanged();
        Assert.AreEqual("New common skill",a.Options[0].Name);Assert.AreEqual("New common skill",b.Options[0].Name);
    }
    [Test] public void StatusPresentationUsesTicksAndLiveAmountsInsteadOfInventingTurnDurations()
    {
        var unit=new GridUnit("Target",Vector2Int.zero){BurnTickDamage=9,RegenerationTickHealing=7};
        unit.ApplyStatus(CombatStatus.Burning,3);unit.ApplyStatus(CombatStatus.Regeneration,2);unit.AddBarrier(30);
        var badges=StatusPresentation.For(unit);Assert.AreEqual(3,badges.Count);
        Assert.AreEqual("3",badges[0].Count);StringAssert.Contains("9 damage",badges[0].Tooltip);
        StringAssert.Contains("7 health",badges[1].Tooltip);StringAssert.Contains("not turns",badges[2].Tooltip);
        unit.BeginTurn(0);badges=StatusPresentation.For(unit);Assert.AreEqual("2",badges[0].Count);Assert.AreEqual("21",badges[2].Count);
    }
    [Test] public void FlammableObjectsTickBurningOnlyAndCanBeExtinguished()
    {
        var prop=Make().AddComponent<BattleDestructible>();var effects=prop.gameObject.AddComponent<BattleObjectStatus>();
        effects.BurningDamagePerRound=6;effects.ApplyStatus(CombatStatus.Burning,3);
        effects.ApplyStatus(CombatStatus.Regeneration,4);effects.ApplyStatus(CombatStatus.Paralyzed,4);
        Assert.AreEqual(0,effects.State.Stacks(CombatStatus.Regeneration));Assert.AreEqual(0,effects.State.Stacks(CombatStatus.Paralyzed));
        Assert.AreEqual(50,prop.Health);effects.TickRound();Assert.AreEqual(44,prop.Health);Assert.AreEqual(2,effects.State.Stacks(CombatStatus.Burning));
        StringAssert.Contains("battle round",StatusPresentation.For(effects.State,true)[0].Tooltip);
        effects.Extinguish();effects.TickRound();Assert.AreEqual(44,prop.Health);Assert.IsEmpty(StatusPresentation.For(effects.State,true));
        effects.Flammable=false;effects.ApplyStatus(CombatStatus.Burning,3);Assert.AreEqual(0,effects.State.Stacks(CombatStatus.Burning));
    }
}
