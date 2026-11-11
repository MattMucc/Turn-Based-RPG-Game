using System.Linq;
using Game.GridPrototype;
using NUnit.Framework;
using UnityEngine;

public class SpellMasteryTests
{
    GameObject first, second;
    BattleActionLoadout a, b;
    [SetUp] public void Setup()
    {
        first=new GameObject("Learner");second=new GameObject("Other learner");
        first.AddComponent<BattleCharacterInfo>();second.AddComponent<BattleCharacterInfo>();
        a=first.AddComponent<BattleActionLoadout>();b=second.AddComponent<BattleActionLoadout>();
    }
    [TearDown] public void Cleanup(){Object.DestroyImmediate(first);Object.DestroyImmediate(second);}
    BattleActionOption Fire()=>a.Options.First(s=>s.SpellId=="fireball");
    [Test] public void BooksConsumeOnceForOneLearnerAndNeverAppearInCombat()
    {
        var book=new BattleActionOption{ItemId="book",LearnSpellId="fireball",Category=BattleActionCategory.Item,Quantity=2};
        var inventory=new PartyInventory();inventory.Items.Add(book);a.BindInventory(inventory);
        Assert.IsTrue(inventory.TryLearn(book,first.GetComponent<BattleCharacterInfo>()));
        Assert.IsTrue(a.KnowsSpell("fireball"));Assert.IsFalse(b.KnowsSpell("fireball"));Assert.AreEqual(1,book.Quantity);
        Assert.IsFalse(inventory.TryLearn(book,first.GetComponent<BattleCharacterInfo>()));Assert.AreEqual(1,book.Quantity);
        Assert.IsFalse(a.Options.Any(s=>s.ItemId=="book"));Assert.AreEqual(1,a.Options.Count(s=>s.SpellId=="fireball"));
        Assert.IsTrue(inventory.TryLearn(book,second.GetComponent<BattleCharacterInfo>()));Assert.AreEqual(0,book.Quantity);
        Assert.IsFalse(a.LearnSpell("missing-spell"));
    }
    [Test] public void MasteryAwardsOncePerSuccessfulCastAndKeepsLowerTiersAndShortcuts()
    {
        a.LearnSpell("fireball");b.LearnSpell("fireball");a.AssignFavorite(Fire(),3);
        for(int cast=0;cast<10;cast++) {
            var use=new SpellMasteryUse(a,Fire());
            use.Record(false,true,0);use.Record(true,false,15);use.Record(true,true,20);use.Record(true,true,20);
            Assert.AreEqual(1,use.Complete());Assert.AreEqual(0,use.Complete());
        }
        Assert.AreEqual(10,a.LearnedSpells[0].Experience);Assert.AreEqual(0,b.LearnedSpells[0].Experience);
        CollectionAssert.AreEqual(new[]{"Fireball I","Fireball II"},a.Options.Where(s=>s.SpellId=="fireball").Select(s=>s.Name));
        Assert.AreEqual("Fireball I",a.Favorite(BattleActionCategory.Magic,3).Name);
        for(int i=0;i<30;i++)a.AwardSpellExperience("fireball");
        Assert.AreEqual(25,a.LearnedSpells[0].Experience);Assert.AreEqual(3,a.Options.Count(s=>s.SpellId=="fireball"));
    }
    [Test] public void MissesFriendlyFireEmptyCastsAndUnlearnedActionsDoNotAwardMastery()
    {
        a.LearnSpell("fireball");
        var missed=new SpellMasteryUse(a,Fire());missed.Record(false,true,0);Assert.AreEqual(0,missed.Complete());
        var friendly=new SpellMasteryUse(a,Fire());friendly.Record(true,false,20);Assert.AreEqual(0,friendly.Complete());
        Assert.AreEqual(0,new SpellMasteryUse(a,Fire()).Complete());
        var stolen=new SpellMasteryUse(b,Fire());stolen.Record(true,true,20);Assert.AreEqual(0,stolen.Complete());
        Assert.AreEqual(0,a.LearnedSpells[0].Experience);
    }
    [Test] public void HealingMustRestoreHealthAndCannotRevive()
    {
        a.LearnSpell("mend");var heal=a.Options.Single(s=>s.SpellId=="mend");
        var source=new GridUnit("Source",Vector2Int.zero);var target=new GridUnit("Target",Vector2Int.one);
        target.Health=target.MaxHealth;Assert.IsTrue(heal.ApplyHit(source,target,0,0,0,out int full));Assert.AreEqual(0,full);
        target.Health=50;Assert.IsTrue(heal.ApplyHit(source,target,0,0,0,out int restored));Assert.Less(restored,0);
        target.Health=0;Assert.IsFalse(heal.ApplyHit(source,target,0,0,0,out _));Assert.AreEqual(0,target.Health);
        Assert.AreEqual(1,a.Options.Count(s=>s.SpellId=="mend"));Assert.AreEqual("Mend",heal.Name);
    }
}
