using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Game.GridPrototype;
using Object = UnityEngine.Object;

public class BattleEntryTests
{
    readonly List<GameObject> owned = new();
    readonly Vector3 origin = new(90000, 0, 90000);
    TerrainBattleGrid grid;
    GameObject Make(string name) { var obj = new GameObject(name); owned.Add(obj); return obj; }
    [SetUp] public void Setup()
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); owned.Add(floor);
        floor.transform.position = origin - Vector3.up * .5f; floor.transform.localScale = new Vector3(30, 1, 30);
        Physics.SyncTransforms(); grid = new TerrainBattleGrid(origin, 10, 10, 2, ~0, new Collider[0]);
    }
    [TearDown] public void Cleanup() { for (int i = owned.Count - 1; i >= 0; i--) if (owned[i]) Object.DestroyImmediate(owned[i]); owned.Clear(); }
    Vector3[] Feet() => new[] { origin, origin, origin, origin + Vector3.forward * 2 };
    bool Plan(out BattleEntryPlan plan, IReadOnlyList<Vector3?> markers = null) => BattleFormation.TryPlan(grid, Feet(), 3, Vector3.right, markers, out plan);
    [Test] public void FormationFacesEnemyRatherThanExplorationHeadingAndPreservesEnemyCell()
    {
        Assert.IsTrue(Plan(out var plan));
        CollectionAssert.AreEqual(new[] { new Vector2Int(5, 5), new Vector2Int(4, 4), new Vector2Int(6, 4), new Vector2Int(5, 6) }, plan.Cells);
        Assert.AreEqual(Vector3.forward, plan.Facing); Assert.AreEqual(4, new HashSet<Vector2Int>(plan.Cells).Count);
        Assert.IsEmpty(grid.Board.Units);
    }
    [Test] public void NarrowPassageUsesSingleFileWithoutCrossingBlockedCorners()
    {
        for (int x = 0; x < 10; x++) for (int z = 0; z < 10; z++) if (x != 5) grid.Board.Cells[x, z].Ground = Ground.Obstacle;
        Assert.IsTrue(Plan(out var plan));
        Assert.AreEqual(new Vector2Int(5, 4), plan.Cells[1]); Assert.AreEqual(new Vector2Int(5, 3), plan.Cells[2]);
        foreach (var point in plan.Routes[2]) Assert.AreEqual(origin.x, point.x, .001f);
    }
    [Test] public void CompanionsCannotCrossRiverEvenWhenMarkerRequestsOtherBank()
    {
        for (int x = 0; x < 10; x++) grid.Board.Cells[x, 4].Ground = Ground.Water;
        Assert.IsTrue(Plan(out var plan, new Vector3?[] { grid.Point(new Vector2Int(5, 3)), null }));
        Assert.GreaterOrEqual(plan.Cells[1].y, 5); Assert.GreaterOrEqual(plan.Cells[2].y, 5);
    }
    [Test] public void FormationAvoidsHazardsAndHonorsReachableAuthoredPreference()
    {
        grid.Board.Cells[4, 4].Ground = Ground.Hazard;
        var marker = new Vector2Int(7, 5);
        Assert.IsTrue(Plan(out var plan, new Vector3?[] { grid.Point(marker), null }));
        Assert.AreEqual(marker, plan.Cells[1]); Assert.AreEqual(Ground.Land, grid.Board.At(plan.Cells[2]).Ground);
    }
    [Test] public void InsufficientRoomFailsWithoutPartialPlacement()
    {
        for (int x = 0; x < 10; x++) for (int z = 0; z < 10; z++) grid.Board.Cells[x, z].Ground = Ground.Obstacle;
        grid.Board.Cells[5, 5].Ground = grid.Board.Cells[5, 6].Ground = Ground.Land;
        Assert.IsFalse(Plan(out var plan)); Assert.IsNull(plan); Assert.IsEmpty(grid.Board.Units);
    }
    [Test] public void ExpandedGridIncludesSpreadOutEnemiesWithoutPullingThemTowardCaptain()
    {
        owned[0].transform.localScale = new Vector3(100, 1, 100); Physics.SyncTransforms();
        var feet = new[] { origin + new Vector3(-18, 0, -8), Vector3.zero, Vector3.zero, origin + new Vector3(18, 0, 28) };
        Assert.IsTrue(BattleFormation.TryGridBounds(feet, 3, 2, 14, 12, null, out var center, out int width, out int depth));
        Assert.Greater(width, 14); Assert.Greater(depth, 12);
        var expanded = new TerrainBattleGrid(center, width, depth, 2, ~0, new Collider[0]);
        Assert.IsTrue(BattleFormation.TryPlan(expanded, feet, 3, Vector3.forward, null, out var plan, out string failure), failure);
        Assert.That(Vector3.Distance(feet[3], expanded.Point(plan.Cells[3])), Is.LessThan(.01f));
        Assert.That(Vector3.Distance(feet[0], expanded.Point(plan.Cells[0])), Is.LessThan(.01f));
        Assert.AreEqual(2f, plan.Duration);
    }
    [Test] public void PlacementFailureIdentifiesEnemyInsteadOfBlamingPartySpace()
    {
        var feet = Feet(); feet[3] = origin + Vector3.right * 100;
        Assert.IsFalse(BattleFormation.TryPlan(grid, feet, 3, Vector3.forward, null, out _, out string failure));
        StringAssert.StartsWith("Enemy 1", failure);
        feet[3] = origin + Vector3.right * 1000;
        Assert.IsFalse(BattleFormation.TryGridBounds(feet, 3, 2, 14, 12, null, out _, out _, out _));
    }
    [Test] public void FollowingPartyEntersFromActualPositionsWithoutFormationOrReveal()
    {
        var feet = new[] { origin, origin + Vector3.left * 6, origin + Vector3.back * 8, origin + Vector3.forward * 2 };
        Assert.IsTrue(BattleFormation.TryGridBounds(feet, 3, 2, 6, 6, null, out var center, out int width, out int depth, true));
        var terrain = new TerrainBattleGrid(center, width, depth, 2, ~0, new Collider[0]);
        Assert.IsTrue(BattleFormation.TryPlan(terrain, feet, 3, Vector3.forward, null, out var plan, out string failure, true), failure);
        var figures = new List<Transform>();
        for (int i = 0; i < feet.Length; i++)
        {
            Assert.That(Vector3.Distance(feet[i], terrain.Point(plan.Cells[i])), Is.LessThan(.01f));
            var obj = Make("World party " + i); obj.transform.position = feet[i]; figures.Add(obj.transform);
        }
        var camera = Make("Source").AddComponent<Camera>(); var battle = Make("Battle").AddComponent<InSceneGridBattle>();
        battle.Initialize(terrain, figures, new float[4], plan.Cells, camera, Shader.Find("Game/GridOverlay"), _ => {}, false, 3, plan);
        Assert.IsTrue(figures[1].gameObject.activeSelf); Assert.AreEqual(feet[1], figures[1].position);
        battle.AdvanceEntry(.1f); Assert.IsTrue(figures[2].gameObject.activeSelf); Assert.AreEqual(feet[2], figures[2].position);
    }
    [Test] public void SettlingCannotSlideThroughPhysicalWall()
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); owned.Add(wall);
        wall.transform.position = origin + new Vector3(1, 1, 0); wall.transform.localScale = new Vector3(.1f, 2, 3);
        Physics.SyncTransforms();
        Assert.IsFalse(grid.ClearEntryApproach(origin, new Vector2Int(6, 5)));
    }
    [TestCase(false)] [TestCase(true)]
    public void EntryBlendsCameraAndDefersAllTurnResourcesAndStatusTicks(bool preemptive)
    {
        Assert.IsTrue(Plan(out var plan)); plan.Duration = 1;
        var figures = new List<Transform>();
        for (int i = 0; i < 4; i++)
        {
            var actor = Make("Entry actor " + i); actor.transform.position = Feet()[i]; figures.Add(actor.transform);
            actor.AddComponent<BattleCharacterInfo>().Initiative = i == 3 ? 100 : 10;
        }
        var source = Make("Exploration camera").AddComponent<Camera>();
        source.transform.position = origin + new Vector3(0, 3, -4); source.transform.rotation = Quaternion.Euler(12, 0, 0);
        var battle = Make("Entry battle").AddComponent<InSceneGridBattle>();
        battle.Initialize(grid, figures, new float[4], plan.Cells, source, Shader.Find("Game/GridOverlay"), _ => {}, preemptive, 3, plan);
        int first = preemptive ? 0 : 3;
        Assert.AreEqual(first, battle.CurrentActorIndex); Assert.IsTrue(battle.EntryActive); Assert.IsFalse(battle.PlayerReady);
        Assert.AreEqual(source.transform.position, battle.BattleCamera.transform.position);
        battle.Combatants[first].ApplyStatus(CombatStatus.Burning, 2);
        battle.AdvanceEntry(.5f);
        Assert.AreNotEqual(source.transform.position, battle.BattleCamera.transform.position);
        Assert.IsFalse(battle.Combatants[first].TurnOpen); Assert.AreEqual(0, battle.Combatants[first].Movement);
        Assert.AreEqual(2, battle.Combatants[first].Stacks(CombatStatus.Burning));
        battle.AdvanceEntry(.5f); Assert.IsFalse(battle.EntryActive);
        Assert.IsTrue(battle.InteractionBlocked, "The input that finishes entry cannot also issue a battle command.");
        Assert.AreEqual(3, battle.Combatants[first].Movement); Assert.IsTrue(battle.Combatants[first].ActionAvailable);
        Assert.AreEqual(1, battle.Combatants[first].Stacks(CombatStatus.Burning));
        for (int i = 0; i < 4; i++) Assert.That(Vector3.Distance(grid.Point(plan.Cells[i]), figures[i].position), Is.LessThan(.001f));
        battle.AdvanceEntry(10); Assert.AreEqual(3, battle.Combatants[first].Movement);
    }
}
