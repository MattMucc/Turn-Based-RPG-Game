using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Game.GridPrototype;
using Object = UnityEngine.Object;

public class PartyBattleTests
{
    readonly List<GameObject> objects = new List<GameObject>();
    InSceneGridBattle battle;
    bool? outcome;
    GameObject Make(string name) { var obj = new GameObject(name); objects.Add(obj); return obj; }
    [SetUp] public void Setup()
    {
        var origin = new Vector3(80000, 0, 80000);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); objects.Add(floor);
        floor.transform.position = origin - Vector3.up * .5f; floor.transform.localScale = new Vector3(30, 1, 30); Physics.SyncTransforms();
        var figures = new List<Transform>();
        for (int i = 0; i < 4; i++)
        {
            var obj = Make("Party test " + i); figures.Add(obj.transform);
            var info = obj.AddComponent<BattleCharacterInfo>(); info.Initiative = 40 - i * 10;
            info.Vitality = i; info.Strength = 5 + i;
            if (i < 3)
            {
                var loadout = obj.AddComponent<BattleActionLoadout>(); loadout.Options.Clear();
                loadout.Options.Add(new BattleActionOption { Name = "Personal attack " + i, Reach = 5, Damage = 7 + i });
                loadout.Options.Add(new BattleActionOption { Name = "Personal ward " + i, Category = BattleActionCategory.Skill, Reach = 5, Effect = ActionEffect.Support, BarrierGranted = 20 + i });
            }
        }
        var camera = Make("Source").AddComponent<Camera>();
        battle = Make("Party encounter").AddComponent<InSceneGridBattle>();
        battle.Initialize(new TerrainBattleGrid(origin, 8, 8, 2, ~0, new Collider[0]), figures, new float[4],
            new[] { new Vector2Int(2, 2), new Vector2Int(3, 3), new Vector2Int(4, 2), new Vector2Int(5, 3) }, camera,
            Shader.Find("Game/GridOverlay"), result => outcome = result, false, 3);
        battle.AdvanceFacing(1);
    }
    [TearDown] public void Cleanup()
    { for (int i = objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(objects[i]); objects.Clear(); outcome = null; }
    void Next() => typeof(InSceneGridBattle).GetMethod("AdvanceTurn", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(battle, null);
    BattleCharacterInfo Reserve(string name = "Reserve")
    {
        var info = Make(name).AddComponent<BattleCharacterInfo>(); info.Initiative = 99;
        battle.AddReserve(info, 0); return info;
    }
    [Test] public void SwapTransfersMovementConsumesActionAndPreservesPersonalBank()
    {
        var reserve = Reserve(); var incoming = battle.Reserves[0].Unit;
        incoming.BeginTurn(); incoming.EndTurn(); // Three saved personal points.
        var outgoing = battle.Combatants[0]; outgoing.SpendMovement(1);
        var position = outgoing.Position;
        Assert.IsTrue(battle.SwapWithReserve(0));
        Assert.AreSame(incoming, battle.Combatants[0]); Assert.AreEqual(position, incoming.Position);
        Assert.AreEqual(2, incoming.Movement); Assert.IsFalse(incoming.ActionAvailable);
        Assert.AreEqual(0, outgoing.Movement); Assert.IsFalse(outgoing.TurnOpen);
        Assert.IsFalse(battle.SwapWithReserve(0));
        Next(); Next(); Next(); Next();
        Assert.AreEqual(0, battle.CurrentActorIndex); Assert.AreEqual(8, incoming.Movement);
        CollectionAssert.Contains(battle.Participants, reserve);
    }
    [Test] public void KnockedMemberCannotManuallySwapButFullWipeDeploysLivingReserves()
    {
        var reserve = Reserve(); battle.Combatants[0].Health = 0;
        Assert.IsFalse(battle.SwapWithReserve(0));
        var resolve = typeof(InSceneGridBattle).GetMethod("ResolveOutcome", BindingFlags.Instance | BindingFlags.NonPublic);
        resolve.Invoke(battle, null); Assert.IsFalse(battle.ReserveTakeover);
        battle.Combatants[1].Health = battle.Combatants[2].Health = 0;
        resolve.Invoke(battle, null);
        Assert.IsTrue(battle.ReserveTakeover); Assert.IsNull(outcome);
        Assert.AreSame(reserve, battle.Info(0)); Assert.AreEqual(0, battle.Reserves[0].Unit.Health);
        Assert.IsFalse(battle.Reserves[0].Info.gameObject.activeSelf);
        battle.Combatants[0].Health = 0; resolve.Invoke(battle, null);
        Assert.AreEqual(false, outcome); Assert.IsTrue(battle.TotalDefeat);
    }
    [Test] public void StoredHealthPersistsAndKnockedMembersReturnAtOneNextBattle()
    {
        battle.Combatants[0].Health = 17; battle.Combatants[1].Health = 0;
        battle.StorePartyHealth();
        var healthy = new GridUnit("next", Vector2Int.zero); var knocked = new GridUnit("next", Vector2Int.zero);
        battle.Info(0).Configure(healthy); battle.Info(0).RestoreBattleHealth(healthy);
        battle.Info(1).Configure(knocked); battle.Info(1).RestoreBattleHealth(knocked);
        Assert.AreEqual(17, healthy.Health); Assert.AreEqual(1, knocked.Health);
        battle.Info(1).HealOutsideCombat(25); battle.Info(1).RestoreBattleHealth(knocked); Assert.AreEqual(25, knocked.Health);
    }
    [Test] public void ParticipationXpIsFullForEveryDeployedMemberAndHalfForUnusedReserves()
    {
        var first = Reserve("First reserve"); var second = Reserve("Second reserve");
        var party = new PartyProgression();
        for (int i = 0; i < 3; i++) party.Register(battle.Info(i));
        party.Register(first); party.Register(second);
        var original = battle.Info(0); battle.SwapWithReserve(0);
        party.AwardExperience(101, battle.Participants);
        Assert.AreEqual(101, party.ExperienceFor(original)); Assert.AreEqual(101, party.ExperienceFor(first));
        Assert.AreEqual(50, party.ExperienceFor(second));
        party.AwardExperience(1, battle.Participants); Assert.AreEqual(51, party.ExperienceFor(second));
        party.Register(second); Assert.AreEqual(1, second.Level);
    }
    [Test] public void SwapPanelRequiresConfirmationAndCancellationSpendsNothing()
    {
        Reserve(); var original = battle.Combatants[0];
        var root = Make("Swap UI"); root.AddComponent<RectTransform>(); root.AddComponent<Canvas>();
        var type = System.Type.GetType("BattleSwapPanel, Assembly-CSharp", true); var panel = root.AddComponent(type);
        type.GetMethod("Open").Invoke(panel, new object[] { battle, TMPro.TMP_Settings.defaultFontAsset });
        Assert.IsTrue(battle.PartyInputBlocked); Assert.IsFalse(battle.GridInputEnabled);
        var choose = type.GetMethod("Choose", BindingFlags.Instance | BindingFlags.NonPublic);
        choose.Invoke(panel, new object[] { 0 });
        Assert.AreSame(original, battle.Combatants[0]); Assert.IsTrue(original.ActionAvailable);
        type.GetMethod("Close").Invoke(panel, null); Assert.IsTrue(original.ActionAvailable);
        type.GetMethod("Open").Invoke(panel, new object[] { battle, TMPro.TMP_Settings.defaultFontAsset });
        choose.Invoke(panel, new object[] { 0 });
        type.GetMethod("Confirm", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(panel, null);
        Assert.AreNotSame(original, battle.Combatants[0]); Assert.IsFalse(battle.Combatants[0].ActionAvailable);
        Assert.IsFalse(battle.PartySelectionOpen); Assert.IsTrue(battle.GridInputEnabled);
    }
    void Resolve(BattleActionOption option, Vector2Int cell)
    {
        Assert.IsTrue(battle.ChooseAction(option)); Assert.IsTrue(battle.ToggleTarget(cell));
        Assert.IsTrue(battle.TryPrepareAction(out var action)); while (action.MoveNext()) { }
    }
    [Test] public void EachAllyUsesOwnStatsLoadoutActionAndMovementBalance()
    {
        Assert.AreEqual(3, battle.AllyCount); Assert.AreEqual(100, battle.Combatants[0].MaxHealth);
        Assert.AreEqual(110, battle.Combatants[1].MaxHealth); Assert.AreEqual(120, battle.Combatants[2].MaxHealth);
        var first = battle.Loadout; battle.Combatants[0].SpendMovement(2); battle.Dash();
        Assert.AreEqual(4, battle.Combatants[0].Movement); Next();
        Assert.IsTrue(battle.PlayerReady); Assert.AreEqual("Personal attack 1", battle.Loadout.Options[0].Name);
        Assert.AreNotSame(first, battle.Loadout); Assert.IsFalse(battle.ChooseAction(first.Options[0]));
        Resolve(battle.Loadout.Options[0], battle.Combatants[3].Position);
        Assert.AreEqual(122, battle.Combatants[3].Health); Assert.IsFalse(battle.Combatants[1].ActionAvailable);
        Assert.AreEqual(3, battle.Combatants[1].Movement); Next();
        Assert.AreEqual("Personal attack 2", battle.Loadout.Options[0].Name);
        Resolve(battle.Loadout.Options[1], battle.Combatants[1].Position);
        Assert.AreEqual(22, battle.Combatants[1].Barrier); Assert.AreEqual(0, battle.Combatants[0].Barrier);
        Next(); Next(); Assert.AreEqual(0, battle.CurrentActorIndex); Assert.AreEqual(7, battle.Movement);
        Assert.IsTrue(battle.Combatants[0].ActionAvailable);
    }
    [Test] public void EnemyChoosesLivingCompanionAndDeadLeaderDoesNotLoseBattle()
    {
        Assert.AreEqual(2, battle.SelectEnemyTarget(3));
        battle.Combatants[2].Health = 0; Assert.AreEqual(1, battle.SelectEnemyTarget(3));
        battle.Combatants[0].Health = 1; battle.Combatants[0].ApplyStatus(CombatStatus.Burning, 1);
        Next(); Next(); Next();
        Assert.AreEqual(1, battle.CurrentActorIndex); Assert.IsNull(outcome);
        Assert.AreEqual(0, battle.Combatants[0].Health); Assert.IsTrue(battle.Actor(1).gameObject.activeSelf);
    }
    [Test] public void ReactionDamageAndFacingBelongToTheTargetedCompanion()
    {
        Next(); Next(); Next(); Assert.AreEqual(3, battle.CurrentActorIndex);
        typeof(InSceneGridBattle).GetField("defenderIndex", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(battle, 2);
        battle.SetAutoCamera(false);
        var attack = (IEnumerator)typeof(InSceneGridBattle).GetMethod("EnemyAttack", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(battle, new object[] { 3, false });
        Assert.IsTrue(attack.MoveNext()); battle.AdvanceFacing(1);
        if (battle.CurrentImpact == null) Assert.IsTrue(attack.MoveNext());
        Assert.IsNotNull(battle.CurrentImpact);
        battle.AdvanceDefense(battle.CurrentImpact.Duration + .1f);
        Assert.AreEqual(87, battle.Combatants[2].Health); // 25 + enemy Strength 8
        Assert.AreEqual(100, battle.Combatants[0].Health); Assert.AreEqual(110, battle.Combatants[1].Health);
        StringAssert.Contains("Party test 2", battle.DefenseCaption);
    }
    [Test] public void PartyHoverColorsTrackActiveAllyInsteadOfOnlyActorZero()
    {
        Assert.AreEqual(Color.cyan, battle.Targetables[1].HoverColor(0)); Next();
        Assert.AreEqual(new Color(.5f, 1f, 0), battle.Targetables[1].HoverColor(battle.CurrentActorIndex));
        Assert.AreEqual(Color.cyan, battle.Targetables[0].HoverColor(1)); Assert.AreEqual(Color.red, battle.Targetables[3].HoverColor(3));
    }
    [Test] public void InactiveCompanionSnapshotRestoresActivationPositionAndColliderState()
    {
        var companion = Make("Inactive companion"); var shape = companion.AddComponent<CharacterController>();
        shape.height = 2; shape.center = Vector3.up; var position = new Vector3(90000, 4, 90000);
        companion.transform.position = position; companion.SetActive(false);
        var type = System.Type.GetType("BattleManager+ActorSnapshot, Assembly-CSharp", true);
        var snapshot = System.Activator.CreateInstance(type, new object[] { companion.transform });
        Assert.AreEqual(0f, type.GetField("FootOffset").GetValue(snapshot));
        type.GetMethod("Suspend").Invoke(snapshot, null); companion.SetActive(true);
        Assert.IsFalse(shape.enabled); companion.transform.position += Vector3.right * 10;
        type.GetMethod("Restore").Invoke(snapshot, null);
        Assert.IsFalse(companion.activeSelf); Assert.IsTrue(shape.enabled); Assert.AreEqual(position, companion.transform.position);
    }
    [Test] public void WholePartyDefeatEndsEncounterButOneAllyFallingDoesNot()
    {
        battle.Combatants[0].Health = 0;
        var check = typeof(InSceneGridBattle).GetMethod("ResolveOutcome", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.AreEqual(false, check.Invoke(battle, null)); Assert.IsNull(outcome);
        battle.Combatants[1].Health = battle.Combatants[2].Health = 0;
        Assert.AreEqual(true, check.Invoke(battle, null)); Assert.AreEqual(false, outcome);
    }
}
