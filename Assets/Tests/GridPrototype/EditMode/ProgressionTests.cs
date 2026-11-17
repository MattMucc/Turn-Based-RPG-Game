using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Game.GridPrototype;
using Object = UnityEngine.Object;

public class ProgressionTests
{
    readonly List<GameObject> objects = new List<GameObject>();
    BattleCharacterInfo Member()
    { var go = new GameObject("Party member"); objects.Add(go); return go.AddComponent<BattleCharacterInfo>(); }
    [TearDown] public void Cleanup() { foreach (var obj in objects) if (obj) Object.DestroyImmediate(obj); objects.Clear(); }
    [Test] public void LegacySaveKeepsSpeedCostsAndQuotedDescriptionText()
    {
        var data = new GameSaveData { Scene = "Assets/Scenes/SampleScene.unity", Inventory = new PartyInventory() };
        data.Characters.Add(new SavedCharacter { Allocated = new StatValues { Speed = 7 },
            PersonalActions = new List<BattleActionOption> { new BattleActionOption {
                ActionPointCost = 6, ActionPointRecovery = 2, Description = "Initiative and \"SkillPointCost\": are just text." } } });
        string payload = JsonUtility.ToJson(data).Replace("\"Speed\":", "\"Initiative\":")
            .Replace("\"ActionPointCost\":", "\"SkillPointCost\":").Replace("\"ActionPointRecovery\":", "\"SkillPointRecovery\":");
        string checksum;
        using (var hash = System.Security.Cryptography.SHA256.Create())
            checksum = System.Convert.ToBase64String(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload)));
        var restored = SaveFileStore.Decode(JsonUtility.ToJson(new SaveFileStore.Envelope { Payload = payload, Checksum = checksum }));
        Assert.AreEqual(7, restored.Characters[0].Allocated.Speed);
        Assert.AreEqual(0, restored.Characters[0].Allocated.Soul);
        var action = restored.Characters[0].PersonalActions[0];
        Assert.AreEqual(6, action.ActionPointCost); Assert.AreEqual(2, action.ActionPointRecovery);
        Assert.AreEqual(data.Characters[0].PersonalActions[0].Description, action.Description);
    }
    [Test] public void SoulGrowthAndAllocationReachDamageAndSaveRoundTrip()
    {
        var member = Member(); member.Soul = 4; member.AutomaticGrowth.Soul = 2;
        member.GrantLevel(3, 3); Assert.IsTrue(member.SpendPoint(CharacterStat.Soul));
        var unit = new GridUnit("Test", Vector2Int.zero); member.Configure(unit);
        Assert.AreEqual(9, unit.Soul);
        var saved = new SavedCharacter(); member.CaptureProgress(saved);
        var restored = Member(); restored.Soul = 4; restored.AutomaticGrowth.Soul = 2;
        restored.RestoreProgress(JsonUtility.FromJson<SavedCharacter>(JsonUtility.ToJson(saved)));
        Assert.AreEqual(9, restored.Stat(CharacterStat.Soul));
        Assert.AreEqual(5, restored.UnspentPoints);
    }
    [Test] public void SharedExperienceLevelsActiveAndInactiveMembersWithoutDivision()
    {
        var party = new PartyProgression(); var a = Member(); var b = Member(); b.gameObject.SetActive(false);
        party.Register(a); party.Register(b); party.Members.Add(a);
        Assert.AreEqual(0, party.AwardExperience(50)); Assert.AreEqual(50, party.Progress);
        Assert.AreEqual(1, party.AwardExperience(50)); Assert.AreEqual(2, a.Level); Assert.AreEqual(2, b.Level);
        Assert.AreEqual(3, a.UnspentPoints); Assert.AreEqual(3, b.UnspentPoints); Assert.AreEqual(0, party.Progress);
        party.Register(a); Assert.AreEqual(3, a.UnspentPoints);
    }
    [Test] public void MultipleLevelsRetainRemainderAndLateMembersCatchUp()
    {
        var party = new PartyProgression(); Assert.AreEqual(2, party.AwardExperience(260));
        Assert.AreEqual(3, party.Level); Assert.AreEqual(10, party.Progress);
        var member = Member(); party.Register(member); Assert.AreEqual(3, member.Level); Assert.AreEqual(6, member.UnspentPoints);
    }
    [Test] public void LevelCapAndZeroRewardsCannotRepeatedlyGrantPoints()
    {
        var party = new PartyProgression { LevelCap = 2 }; var member = Member(); party.Register(member);
        party.AwardExperience(10000); party.AwardExperience(10000); party.AwardExperience(0); party.AwardExperience(-50);
        Assert.AreEqual(2, member.Level); Assert.AreEqual(3, member.UnspentPoints); Assert.AreEqual(20000, party.Experience);
    }
    [Test] public void GrowthAndAllocationReachCombatStatsWithoutOverwritingBaseStats()
    {
        var member = Member(); member.Vitality = 0; member.Strength = 5; member.Speed = 10;
        var party = new PartyProgression(); party.Register(member); party.AwardExperience(100);
        Assert.IsTrue(member.SpendPoint(CharacterStat.Strength)); Assert.IsTrue(member.SpendPoint(CharacterStat.Speed));
        Assert.IsTrue(member.SpendPoint(CharacterStat.Soul)); Assert.IsFalse(member.SpendPoint(CharacterStat.Magic)); Assert.AreEqual(5, member.Strength);
        var unit = new GridUnit("Test", Vector2Int.zero); member.Configure(unit);
        Assert.AreEqual(110, unit.MaxHealth); Assert.AreEqual(6, unit.Strength); Assert.AreEqual(11, member.EffectiveSpeed);
    }
    [Test] public void VictoryRewardCanOnlyBeClaimedOncePerGroup()
    {
        var go = new GameObject("Group"); objects.Add(go); go.AddComponent<BoxCollider>();
        var type = System.Type.GetType("BattleGroup, Assembly-CSharp", true); var group = go.AddComponent(type);
        var claim = type.GetMethod("ClaimVictoryExperience"); Assert.AreEqual(50, claim.Invoke(group, null)); Assert.AreEqual(0, claim.Invoke(group, null));
    }
    [Test] public void HoverClassificationPrioritizesDangerAndDistinguishesActiveAlly()
    {
        var member = Member(); var actor = new BattleTarget(member.transform, new GridUnit("Ally", Vector2Int.zero), 0);
        Assert.AreEqual(Color.cyan, actor.HoverColor(1));
        Assert.AreEqual(new Color(.5f, 1f, 0), actor.HoverColor(0));
        var info = member.gameObject.AddComponent<BattleHoverInfo>(); info.Kind = BattleHoverKind.Hostile;
        Assert.AreEqual(Color.red, actor.HoverColor(0));
        info.Kind = BattleHoverKind.Ally;
        var futureAlly = new BattleTarget(member.transform, new GridUnit("Ally", Vector2Int.zero), 2);
        Assert.AreEqual(Color.cyan, futureAlly.HoverColor(0));
        Assert.AreEqual(new Color(.5f, 1f, 0), futureAlly.HoverColor(2));
        info.Kind = BattleHoverKind.Interactable;
        var prop = member.gameObject.AddComponent<BattleDestructible>(); var target = new BattleTarget(prop);
        Assert.AreEqual(Color.yellow, target.HoverColor(0));
        prop.DangerousWhenDestroyed = true; Assert.AreEqual(Color.red, target.HoverColor(0));
    }
    [Test] public void InteractableCanBeHoveredWithoutPretendingItHasHealth()
    {
        var member = Member(); var info = member.gameObject.AddComponent<BattleHoverInfo>(); info.Kind = BattleHoverKind.Interactable;
        var target = new BattleTarget(info);
        Assert.IsTrue(target.Alive); Assert.IsFalse(target.HasHealth); Assert.AreEqual(Color.yellow, target.HoverColor(0));
        info.Kind = BattleHoverKind.Dangerous; Assert.AreEqual(Color.red, target.HoverColor(0));
        member.gameObject.SetActive(false); Assert.IsFalse(target.Alive);
    }
    [Test] public void DestructibleHighlightIsYellowAndSwitchesBackToRedForActors()
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); objects.Add(floor);
        var origin = new Vector3(60000, 0, 60000); floor.transform.position = origin - Vector3.up * .5f;
        floor.transform.localScale = new Vector3(30, 1, 30); Physics.SyncTransforms();
        var terrain = new TerrainBattleGrid(origin, 8, 8, 2, ~0, new Collider[0]);
        var propObject = GameObject.CreatePrimitive(PrimitiveType.Cube); objects.Add(propObject); propObject.transform.position = origin;
        var prop = propObject.AddComponent<BattleDestructible>();
        var root = new GameObject("Highlight"); objects.Add(root); var highlight = root.AddComponent<BattleTargetHighlight>();
        highlight.Initialize(Shader.Find("Game/GridOverlay")); highlight.Show(new BattleTarget(prop), terrain);
        var material = (Material)typeof(BattleTargetHighlight).GetField("outlineMaterial", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(highlight);
        Assert.AreEqual(Color.yellow, material.color);
        highlight.Show(new BattleTarget(propObject.transform, new GridUnit("Actor", Vector2Int.zero), 1), terrain);
        Assert.AreEqual(Color.red, material.color);
    }
}
