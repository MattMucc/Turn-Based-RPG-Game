using System;
using System.Reflection;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using NUnit.Framework;
using UnityEngine;
using Tenebrasia.GridPrototype;

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
        battle.Initialize(grid, figures, new float[cells.Length], cells, camera, Shader.Find("Tenebrasia/GridOverlay"), victory => { });
    }
    [TearDown] public void Cleanup()
    {
        for (int i = objects.Count - 1; i >= 0; i--) if (objects[i]) Object.DestroyImmediate(objects[i]);
        objects.Clear(); Physics.SyncTransforms();
    }
    [Test] public void CancelAndInvalidTargetPreserveActionMovementAndItem()
    {
        var item = battle.Loadout.Options.Find(o => o.Category == BattleActionCategory.Item);
        Assert.IsTrue(battle.ChooseAction(item));
        Assert.IsFalse(battle.ToggleTarget(Vector2Int.zero)); Assert.IsFalse(battle.CommitAction());
        battle.ToggleTarget(cells[1]); battle.CancelAction();
        Assert.IsTrue(battle.ActionReady); Assert.AreEqual(3, battle.Movement); Assert.AreEqual(3, item.Quantity);
    }
    [Test] public void MultiTargetCapsTogglesAndDamagesOnlyChosenEnemies()
    {
        var action = battle.Loadout.Options.Find(o => o.Pattern == BattleTargetPattern.Multiple);
        battle.ChooseAction(action);
        for (int i = 1; i <= 3; i++) Assert.IsTrue(battle.ToggleTarget(cells[i]));
        Assert.IsFalse(battle.ToggleTarget(cells[4])); Assert.AreEqual(3, battle.TargetCount);
        battle.ToggleTarget(cells[2]); Assert.AreEqual(2, battle.TargetCount);
        Assert.IsTrue(battle.CommitAction());
        Assert.AreEqual(88, battle.Grid.Board.Units[1].Health);
        Assert.AreEqual(100, battle.Grid.Board.Units[2].Health);
        Assert.AreEqual(88, battle.Grid.Board.Units[3].Health);
        Assert.AreEqual(100, battle.Grid.Board.Units[4].Health);
        Assert.IsFalse(battle.ActionReady); Assert.AreEqual(3, battle.Movement);
    }
    [Test] public void ItemConsumesExactlyOnceAndDamageMatchesAreaPreview()
    {
        var action = battle.Loadout.Options.Find(o => o.Category == BattleActionCategory.Item);
        battle.ChooseAction(action); battle.ToggleTarget(cells[1]);
        var affected = GridTargeting.Footprint(battle.Grid.Board, cells[0], cells[1], action);
        Assert.IsTrue(battle.CommitAction()); Assert.IsFalse(battle.CommitAction()); Assert.AreEqual(2, action.Quantity);
        for (int i = 1; i < cells.Length; i++) Assert.AreEqual(affected.Contains(cells[i]) ? 80 : 100, battle.Grid.Board.Units[i].Health);
    }
    [Test] public void TargetBecomingInvalidPreventsAllCosts()
    {
        var action = battle.Loadout.Options[0]; battle.ChooseAction(action); battle.ToggleTarget(cells[1]);
        battle.Grid.Board.Units[1].Position = Vector2Int.zero;
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
                if (button.GetComponentInChildren<TMP_Text>().text == label) return button;
            Assert.Fail("Missing active button: " + label); return null;
        }
        menuType.GetMethod("Bind").Invoke(menu, new object[] { battle });
        Assert.AreEqual(7, root.GetComponentsInChildren<Button>().Length);
        Find("Skills").onClick.Invoke();
        Assert.AreEqual(4, root.GetComponentsInChildren<Button>().Length);
        Find("Multi-target Skill").onClick.Invoke();
        Assert.IsFalse(Find("Area Skill").interactable);
        Assert.IsTrue(Find("Multi-target Skill").interactable);
        Assert.IsFalse(Find("Confirm").interactable);
        Assert.IsTrue(battle.KeyboardGridInputEnabled);
        Find("Back").onClick.Invoke();
        Assert.IsNull(battle.ChosenAction); Assert.IsTrue(battle.ActionReady);
        Assert.IsTrue(Find("Area Skill").interactable);
        Find("Back").onClick.Invoke(); Assert.AreEqual(7, root.GetComponentsInChildren<Button>().Length);
        menuType.GetMethod("Unbind").Invoke(menu, null); Assert.IsFalse(root.activeSelf);
        menuType.GetMethod("Bind").Invoke(menu, new object[] { battle }); Assert.IsTrue(root.activeSelf);
        Assert.AreEqual(7, root.GetComponentsInChildren<Button>().Length);
        menuType.GetMethod("Unbind").Invoke(menu, null);
    }
    [Test] public void EntryFacesActorsAlongGridDirections()
    {
        foreach (var obj in objects)
            if (obj.name.StartsWith("Test unit")) Assert.That(Mathf.Abs(Mathf.DeltaAngle(0, obj.transform.eulerAngles.y) % 45), Is.LessThan(0.01f));
    }
}
