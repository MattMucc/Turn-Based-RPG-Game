using System;
using System.Reflection;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using NUnit.Framework;
using UnityEngine;
using Game.GridPrototype;

public class BattleActionTests
{
    private readonly List<GameObject> objects = new();
    private InSceneGridBattle battle;
    private readonly Vector2Int[] cells = { new(3, 3), new(4, 3), new(5, 3), new(4, 4), new(5, 5) };
    private GameObject Create(string name) { var obj = new GameObject(name); objects.Add(obj); return obj; }
    [SetUp] public void Setup()
    {
        var origin = new Vector3(20000, 0, 20000);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); objects.Add(floor);
        floor.transform.position = origin - Vector3.up * 0.5f; floor.transform.localScale = new Vector3(30, 1, 30);
        Physics.SyncTransforms();
        var grid = new TerrainBattleGrid(origin, 8, 8, 2, ~0, new Collider[0]);
        var figures = new List<Transform>();
        for (int i = 0; i < cells.Length; i++) figures.Add(Create("Test unit " + i).transform);
        var camera = Create("Source camera").AddComponent<Camera>();
        battle = Create("Test encounter").AddComponent<InSceneGridBattle>();
        battle.Initialize(grid, figures, new float[cells.Length], cells, camera, Shader.Find("Game/GridOverlay"), victory => { });
        battle.Inventory.Items.Clear();
        battle.Inventory.Items.Add(new BattleActionOption { ItemId="test-flask",Name="Blast Flask",Category=BattleActionCategory.Item,Pattern=BattleTargetPattern.Area,Reach=3,Damage=20,Quantity=3 });
        battle.Inventory.NotifyChanged();
        DemoActionFixtures.Populate(battle.Loadout);
        battle.AdvanceFacing(1);
    }
    [TearDown] public void Cleanup()
    {
        for (int i = objects.Count - 1; i >= 0; i--) if (objects[i]) Object.DestroyImmediate(objects[i]);
        objects.Clear(); Physics.SyncTransforms();
    }
    [Test] public void DashSkillSpendsOnlyActionAndNeedsNoTarget()
    {
        var dash = new BattleActionOption { Name = "Dash", Category = BattleActionCategory.Skill, Effect = ActionEffect.Dash };
        battle.Loadout.Options.Add(dash);
        int movement = battle.Movement;
        int points = battle.Combatants[battle.CurrentActorIndex].SkillPoints;
        Assert.IsTrue(battle.ChooseAction(dash));
        Assert.AreEqual(movement + 3, battle.Movement);
        Assert.AreEqual(points - 1, battle.Combatants[battle.CurrentActorIndex].SkillPoints);
        Assert.IsNull(battle.ChosenAction);
        Assert.IsFalse(battle.ActionReady);
        Assert.IsTrue(battle.PlayerReady, "Dash must leave the turn open for movement.");
        Assert.IsFalse(battle.ChooseAction(dash));
        Assert.AreEqual(movement + 3, battle.Movement);
    }
    [Test] public void SkillSelectionIsFreeAndCommitRechecksPointsThenChargesOnce()
    {
        var unit = battle.Combatants[battle.CurrentActorIndex];
        var skill = new BattleActionOption { Name = "Test SP", Category = BattleActionCategory.Skill, SkillPointCost = 4, Damage = 0 };
        battle.Loadout.Options.Add(skill);
        Assert.IsTrue(battle.ChooseAction(skill)); Assert.AreEqual(4, unit.SkillPoints);
        battle.CancelAction(); Assert.AreEqual(4, unit.SkillPoints);
        Assert.IsTrue(battle.ChooseAction(skill)); Assert.IsTrue(battle.ToggleTarget(unit.Position));
        unit.InitializeSkillPoints(3);
        Assert.IsFalse(battle.TryPrepareAction(out _)); Assert.IsTrue(unit.ActionAvailable);
        unit.InitializeSkillPoints(4);
        Assert.IsTrue(battle.TryPrepareAction(out var sequence)); Assert.AreEqual(0, unit.SkillPoints);
        Assert.IsFalse(battle.TryPrepareAction(out _));
        while (sequence.MoveNext()) { }
        Assert.AreEqual(0, unit.SkillPoints); Assert.AreEqual(3, unit.Movement);
    }
    [Test] public void MultiHitBasicAttackRecoversOnePointOnlyAfterAllHits()
    {
        var unit = battle.Combatants[battle.CurrentActorIndex];
        var attack = new BattleActionOption { Name = "Test attack", Pattern = BattleTargetPattern.Multiple, MaxTargets = 3, Damage = 0, HitChance = 0 };
        battle.Loadout.Options.Add(attack);
        Assert.IsTrue(battle.ChooseAction(attack));
        for (int i = 0; i < 3; i++) Assert.IsTrue(battle.ToggleTarget(unit.Position));
        Assert.IsTrue(battle.TryPrepareAction(out var sequence)); Assert.AreEqual(4, unit.SkillPoints);
        Assert.IsTrue(sequence.MoveNext()); Assert.AreEqual(4, unit.SkillPoints);
        while (sequence.MoveNext()) { }
        Assert.AreEqual(5, unit.SkillPoints);
    }
    [Test] public void DetailsMatchDamageCalculationWithoutActionCostOrSingleHitSuffix()
    {
        var source = new GridUnit("Caster", Vector2Int.zero) { Strength = 10 };
        var target = new GridUnit("Target", Vector2Int.zero) { PhysicalDefense = 100 };
        var action = new BattleActionOption { Name = "Test", Damage = 20, Scaling = ScalingStat.Strength, StatScaling = 2, HitChance = 75, InflictedStatus = CombatStatus.Burning, StatusStacks = 2, StatusChance = 10 };
        string plain = action.TooltipFor(source);
        StringAssert.Contains("<b>Damage</b><pos=105>40", plain);
        StringAssert.Contains("75%", plain);
        StringAssert.Contains("Burning ×2", plain);
        StringAssert.DoesNotContain("per hit", plain);
        StringAssert.DoesNotContain("<b>Cost</b>", plain);
        StringAssert.Contains("<b>Damage</b><pos=105>20", action.TooltipFor(source, target));
        action.Pattern = BattleTargetPattern.Multiple;
        action.SignatureQte = true; action.QteBonusDamage = 10;
        StringAssert.Contains("40–50 (QTE) per hit", action.TooltipFor(source));
        action.Category = BattleActionCategory.Item; action.Quantity = 4;
        StringAssert.Contains("1 item (4 carried)", action.TooltipFor(source));
    }
    [Test] public void PotionConsumesSharedStockOnceAndLeavesMovementAvailable()
    {
        battle.Inventory.Items.Clear();battle.Inventory.Items.Add(PartyInventory.HealthPotion());battle.Inventory.NotifyChanged();
        var potion=battle.Loadout.Options.Find(x=>x.ItemId=="health-potion");
        var other=Create("Another party member").AddComponent<BattleActionLoadout>();other.BindInventory(battle.Inventory);
        var shared=other.Options.Find(x=>x.ItemId=="health-potion");
        battle.Combatants[0].Health=20;int movement=battle.Movement;
        battle.ChooseAction(potion);battle.CancelAction();Assert.AreEqual(3,shared.Quantity);
        battle.ChooseAction(potion);battle.ToggleTarget(cells[0]);Assert.IsTrue(CompletePrepared());
        Assert.AreEqual(70,battle.Combatants[0].Health);Assert.AreEqual(2,shared.Quantity);Assert.AreEqual(movement,battle.Movement);
        Assert.IsTrue(battle.PlayerReady);Assert.IsFalse(battle.ActionReady);Assert.IsFalse(battle.CommitAction());Assert.AreEqual(2,shared.Quantity);
    }
    private bool CompletePrepared()
    {
        if (!battle.TryPrepareAction(out var sequence)) return false;
        while (sequence.MoveNext()) { }
        return true;
    }
    [Test] public void CancelAndInvalidTargetPreserveActionMovementAndItem()
    {
        var item = battle.Loadout.Options.Find(o => o.Category == BattleActionCategory.Item);
        Assert.IsTrue(battle.ChooseAction(item));
        Assert.IsFalse(battle.ToggleTarget(new Vector2Int(-1, -1))); Assert.IsFalse(battle.CommitAction());
        battle.ToggleTarget(cells[1]); battle.CancelAction();
        Assert.IsTrue(battle.ActionReady); Assert.AreEqual(3, battle.Movement); Assert.AreEqual(3, item.Quantity);
    }
    [Test] public void MultiTargetCapsTogglesAndDamagesOnlyChosenEnemies()
    {
        var action = battle.Loadout.Options.Find(o => o.Pattern == BattleTargetPattern.Multiple);
        battle.ChooseAction(action);
        foreach (int i in new[] { 1, 3, 2 }) Assert.IsTrue(battle.ToggleTarget(cells[i]));
        Assert.IsFalse(battle.ToggleTarget(cells[4])); Assert.AreEqual(3, battle.TargetCount);
        battle.UndoTarget(); Assert.AreEqual(2, battle.TargetCount);
        Assert.IsFalse(battle.ReadyToResolve); Assert.IsFalse(battle.TryPrepareAction(out _));
        battle.ToggleTarget(cells[3]); Assert.IsTrue(CompletePrepared());
        Assert.AreEqual(88, battle.Grid.Board.Units[1].Health);
        Assert.AreEqual(100, battle.Grid.Board.Units[2].Health);
        Assert.AreEqual(76, battle.Grid.Board.Units[3].Health);
        Assert.AreEqual(100, battle.Grid.Board.Units[4].Health);
        Assert.IsFalse(battle.ActionReady); Assert.AreEqual(3, battle.Movement);
    }
    [Test] public void RepeatedTargetsSpendOneActionAndDealOneHitPerAssignment()
    {
        var action = battle.Loadout.Options.Find(o => o.Pattern == BattleTargetPattern.Multiple);
        battle.ChooseAction(action);
        battle.ToggleTarget(cells[1]); battle.ToggleTarget(cells[2]); battle.ToggleTarget(cells[2]);
        Assert.AreEqual(1, battle.AssignedHits(1)); Assert.AreEqual(2, battle.AssignedHits(2));
        Assert.IsFalse(battle.ToggleTarget(cells[2]));
        Assert.IsTrue(CompletePrepared());
        Assert.AreEqual(88, battle.Combatants[1].Health); Assert.AreEqual(76, battle.Combatants[2].Health);
        Assert.AreEqual(12, battle.RecentDamage(2)); Assert.AreEqual(3, battle.Movement); Assert.IsFalse(battle.ActionReady);
    }
    [Test] public void ClearAndUndoHitPreserveResourcesAndAllowReassignment()
    {
        battle.ChooseAction(battle.Loadout.Options.Find(o => o.Pattern == BattleTargetPattern.Multiple));
        battle.ToggleTarget(cells[1]); battle.ToggleTarget(cells[1]); battle.UndoTarget();
        Assert.AreEqual(1, battle.AssignedHits(1)); battle.ClearTargets();
        Assert.AreEqual(0, battle.TargetCount); Assert.IsFalse(battle.CommitAction()); Assert.IsTrue(battle.ActionReady);
        Assert.IsTrue(battle.ToggleTarget(cells[2]));
    }
    [Test] public void HoverDoesNotSelectSpendOrReplaceAnAlreadyChosenAction()
    {
        var first = battle.Loadout.Options[0]; var other = battle.Loadout.Options[1];
        battle.HoverAction(first); Assert.AreSame(first, battle.HoveredAction);
        battle.HoverAction(other); Assert.AreSame(other, battle.HoveredAction); Assert.IsNull(battle.ChosenAction);
        battle.ChooseAction(first); battle.HoverAction(other);
        Assert.AreSame(first, battle.ChosenAction); Assert.IsNull(battle.HoveredAction); Assert.IsTrue(battle.ActionReady);
    }
    [Test] public void PushActionMovesWeakerTargetOneCellWithoutSpendingMovement()
    {
        battle.Combatants[0].Strength = 10; battle.Combatants[1].Strength = 5;
        battle.Combatants[2].Position = new Vector2Int(6, 3);
        var action = new BattleActionOption { Name = "Push", Effect = ActionEffect.Push, Damage = 0 };
        battle.Loadout.Options.Add(action); battle.ChooseAction(action); battle.ToggleTarget(cells[1]);
        Assert.IsTrue(CompletePrepared());
        Assert.AreEqual(new Vector2Int(5, 3), battle.Combatants[1].Position);
        Assert.AreEqual(100, battle.Combatants[1].Health); Assert.AreEqual(3, battle.Movement);
        Assert.IsFalse(battle.Combatants[0].ActionAvailable);
    }
    [Test] public void LethalBurnAtEnemyTurnStartRemovesItAndContinuesOrder()
    {
        battle.Combatants[1].Health = 1; battle.Combatants[1].ApplyStatus(CombatStatus.Burning, 1);
        typeof(InSceneGridBattle).GetMethod("AdvanceTurn", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(battle, null);
        Assert.AreEqual(2, battle.CurrentActorIndex); Assert.AreEqual(0, battle.Combatants[1].Health);
        Assert.IsFalse(battle.Actor(1).gameObject.activeSelf); Assert.AreEqual(3, battle.Combatants[2].Movement);
    }
    [Test] public void ParalysisRejectsBlockWithoutSpendingAction()
    {
        battle.Combatants[0].ApplyStatus(CombatStatus.Paralyzed, 1); battle.Block();
        Assert.IsTrue(battle.Combatants[0].ActionAvailable); Assert.IsFalse(battle.Combatants[0].Blocking);
    }
    [Test] public void UpcomingTurnsWrapFromCurrentActorAndSkipDefeatedUnits()
    {
        battle.Combatants[2].Health = 0;
        typeof(InSceneGridBattle).GetField("actorIndex", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(battle, 3);
        CollectionAssert.AreEqual(new[] { 3, 4, 0, 1 }, battle.UpcomingTurns());
        Assert.AreEqual(100, battle.Combatants[0].MaxHealth);
    }
    [Test] public void ItemConsumesExactlyOnceAndDamageMatchesAreaPreview()
    {
        var action = battle.Loadout.Options.Find(o => o.Category == BattleActionCategory.Item);
        battle.ChooseAction(action); battle.ToggleTarget(cells[1]);
        var affected = GridTargeting.Footprint(battle.Grid.Board, cells[0], cells[1], action);
        Assert.IsTrue(CompletePrepared()); Assert.IsFalse(battle.CommitAction()); Assert.AreEqual(2, action.Quantity);
        for (int i = 0; i < cells.Length; i++) Assert.AreEqual(affected.Contains(cells[i]) ? 80 : 100, battle.Grid.Board.Units[i].Health);
    }
    [Test] public void TargetBecomingInvalidPreventsAllCosts()
    {
        var action = battle.Loadout.Options[0]; battle.ChooseAction(action); battle.ToggleTarget(cells[1]);
        battle.Grid.Board.Cells[cells[1].x, cells[1].y].Ground = Ground.Obstacle;
        Assert.IsFalse(battle.CommitAction()); Assert.IsTrue(battle.ActionReady);
    }
    [Test] public void OriginalMenuLifecycleListsLocksCancelsAndHides()
    {
        // Exercise the actual Assembly-CSharp UI without starting Play Mode.
        var menuType = Type.GetType("BattleMenuUI, Assembly-CSharp");
        var buttonType = Type.GetType("BattleMenuButton, Assembly-CSharp");
        var root = Create("Test UI"); root.AddComponent<RectTransform>(); root.AddComponent<Canvas>();
        var menu = root.AddComponent(menuType);
        var container = new GameObject("Buttons", typeof(RectTransform), typeof(VerticalLayoutGroup)); container.transform.SetParent(root.transform, false);
        foreach (string field in new[] { "attackButton", "skillsButton", "itemsButton", "runButton" })
        {
            var obj = new GameObject(field, typeof(RectTransform), typeof(Image), typeof(Button)); obj.transform.SetParent(container.transform, false);
            obj.GetComponent<Button>().targetGraphic = obj.GetComponent<Image>();
            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); label.transform.SetParent(obj.transform, false);
            var button = obj.AddComponent(buttonType);
            menuType.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(menu, button);
        }
        Button Find(string label)
        {
            foreach (var button in root.GetComponentsInChildren<Button>())
                if (button.transform.Find("Label")?.GetComponent<TMP_Text>()?.text == label) return button;
            Assert.Fail("Missing active button: " + label); return null;
        }
        menuType.GetField("blockKeyboardIcon", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(menu,
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Third Party Downloads/GameInputControllerIconsFree/keyboard/keyboard-outlined/4.png"));
        menuType.GetField("runKeyboardIcon", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(menu,
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Third Party Downloads/GameInputControllerIconsFree/keyboard/keyboard-outlined/5.png"));
        var endKey = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Third Party Downloads/GameInputControllerIconsFree/keyboard/keyboard-outlined/t.png");
        var endPad = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Third Party Downloads/Flat Icons [Free]/Free Flat Button Solid Right 3 Icon.png");
        menuType.GetField("endTurnKeyboardIcon", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(menu, endKey);
        menuType.GetField("endTurnControllerIcon", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(menu, endPad);
        menuType.GetMethod("Bind").Invoke(menu, new object[] { battle });
        Assert.AreSame(endKey, buttonType.GetProperty("KeyboardIcon").GetValue(Find("End Turn").GetComponent(buttonType)));
        Assert.AreSame(endPad, buttonType.GetProperty("ControllerIcon").GetValue(Find("End Turn").GetComponent(buttonType)));
        Assert.AreSame(endPad, buttonType.GetProperty("ControllerIcon").GetValue(root.transform.Find("Remaining turn").GetComponent(buttonType)));
        Assert.AreEqual("4", ((Sprite)buttonType.GetProperty("KeyboardIcon").GetValue(Find("Block").GetComponent(buttonType))).name);
        Assert.AreEqual("5", ((Sprite)buttonType.GetProperty("KeyboardIcon").GetValue(Find("Run").GetComponent(buttonType))).name);
        Assert.AreEqual(8, container.GetComponentsInChildren<Button>().Length);
        CollectionAssert.AreEqual(new[] { "Attacks", "Skills", "Spells", "Block", "Items", "Swap", "Run", "End Turn" },
            Array.ConvertAll(container.GetComponentsInChildren<Button>(), b => b.GetComponentInChildren<TMP_Text>().text));
        Assert.IsNull(root.transform.Find("Battle targeting"));
        var hudType = Type.GetType("BattleHud, Assembly-CSharp");
        var hud = root.GetComponentInChildren(hudType);
        hudType.GetMethod("ShowContext").Invoke(hud, new object[] { 1, Vector2.zero });
        Assert.IsTrue(battle.InspectionOpen);
        Find("Examine").onClick.Invoke();
        battle.Combatants[1].Health = 37; hudType.GetMethod("Render").Invoke(hud, null);
        Assert.IsTrue(Array.Exists(root.GetComponentsInChildren<TMP_Text>(), t => t.text.Contains("Health  37 / 100")));
        Find("Close").onClick.Invoke(); Assert.IsFalse(battle.InspectionOpen);
        var portraitType = Type.GetType("BattlePortrait, Assembly-CSharp");
        var portrait = root.GetComponentsInChildren(portraitType)[1];
        ((Action)portraitType.GetField("LeftClick").GetValue(portrait))();
        var cameraControl = battle.BattleCamera.GetComponent<GridCameraController>();
        Assert.AreEqual(battle.Actor(1), cameraControl.FocusTarget); Assert.IsTrue(cameraControl.Transitioning);
        hudType.GetMethod("Render").Invoke(hud, null);
        var targetPanel = ((Component)hud).transform.Find("Hovered or pinned target");
        var track = (RectTransform)targetPanel.Find("Target maximum health");
        var fill = (RectTransform)track.Find("Target current health");
        float trackWidth = track.rect.width;
        Assert.IsTrue(targetPanel.gameObject.activeSelf);
        Assert.AreEqual("37 / 100", track.GetComponentInChildren<TMP_Text>().text);
        Assert.AreEqual(0.37f, fill.anchorMax.x, 0.001f);
        battle.Combatants[2].MaxHealth = 200; battle.Combatants[2].Health = 150;
        battle.TargetFocus.Hover(battle.Targetables[2]); hudType.GetMethod("Render").Invoke(hud, null);
        Assert.AreEqual("150 / 200", track.GetComponentInChildren<TMP_Text>().text);
        Assert.AreEqual(battle.Targetables[2].Name, targetPanel.Find("Target name").GetComponentInChildren<TMP_Text>().text);
        Assert.AreEqual(trackWidth, track.rect.width); Assert.AreEqual(0.75f, fill.anchorMax.x, 0.001f);
        battle.TargetFocus.Hover(null); hudType.GetMethod("Render").Invoke(hud, null);
        Assert.AreEqual("37 / 100", track.GetComponentInChildren<TMP_Text>().text);
        battle.TargetFocus.Dismiss(); hudType.GetMethod("Render").Invoke(hud, null);
        Assert.IsFalse(targetPanel.gameObject.activeSelf);
        Assert.AreEqual(true, menuType.GetMethod("CanUseEndTurnShortcut", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(menu, null));
        Find("Skills").onClick.Invoke();
        Assert.AreEqual(false, menuType.GetMethod("CanUseEndTurnShortcut", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(menu, null));
        Assert.AreEqual(battle.Loadout.Options.FindAll(o => o.Category == BattleActionCategory.Skill).Count + 1, container.GetComponentsInChildren<Button>().Length);
        Assert.IsFalse(Array.Exists(container.GetComponentsInChildren<TMP_Text>(), t => t.text == "Favorites"));
        // Assignment belongs to the future player menu; existing shortcuts remain usable in battle.
        battle.Loadout.AssignFavorite(battle.Loadout.Options.Find(o => o.Pattern == BattleTargetPattern.Multiple), 2);
        menuType.GetMethod("UseFavorite").Invoke(menu, new object[] { 2 });
        Assert.AreEqual(BattleTargetPattern.Multiple, battle.ChosenAction.Pattern);
        Assert.AreEqual(0, battle.TargetCount); Assert.IsTrue(battle.ActionReady);
        menuType.GetMethod("CancelSelection", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(menu, null);
        var area = Find("Area Skill");
        var hover = (Action<bool>)buttonType.GetField("HoverChanged").GetValue(area.GetComponent(buttonType));
        hover(true); Assert.AreEqual(BattleTargetPattern.Area, battle.HoveredAction.Pattern); Assert.IsNull(battle.ChosenAction);
        hover(false); Assert.IsNull(battle.HoveredAction);
        Assert.That(area.GetComponentInChildren<TMP_Text>().color.grayscale, Is.LessThan(0.15f));
        Find("Turn order: Top").onClick.Invoke(); Find("Turn order: Right");
        Find("Auto camera: On").onClick.Invoke(); Assert.IsFalse(battle.AutoCamera);
        Find("Multi-target Skill").onClick.Invoke();
        Assert.IsFalse(Find("Area Skill").interactable);
        Assert.IsTrue(Find("Multi-target Skill").interactable);
        Assert.IsFalse(Find("Undo Hit").interactable);
        Assert.AreEqual(battle.Loadout.Options.FindAll(o => o.Category == BattleActionCategory.Skill).Count + 3, container.GetComponentsInChildren<Button>().Length);
        Assert.IsTrue(battle.KeyboardGridInputEnabled);
        battle.ToggleTarget(cells[1]); battle.ToggleTarget(cells[1]);
        menuType.GetMethod("UpdateAvailability", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(menu, null);
        var counter = root.transform.Find("Target selection count").GetComponentInChildren<TMP_Text>();
        Assert.AreEqual("2 / 3 targets selected", counter.text);
        Assert.IsTrue(counter.gameObject.activeInHierarchy);
        menuType.GetMethod("CancelSelection", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(menu, null);
        Assert.IsNull(battle.ChosenAction); Assert.AreEqual(0, battle.TargetCount); Assert.IsTrue(battle.ActionReady);
        Assert.IsTrue(Find("Area Skill").interactable); Assert.IsFalse(counter.gameObject.activeInHierarchy);
        Find("Multi-target Skill").onClick.Invoke(); battle.ToggleTarget(cells[1]);
        Find("Back").onClick.Invoke(); Assert.AreEqual(8, container.GetComponentsInChildren<Button>().Length);
        CollectionAssert.AreEqual(new[] { "Attacks", "Skills", "Spells", "Block", "Items", "Swap", "Run", "End Turn" },
            Array.ConvertAll(container.GetComponentsInChildren<Button>(), b => b.GetComponentInChildren<TMP_Text>().text));
        Assert.IsNull(battle.ChosenAction); Assert.AreEqual(0, battle.TargetCount); Assert.IsTrue(battle.ActionReady);
        Assert.AreEqual(3, Find("Block").transform.GetSiblingIndex()); Assert.AreEqual(6, Find("Run").transform.GetSiblingIndex());
        menuType.GetMethod("Unbind").Invoke(menu, null); Assert.IsFalse(root.activeSelf);
        menuType.GetMethod("Bind").Invoke(menu, new object[] { battle }); Assert.IsTrue(root.activeSelf);
        Assert.AreEqual(8, container.GetComponentsInChildren<Button>().Length);
        CollectionAssert.AreEqual(new[] { "Attacks", "Skills", "Spells", "Block", "Items", "Swap", "Run", "End Turn" },
            Array.ConvertAll(container.GetComponentsInChildren<Button>(), b => b.GetComponentInChildren<TMP_Text>().text));
        battle.Block();
        menuType.GetMethod("UpdateAvailability", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(menu, null);
        Assert.IsFalse(root.transform.Find("Battle command viewport").gameObject.activeSelf);
        Assert.IsTrue(root.transform.Find("Remaining turn").gameObject.activeSelf);
        Assert.AreEqual(true, menuType.GetMethod("CanUseEndTurnShortcut", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(menu, null));
        Assert.IsTrue(((Component)hud).gameObject.activeInHierarchy);
        Assert.AreEqual(3, battle.Movement);
        typeof(InSceneGridBattle).GetField("actorIndex", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(battle, 1);
        menuType.GetMethod("UpdateAvailability", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(menu, null);
        Assert.IsFalse(root.transform.Find("Remaining turn").gameObject.activeSelf);
        Assert.IsFalse(root.transform.Find("Battle command viewport").gameObject.activeSelf);
        typeof(InSceneGridBattle).GetField("actorIndex", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(battle, 0);
        battle.Combatants[0].EndTurn(); battle.Combatants[0].BeginTurn();
        menuType.GetMethod("UpdateAvailability", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(menu, null);
        Assert.IsTrue(root.transform.Find("Battle command viewport").gameObject.activeSelf);
        Assert.IsFalse(root.transform.Find("Remaining turn").gameObject.activeSelf);
        battle.SetAutoCamera(false);
        battle.ChooseAction(battle.Loadout.Options.Find(o => o.SignatureQte)); battle.ToggleTarget(cells[1]);
        battle.TryPrepareAction(out var signature);
        signature.MoveNext(); battle.AdvanceFacing(1); if (battle.CurrentQte == null) signature.MoveNext();
        battle.AdvanceSignature(0.5f); battle.AdvanceSignature(0.75f);
        hudType.GetMethod("Render").Invoke(hud, null);
        menuType.GetMethod("UpdateAvailability", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(menu, null);
        Assert.IsFalse(root.transform.Find("Battle command viewport").gameObject.activeSelf);
        var qteRoot = ((Component)hud).transform.Find("Signature QTE");
        Assert.IsTrue(qteRoot.gameObject.activeSelf);
        var qteFill = (RectTransform)qteRoot.Find("QTE countdown/Deadline track/Time remaining fill");
        Assert.AreEqual(0.25f, qteFill.anchorMin.x, 0.0001f);
        Assert.AreEqual(0.75f, qteFill.anchorMax.x, 0.0001f);
        StringAssert.Contains("0.75", qteRoot.Find("QTE countdown/Seconds remaining").GetComponentInChildren<TMP_Text>().text);
        Assert.AreEqual(new Vector2(0.5f, 0), ((RectTransform)qteRoot.Find("QTE input")).anchorMin);
        battle.Finish(false); ((IDisposable)signature).Dispose();
        menuType.GetMethod("Unbind").Invoke(menu, null);
    }
    [Test] public void HitsResolveInSelectionOrderWithDelaysAndLockOtherActions()
    {
        battle.ChooseAction(battle.Loadout.Options.Find(o => o.Pattern == BattleTargetPattern.Multiple));
        battle.ToggleTarget(cells[2]); battle.ToggleTarget(cells[1]); battle.ToggleTarget(cells[2]);
        Assert.IsTrue(battle.ReadyToResolve); Assert.IsTrue(battle.TryPrepareAction(out var sequence));
        Assert.IsFalse(battle.PlayerReady); Assert.IsFalse(battle.TryPrepareAction(out _));
        Assert.AreEqual(100, battle.Combatants[2].Health);
        Assert.IsTrue(sequence.MoveNext()); Assert.IsInstanceOf<WaitForSeconds>(sequence.Current);
        Assert.AreEqual(88, battle.Combatants[2].Health); Assert.AreEqual(100, battle.Combatants[1].Health);
        Assert.IsTrue(sequence.MoveNext()); Assert.AreEqual(88, battle.Combatants[1].Health);
        Assert.IsTrue(sequence.MoveNext()); Assert.AreEqual(76, battle.Combatants[2].Health);
        Assert.IsFalse(sequence.MoveNext()); Assert.IsTrue(battle.PlayerReady); Assert.AreEqual(3, battle.Movement);
    }
    [Test] public void SingleTargetIsReadyImmediatelyButInvalidSelectionIsNot()
    {
        battle.ChooseAction(battle.Loadout.Options[0]);
        Assert.IsFalse(battle.ToggleTarget(new Vector2Int(-1, -1))); Assert.IsFalse(battle.ReadyToResolve);
        Assert.IsTrue(battle.ToggleTarget(cells[1])); Assert.IsTrue(battle.ReadyToResolve);
        Assert.IsTrue(CompletePrepared()); Assert.AreEqual(75, battle.Combatants[1].Health);
    }
    [Test] public void ExamineReadsAuthoredInfoAndLiveHealthWithoutInventingStats()
    {
        var info = battle.Info(1); var unit = battle.Combatants[1];
        info.Race = "Human"; info.Role = "Knight"; info.Level = 4; info.Strength = 12;
        unit.Health = 37; unit.Blocking = true;
        string details = info.Details(unit, battle.DisplayName(1));
        StringAssert.Contains("37 / 100", details); StringAssert.Contains("Human", details);
        StringAssert.Contains("Knight", details); StringAssert.Contains("Strength  12", details);
        StringAssert.Contains("Magic  Not assigned", details); StringAssert.Contains("Blocking", details);
    }
    [Test] public void EmptyShotSpendsActionWithoutDamageOrMovementCost()
    {
        battle.ChooseAction(battle.Loadout.Options[0]);
        Assert.IsTrue(battle.ToggleTarget(new Vector2Int(2, 3)));
        Assert.IsTrue(CompletePrepared()); Assert.IsFalse(battle.ActionReady);
        Assert.AreEqual(3, battle.Movement);
        foreach (var unit in battle.Combatants) Assert.AreEqual(100, unit.Health);
    }
    [Test] public void EmptyAreaAnchorHitsNearbyUnitsIncludingTheCaster()
    {
        var item = battle.Loadout.Options.Find(o => o.Category == BattleActionCategory.Item);
        battle.ChooseAction(item); Assert.IsTrue(battle.ToggleTarget(new Vector2Int(3, 4)));
        Assert.IsTrue(CompletePrepared()); Assert.AreEqual(2, item.Quantity);
        Assert.AreEqual(80, battle.Combatants[0].Health);
        Assert.AreEqual(80, battle.Combatants[1].Health);
        Assert.AreEqual(80, battle.Combatants[3].Health);
        Assert.AreEqual(100, battle.Combatants[2].Health);
    }
    [Test] public void SelfTargetAndVacatedEnemyCellAreBothLegal()
    {
        var strike = battle.Loadout.Options[0]; battle.ChooseAction(strike);
        Assert.IsTrue(battle.ToggleTarget(cells[0])); Assert.IsTrue(CompletePrepared());
        Assert.AreEqual(75, battle.Combatants[0].Health);
    }
    [Test] public void EnemyLeavingAnAimedCellDoesNotRefundOrInvalidateTheShot()
    {
        battle.ChooseAction(battle.Loadout.Options[0]); battle.ToggleTarget(cells[1]);
        battle.Combatants[1].Position = Vector2Int.zero;
        Assert.IsTrue(CompletePrepared()); Assert.AreEqual(100, battle.Combatants[1].Health);
        Assert.IsFalse(battle.ActionReady);
    }
    [Test] public void DestructibleCanBeHitOnBlockedCellAndDestructionOpensTerrain()
    {
        var cell = new Vector2Int(2, 3);
        var propObject = GameObject.CreatePrimitive(PrimitiveType.Cube); objects.Add(propObject);
        propObject.transform.position = battle.Grid.Point(cell) + Vector3.up * 0.5f;
        var prop = propObject.AddComponent<BattleDestructible>(); prop.Health = prop.MaxHealth = 20;
        Physics.SyncTransforms(); battle.RegisterDestructible(prop);
        Assert.AreEqual(Ground.Obstacle, battle.Grid.Board.Cells[cell.x, cell.y].Ground);
        battle.ChooseAction(battle.Loadout.Options[0]); Assert.IsTrue(battle.ToggleTarget(cell));
        Assert.IsTrue(CompletePrepared()); Assert.AreEqual(0, prop.Health);
        Assert.IsFalse(propObject.activeSelf);
        Assert.AreEqual(Ground.Land, battle.Grid.Board.Cells[cell.x, cell.y].Ground);
    }
    [Test] public void CrateOnUnsampledCellStillRegistersAndCanBeHovered()
    {
        var cell = new Vector2Int(2, 3);
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); objects.Add(obj);
        obj.transform.position = battle.Grid.Point(cell) + Vector3.up * .5f;
        var prop = obj.AddComponent<BattleDestructible>(); Physics.SyncTransforms();
        battle.Grid.HasSurface[cell.x, cell.y] = false; battle.RegisterDestructible(prop);
        battle.Grid.HasSurface[cell.x, cell.y] = false;
        Assert.IsFalse(battle.Grid.TryCell(obj.transform.position, out _));
        Assert.IsTrue(battle.Grid.TryCoordinate(obj.transform.position, out _));
        Assert.IsTrue(battle.TryPickTarget(battle.BattleCamera.WorldToScreenPoint(obj.transform.position), out var target, out _));
        Assert.AreSame(prop, target.Destructible);
    }
    [Test] public void CrateColliderCanBeHoveredAndOutlineCopyJoinsItsHardEdges()
    {
        var cell = new Vector2Int(2, 3);
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); objects.Add(obj);
        obj.transform.position = battle.Grid.Point(cell) + Vector3.up * .5f;
        var prop = obj.AddComponent<BattleDestructible>(); Physics.SyncTransforms(); battle.RegisterDestructible(prop);
        var point = battle.BattleCamera.WorldToScreenPoint(obj.transform.position);
        Assert.IsTrue(battle.TryPickTarget(point, out var target, out _)); Assert.AreSame(prop, target.Destructible);
        Assert.AreEqual(Color.yellow, target.HoverColor(0));
        var source = obj.GetComponent<MeshFilter>().sharedMesh; var copy = BattleTargetHighlight.CreateOutlineMesh(source);
        try
        {
            Assert.AreNotSame(source, copy); var vertices = copy.vertices; var normals = copy.normals;
            for (int i = 0; i < vertices.Length; i++)
                for (int j = i + 1; j < vertices.Length; j++)
                    if (vertices[i] == vertices[j]) Assert.That(Vector3.Dot(normals[i], normals[j]), Is.GreaterThan(.999f));
            Assert.AreEqual(1, Mathf.Abs(source.normals[0].x) + Mathf.Abs(source.normals[0].y) + Mathf.Abs(source.normals[0].z));
        }
        finally { Object.DestroyImmediate(copy); }
    }
    [Test] public void PushableCrateMovesAndResamplesItsOldAndNewCell()
    {
        var cell = new Vector2Int(2, 3); var end = new Vector2Int(1, 3);
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); objects.Add(obj);
        obj.transform.position = battle.Grid.Point(cell) + Vector3.up * .5f;
        var prop = obj.AddComponent<BattleDestructible>(); prop.Pushable = true; prop.PushResistance = 5;
        Physics.SyncTransforms(); battle.RegisterDestructible(prop); battle.Combatants[0].Strength = 10;
        var action = new BattleActionOption { Name = "Push", Effect = ActionEffect.Push, Damage = 0 };
        battle.Loadout.Options.Add(action); battle.ChooseAction(action); Assert.IsTrue(battle.ToggleTarget(cell));
        Assert.IsTrue(CompletePrepared());
        Assert.AreEqual(Ground.Land, battle.Grid.Board.At(cell).Ground);
        Assert.AreEqual(Ground.Obstacle, battle.Grid.Board.At(end).Ground);
        Assert.AreEqual(50, prop.Health);
    }
    [Test] public void DodgedEnemyHitDoesNotApplyStatus()
    {
        battle.DefenseSettings.EnemyStatus = CombatStatus.Burning; battle.DefenseSettings.EnemyStatusStacks = 2;
        var sequence = BeginEnemySequence(false);
        battle.AdvanceDefense(battle.CurrentImpact.Duration - .01f);
        battle.AdvanceDefense(.04f, true, false);
        Assert.AreEqual(0, battle.Combatants[0].Stacks(CombatStatus.Burning));
    }
    [Test] public void TargetHoverTemporarilyReplacesPinAndDismissWaitsForHoverExit()
    {
        var focus = battle.TargetFocus; var a = battle.Targetables[1]; var b = battle.Targetables[2];
        focus.Pin(a); focus.Hover(b); Assert.AreSame(b, focus.Displayed);
        focus.Hover(null); Assert.AreSame(a, focus.Displayed);
        focus.Hover(b); focus.Pin(b); focus.Hover(null); Assert.AreSame(b, focus.Displayed);
        focus.Hover(b); focus.Dismiss(); focus.Hover(b); Assert.IsNull(focus.Displayed);
        focus.Hover(null); focus.Hover(b); Assert.AreSame(b, focus.Displayed);
        focus.Pin(b); b.Unit.Health = 0; Assert.IsNull(focus.Displayed);
    }
    private System.Collections.IEnumerator BeginEnemySequence(bool combo)
    {
        var sequence = (System.Collections.IEnumerator)typeof(InSceneGridBattle)
            .GetMethod("EnemyAttack", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(battle, new object[] { 1, combo });
        Assert.IsTrue(sequence.MoveNext());
        battle.BattleCamera.GetComponent<GridCameraController>().AdvanceFocus(1);
        if (!(bool)typeof(InSceneGridBattle).GetField("playerTurning", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(battle)
            && battle.CurrentImpact == null) Assert.IsTrue(sequence.MoveNext());
        return sequence;
    }
    [Test] public void ParryUsesDisplayedGoldWindowBeforeAdvancingPastImpact()
    {
        battle.DefenseSettings.InputLatencyCompensation = 0;
        var sequence = BeginEnemySequence(false);
        battle.AdvanceDefense(battle.CurrentImpact.Duration - 0.01f);
        Assert.Less(battle.CurrentImpact.BarPosition(battle.CurrentImpact.Elapsed), DefenseImpact.ImpactBarPosition);
        battle.AdvanceDefense(0.04f, false, true);
        Assert.IsTrue(battle.CurrentImpact.Resolved);
        Assert.AreEqual(DefenseResult.Parried, battle.CurrentImpact.Result);
        Assert.AreEqual(100, battle.Combatants[0].Health);
    }
    [Test] public void DelayedParryPreventsDamageBeforeCompensatedDeadline()
    {
        battle.DefenseSettings.InputLatencyCompensation = 0.035f;
        var sequence = BeginEnemySequence(false);
        battle.AdvanceDefense(battle.CurrentImpact.Duration + 0.016f);
        Assert.IsFalse(battle.CurrentImpact.Resolved);
        Assert.AreEqual(100, battle.Combatants[0].Health);
        battle.AdvanceDefense(0.03f, false, true);
        Assert.IsTrue(battle.CurrentImpact.Resolved);
        Assert.AreEqual(DefenseResult.Parried, battle.CurrentImpact.Result);
        Assert.AreEqual(100, battle.Combatants[0].Health);
    }
    [TestCase(true, false, 55, QteMode.SinglePress)]
    [TestCase(false, true, 70, QteMode.SinglePress)]
    [TestCase(false, false, 70, QteMode.SinglePress)]
    [TestCase(true, false, 55, QteMode.FixedSequence)]
    [TestCase(false, true, 70, QteMode.FixedSequence)]
    [TestCase(false, false, 70, QteMode.FixedSequence)]
    [TestCase(true, false, 55, QteMode.RepeatedTap)]
    [TestCase(false, true, 70, QteMode.RepeatedTap)]
    [TestCase(false, false, 70, QteMode.RepeatedTap)]
    [TestCase(true, false, 55, QteMode.Hold)]
    [TestCase(false, true, 70, QteMode.Hold)]
    [TestCase(false, false, 70, QteMode.Hold)]
    [TestCase(true, false, 55, QteMode.Staged)]
    [TestCase(false, true, 70, QteMode.Staged)]
    [TestCase(false, false, 70, QteMode.Staged)]
    [TestCase(true, false, 55, QteMode.DualInput)]
    [TestCase(false, true, 70, QteMode.DualInput)]
    [TestCase(false, false, 70, QteMode.DualInput)]
    public void SignatureResolvesOnceAndReturnsRemainingMovement(bool success, bool wrong, int remainingHealth, QteMode mode)
    {
        battle.SetAutoCamera(false);
        var action = battle.Loadout.Options.Find(o => o.SignatureQte && o.QteType == mode);
        Assert.IsTrue(battle.ChooseAction(action)); battle.ToggleTarget(cells[1]);
        Assert.IsTrue(battle.TryPrepareAction(out var sequence));
        Assert.IsTrue(battle.SignatureActive); Assert.IsFalse(battle.PlayerReady);
        Assert.IsFalse(battle.TryPrepareAction(out _));
        int movement = battle.Movement;
        Assert.IsTrue(sequence.MoveNext());
        battle.AdvanceFacing(1);
        if (battle.CurrentQte == null) Assert.IsTrue(sequence.MoveNext());
        battle.AdvanceSignature(0.5f, true); // Held/launch input cannot win during preparation.
        Assert.AreEqual(PressQteResult.Waiting, battle.CurrentQte.Result);
        if (mode == QteMode.FixedSequence)
        {
            Assert.AreEqual(4, battle.CurrentQte.InputCount);
            for (int step = 0; step < 3; step++) battle.AdvanceSignature(0.1f, true);
            Assert.IsFalse(battle.CurrentQte.Complete);
            Assert.AreEqual(100, battle.Combatants[1].Health);
        }
        if (mode == QteMode.RepeatedTap)
            for (int tap = 0; tap < action.QteRequiredTaps - 1; tap++) battle.AdvanceSignature(0.1f, true);
        if (mode == QteMode.Staged)
        {
            for (int stage = 0; stage < action.QteSequence.Length - 1; stage++)
            {
                battle.AdvanceSignature(0, true); Assert.IsFalse(battle.CurrentQte.Complete);
                battle.AdvanceSignature(0.35f);
            }
        }
        if (mode == QteMode.DualInput && success)
        {
            battle.AdvanceSignature(0.1f, true, secondaryHeld: true);
            Assert.IsFalse(battle.CurrentQte.Complete);
            battle.AdvanceSignature(0, true, held: true, secondaryHeld: true);
        }
        else if (mode == QteMode.Hold && success) battle.AdvanceSignature(action.QteHoldDuration, true, held: true);
        else if (success || wrong) battle.AdvanceSignature(0, success, wrong);
        else battle.AdvanceSignature(5);
        Assert.IsTrue(sequence.MoveNext()); // Result feedback before applying damage.
        battle.AdvanceSignature(0.5f);
        Assert.IsTrue(sequence.MoveNext());
        Assert.AreEqual(remainingHealth, battle.Combatants[1].Health);
        Assert.IsFalse(sequence.MoveNext());
        Assert.IsFalse(battle.SignatureActive); Assert.IsNull(battle.CurrentQte);
        Assert.IsTrue(battle.PlayerReady); Assert.IsFalse(battle.ActionReady);
        Assert.AreEqual(movement, battle.Movement);
        Assert.IsFalse(battle.BattleCamera.GetComponent<GridCameraController>().KeyboardInputBlocked);
    }
    [Test] public void SignatureWaitsForCameraAndClearsOnExit()
    {
        var action = battle.Loadout.Options.Find(o => o.SignatureQte);
        battle.ChooseAction(action); battle.ToggleTarget(cells[1]); battle.TryPrepareAction(out var sequence);
        Assert.IsTrue(sequence.MoveNext()); Assert.IsNull(battle.CurrentQte);
        battle.AdvanceFacing(1); Assert.IsTrue(sequence.MoveNext()); Assert.IsNull(battle.CurrentQte);
        battle.BattleCamera.GetComponent<GridCameraController>().AdvanceFocus(1);
        Assert.IsTrue(sequence.MoveNext()); Assert.IsNotNull(battle.CurrentQte);
        battle.Finish(false);
        Assert.IsFalse(battle.SignatureActive); Assert.IsNull(battle.CurrentQte);
        Assert.IsFalse(battle.BattleCamera.GetComponent<GridCameraController>().KeyboardInputBlocked);
        ((IDisposable)sequence).Dispose();
    }

    private void DefendCurrentHit(bool parry, bool dodge = false)
    {
        battle.AdvanceDefense(battle.CurrentImpact.Duration - 0.05f);
        battle.AdvanceDefense(0, dodge, parry);
        battle.AdvanceDefense(0.1f); battle.AdvanceDefense(0.5f);
    }
    [Test] public void PerfectComboCountersOnceWithoutSpendingMovementOrNextAction()
    {
        var player = battle.Combatants[0]; var position = battle.Actor(0).position;
        int movement = player.Movement; var sequence = BeginEnemySequence(true);
        Assert.IsFalse(battle.PlayerReady); Assert.IsFalse(battle.ChooseAction(battle.Loadout.Options[0]));
        for (int i = 0; i < 3; i++) { DefendCurrentHit(true); Assert.IsTrue(sequence.MoveNext()); }
        Assert.AreEqual(100, player.Health); Assert.AreEqual(75, battle.Combatants[1].Health);
        Assert.IsFalse(sequence.MoveNext()); Assert.IsFalse(battle.DefenseActive);
        Assert.AreEqual(movement, player.Movement); Assert.IsTrue(player.ActionAvailable);
        Assert.AreEqual(cells[0], player.Position); Assert.AreEqual(position, battle.Actor(0).position);
    }
    [Test] public void FailedFirstHitKeepsBlockAndAllowsLaterParriesWithoutCounter()
    {
        battle.Block(); var sequence = BeginEnemySequence(true);
        DefendCurrentHit(false); Assert.IsTrue(sequence.MoveNext());
        DefendCurrentHit(true); Assert.IsTrue(sequence.MoveNext());
        DefendCurrentHit(true); Assert.IsFalse(sequence.MoveNext());
        Assert.AreEqual(95, battle.Combatants[0].Health); Assert.IsTrue(battle.Combatants[0].Blocking);
        Assert.AreEqual(100, battle.Combatants[1].Health);
    }
    [Test] public void PerfectDefenseDoesNotCounterAnOutOfReachAttacker()
    {
        var sequence = BeginEnemySequence(false); DefendCurrentHit(true);
        battle.Combatants[1].Position = new Vector2Int(6, 6);
        Assert.IsTrue(sequence.MoveNext()); StringAssert.Contains("out of reach", battle.DefenseFeedback);
        Assert.AreEqual(100, battle.Combatants[1].Health); Assert.IsFalse(sequence.MoveNext());
    }
    [Test] public void ReactionDisabledTargetTakesDamageDespiteSuccessfulTiming()
    {
        battle.Combatants[0].ReactionsDisabled = true; var sequence = BeginEnemySequence(false);
        DefendCurrentHit(true); Assert.IsFalse(sequence.MoveNext());
        Assert.AreEqual(75, battle.Combatants[0].Health); Assert.AreEqual(100, battle.Combatants[1].Health);
    }
    [Test] public void SuccessfulDodgeSlowsRecoveryButLeavesNextHitTimingUnchanged()
    {
        var sequence = BeginEnemySequence(true);
        battle.AdvanceDefense(battle.CurrentImpact.Duration - 0.05f);
        battle.AdvanceDefense(0, true, false);
        battle.AdvanceDefense(0.06f + battle.CurrentImpact.InputLatencyCompensation);
        Assert.IsTrue(battle.CurrentImpact.Resolved); Assert.AreEqual(100, battle.Combatants[0].Health);
        float elapsed = battle.CurrentImpact.Elapsed;
        battle.AdvanceDefense(0.1f);
        Assert.AreEqual(0.015f, battle.CurrentImpact.Elapsed - elapsed, 0.0001f);
        battle.AdvanceDefense(0.8f); Assert.IsTrue(sequence.MoveNext());
        Assert.AreEqual(battle.DefenseSettings.ComboSecondWindup, battle.CurrentImpact.Duration);
        Assert.AreEqual(battle.DefenseSettings.ParryWindow, battle.CurrentImpact.ParryWindow);
        DefendCurrentHit(true); Assert.IsTrue(sequence.MoveNext());
        DefendCurrentHit(true); Assert.IsFalse(sequence.MoveNext());
    }
    [Test] public void FailedDefenseDoesNotTriggerDodgeSlowMotion()
    {
        var sequence = BeginEnemySequence(false);
        battle.AdvanceDefense(battle.CurrentImpact.Duration + 0.01f);
        float elapsed = battle.CurrentImpact.Elapsed; battle.AdvanceDefense(0.1f);
        Assert.AreEqual(0.1f, battle.CurrentImpact.Elapsed - elapsed, 0.0001f);
        battle.AdvanceDefense(0.5f); Assert.IsFalse(sequence.MoveNext());
    }
    [Test] public void AttackWaitsForDefenseCameraBeforeOpeningDefenseWindow()
    {
        var camera = battle.BattleCamera.GetComponent<GridCameraController>();
        var sequence = (System.Collections.IEnumerator)typeof(InSceneGridBattle)
            .GetMethod("EnemyAttack", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(battle, new object[] { 1, false });
        Assert.IsTrue(sequence.MoveNext()); Assert.IsTrue(camera.Transitioning); Assert.IsNull(battle.CurrentImpact);
        camera.AdvanceFocus(0.25f); Assert.IsTrue(sequence.MoveNext()); Assert.IsNull(battle.CurrentImpact);
        camera.AdvanceFocus(0.3f); Assert.IsTrue(sequence.MoveNext()); Assert.AreEqual(0, battle.CurrentImpact.Elapsed);
        DefendCurrentHit(false); Assert.IsFalse(sequence.MoveNext());
    }
    [Test] public void DefenderTurnsSmoothlyBeforeAttackTimerStarts()
    {
        battle.Actor(0).rotation = Quaternion.LookRotation(Vector3.back);
        var start = battle.Actor(0).rotation; var position = battle.Actor(0).position;
        var end = Quaternion.LookRotation(battle.Actor(1).position - position);
        var sequence = BeginEnemySequence(false);
        Assert.IsNull(battle.CurrentImpact); Assert.IsFalse(battle.DefenseActive);
        Assert.Less(Quaternion.Angle(start, battle.Actor(0).rotation), 0.001f);
        battle.AdvanceFacing(0.15f); Assert.IsTrue(sequence.MoveNext());
        Assert.Greater(Quaternion.Angle(start, battle.Actor(0).rotation), 1);
        Assert.Greater(Quaternion.Angle(end, battle.Actor(0).rotation), 1);
        Assert.IsNull(battle.CurrentImpact);
        battle.AdvanceFacing(0.15f); Assert.IsTrue(sequence.MoveNext());
        Assert.Less(Quaternion.Angle(end, battle.Actor(0).rotation), 0.001f);
        Assert.AreEqual(0, battle.CurrentImpact.Elapsed); Assert.AreEqual(position, battle.Actor(0).position);
        DefendCurrentHit(false); Assert.IsFalse(sequence.MoveNext());
    }
    [Test] public void EntryFacesEnemiesAlongGridDirections()
    {
        foreach (var obj in objects)
            if (obj.name.StartsWith("Test unit") && obj.transform != battle.Actor(0)) Assert.That(Mathf.Abs(Mathf.DeltaAngle(0, obj.transform.eulerAngles.y) % 45), Is.LessThan(0.01f));
    }
    [Test] public void OverheadBillboardsShowLiveHealthAndOnlyDefensePairDuringCameraTransition()
    {
        var root=Create("Overhead test");var type=System.Type.GetType("BattleOverheadDisplay, Assembly-CSharp",true);var display=root.AddComponent(type);
        type.GetMethod("Bind").Invoke(display,new object[]{battle,TMP_Settings.defaultFontAsset});
        battle.Info(0).Level=7;battle.Combatants[0].Health=31;
        type.GetMethod("Render").Invoke(display,null);Assert.AreEqual(battle.Combatants.Count,root.transform.childCount);
        StringAssert.Contains("Lv 7",root.transform.GetChild(0).Find("Name and level").GetComponent<TMP_Text>().text);
        Assert.AreEqual("31 / 100",root.transform.GetChild(0).Find("Health").GetComponentInChildren<TMP_Text>().text);
        battle.BattleCamera.transform.rotation=Quaternion.Euler(25,80,0);type.GetMethod("Render").Invoke(display,null);
        Assert.Less(Quaternion.Angle(battle.BattleCamera.transform.rotation,root.transform.GetChild(0).rotation),.01f);
        var sequence=BeginEnemySequence(false);Assert.IsTrue(battle.DefensePresentationActive);
        type.GetMethod("Render").Invoke(display,null);
        Assert.IsTrue(root.transform.GetChild(0).gameObject.activeSelf);Assert.IsTrue(root.transform.GetChild(1).gameObject.activeSelf);
        Assert.IsFalse(root.transform.GetChild(2).gameObject.activeSelf);
        typeof(InSceneGridBattle).GetMethod("ResetDefense",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(battle,null);
        Assert.IsFalse(battle.DefensePresentationActive);
        battle.BattleCamera.transform.LookAt(battle.Actor(2));type.GetMethod("Render").Invoke(display,null);
        Assert.IsTrue(root.transform.GetChild(2).gameObject.activeSelf);
        battle.Combatants[2].Health=0;type.GetMethod("Render").Invoke(display,null);Assert.IsFalse(root.transform.GetChild(2).gameObject.activeSelf);
    }
}
