using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Tenebrasia.GridPrototype;

public class TerrainBattleGridTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private readonly Vector3 origin = new Vector3(10000, 0, 10000);
    private GameObject Box(Vector3 localPosition, Vector3 scale)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); objects.Add(obj);
        obj.transform.position = origin + localPosition; obj.transform.localScale = scale; return obj;
    }
    private void Floor() => Box(new Vector3(0, -0.5f, 0), new Vector3(30, 1, 30));
    private TerrainBattleGrid Sample(params Collider[] ignored)
    {
        Physics.SyncTransforms();
        return new TerrainBattleGrid(origin, 6, 6, 2, ~0, ignored);
    }
    [TearDown] public void CleanUp()
    { foreach (var obj in objects) if (obj) Object.DestroyImmediate(obj); objects.Clear(); Physics.SyncTransforms(); }

    [Test] public void FlatGroundPlacesEveryActorOnceNearTheirWorldPositions()
    {
        Floor(); var grid = Sample();
        Assert.IsTrue(grid.TryPlace(new[] { origin + Vector3.up, origin + Vector3.up, origin + Vector3.right * 4 }, out var cells));
        Assert.AreEqual(3, new HashSet<Vector2Int>(cells).Count);
        Assert.That(Vector3.Distance(grid.Point(cells[0]), origin), Is.LessThan(0.1f));
        foreach (var cell in cells) Assert.That(grid.Point(cell).y, Is.EqualTo(0).Within(0.01f));
    }
    [Test] public void MissingGroundFailsWithoutPartialPlacement()
    {
        var grid = Sample(); Assert.IsFalse(grid.TryPlace(new[] { origin }, out var cells)); Assert.IsEmpty(cells);
    }
    [Test] public void TooManyActorsFailBeforeAnyPlacement()
    {
        Floor(); var grid = Sample(); var desired = new Vector3[37];
        for (int i = 0; i < desired.Length; i++) desired[i] = origin;
        Assert.IsFalse(grid.TryPlace(desired, out var cells)); Assert.IsEmpty(cells);
    }
    [Test] public void ParticipantCollidersAreExcludedFromSampling()
    {
        Floor(); var actor = Box(new Vector3(0, 1, 0), new Vector3(1, 2, 1));
        var grid = Sample(actor.GetComponent<Collider>());
        Assert.AreEqual(Ground.Land, grid.Board.Cells[3, 3].Ground);
        Assert.That(grid.Point(new Vector2Int(3, 3)).y, Is.EqualTo(0).Within(0.01f));
    }
    [Test] public void LowCeilingRejectsLanding()
    {
        Floor();
        // Exclude ceiling from floor selection, but not from physical body clearance.
        var ceiling = Box(new Vector3(0, 1.6f, 0), new Vector3(8, 0.2f, 8)); ceiling.layer = 2;
        Physics.SyncTransforms();
        var grid = new TerrainBattleGrid(origin, 6, 6, 2, 1 << 0, new Collider[0]);
        Assert.AreEqual(Ground.Obstacle, grid.Board.Cells[3, 3].Ground);
    }
    [Test] public void ThinWallBetweenValidCellCentersBlocksTraversal()
    {
        Floor(); Box(new Vector3(0.7f, 1.5f, 0), new Vector3(0.1f, 3, 20)); var grid = Sample();
        var a = new Vector2Int(3, 3); var b = new Vector2Int(4, 3); var unit = new GridUnit("Test", a);
        Assert.AreEqual(Ground.Land, grid.Board.At(a).Ground);
        Assert.AreEqual(Ground.Land, grid.Board.At(b).Ground);
        Assert.IsFalse(grid.Board.CanTraverse(unit, a, b));
    }
    [Test] public void DisconnectedIslandsCannotSplitTheEncounter()
    {
        Box(new Vector3(0, -0.5f, 0), new Vector3(2, 1, 2));
        Box(new Vector3(4, -0.5f, 0), new Vector3(2, 1, 2));
        var grid = Sample();
        Assert.IsFalse(grid.TryPlace(new[] { origin, origin + Vector3.right * 4 }, out var cells)); Assert.IsEmpty(cells);
    }
    [Test] public void WorldCoordinatesMapBackToSameGridCell()
    {
        Floor(); var grid = Sample();
        Assert.IsTrue(grid.TryCell(origin, out var cell)); Assert.AreEqual(new Vector2Int(3, 3), cell);
        Assert.IsFalse(grid.TryCell(origin + Vector3.right * 50, out _));
    }
    [Test] public void FractionalElevationCannotRoundAwayJumpLimit()
    {
        var board = new GridBoard(3, 1);
        board.Cells[1, 0].SampledHeight = 1.3f;
        Assert.IsFalse(board.CanTraverse(new GridUnit("Test", Vector2Int.zero), Vector2Int.zero, new Vector2Int(1, 0)));
    }
}
