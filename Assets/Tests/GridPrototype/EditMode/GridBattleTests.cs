using NUnit.Framework;
using UnityEngine;
using Tenebrasia.GridPrototype;

public class GridBattleTests
{
    private GridBoard board;
    private GridUnit unit;

    [SetUp]
    public void SetUp()
    {
        board = new GridBoard(6, 6);
        unit = new GridUnit("Test", new Vector2Int(1, 1));
        board.Units.Add(unit); unit.BeginTurn();
    }

    [Test] public void MovementDoesNotSpendAction()
    {
        Assert.IsTrue(board.TryMove(unit, new Vector2Int(2, 1), out _));
        Assert.AreEqual(2, unit.Movement); Assert.IsTrue(unit.ActionAvailable);
    }
    [Test] public void ActionStillAllowsMovementAndCannotRepeat()
    {
        Assert.IsTrue(unit.SpendAction()); Assert.IsFalse(unit.SpendAction());
        Assert.IsTrue(unit.TurnOpen);
        Assert.IsTrue(board.TryMove(unit, new Vector2Int(2, 1), out _));
        Assert.AreEqual(2, unit.Movement);
    }
    [Test] public void TwoBankedPlusThreeEqualsFiveOnNextTurn()
    {
        board.TryMove(unit, new Vector2Int(2, 1), out _);
        unit.EndTurn(); Assert.IsFalse(unit.SpendMovement(1));
        unit.BeginTurn(); Assert.AreEqual(5, unit.Movement); Assert.IsTrue(unit.ActionAvailable);
    }
    [Test] public void CarryoverIsPerUnitAndUncapped()
    {
        var other = new GridUnit("Other", Vector2Int.zero);
        for (int i = 0; i < 5; i++) { unit.EndTurn(); unit.BeginTurn(); }
        other.BeginTurn(); Assert.AreEqual(18, unit.Movement); Assert.AreEqual(3, other.Movement);
    }
    [Test] public void MovementGrantCanBeSuppressedWithoutDeletingBank()
    {
        unit.EndTurn(); unit.BeginTurn(0); Assert.AreEqual(3, unit.Movement);
    }
    [Test] public void DuplicateBeginCannotFarmMovement()
    { Assert.Throws<System.InvalidOperationException>(() => unit.BeginTurn()); }
    [Test] public void EmptyBudgetsStillRequireExplicitEndTurn()
    {
        unit.SpendMovement(3); unit.SpendAction(); Assert.IsTrue(unit.TurnOpen);
        unit.EndTurn(); Assert.IsFalse(unit.TurnOpen);
    }
    [Test] public void DiagonalCostsOne()
    {
        Assert.IsTrue(board.TryMove(unit, new Vector2Int(2, 2), out var route));
        Assert.AreEqual(1, route.Cost); Assert.AreEqual(2, unit.Movement);
    }
    [TestCase(3, 1, 2, 1)]
    [TestCase(1, 3, 1, 2)]
    public void StraightRouteWinsEqualCostZigzag(int x, int z, int middleX, int middleZ)
    {
        var route = board.Reachable(unit, 3)[new Vector2Int(x, z)];
        Assert.AreEqual(2, route.Cost);
        Assert.AreEqual(new Vector2Int(middleX, middleZ), route.Stops[1]);
    }
    [Test] public void CannotCutObstacleCorner()
    {
        board.Cells[2, 1].Ground = Ground.Obstacle;
        Assert.IsFalse(board.CanTraverse(unit, unit.Position, new Vector2Int(2, 2)));
    }
    [Test] public void OccupiedCellBlocksLandingAndDiagonalCorner()
    {
        board.Units.Add(new GridUnit("Other", new Vector2Int(2, 1)));
        Assert.IsFalse(board.CanTraverse(unit, unit.Position, new Vector2Int(2, 1)));
        Assert.IsFalse(board.CanTraverse(unit, unit.Position, new Vector2Int(2, 2)));
    }
    [Test] public void SingleWaterCellJumpCostsTwo()
    {
        for (int z = 0; z < 6; z++) board.Cells[2, z].Ground = Ground.Water;
        Assert.IsTrue(board.TryMove(unit, new Vector2Int(3, 1), out var route));
        Assert.AreEqual(2, route.Cost); Assert.AreEqual(1, unit.Movement);
    }
    [Test] public void TwoCellGapNeedsRangeThreeEvenWithBankedMovement()
    {
        for (int z = 0; z < 6; z++) { board.Cells[2, z].Ground = Ground.Water; board.Cells[3, z].Ground = Ground.Water; }
        unit.EndTurn(); unit.BeginTurn();
        Assert.IsFalse(board.TryMove(unit, new Vector2Int(4, 1), out _));
        unit.JumpRange = 3;
        Assert.IsTrue(board.TryMove(unit, new Vector2Int(4, 1), out var route)); Assert.AreEqual(3, route.Cost);
    }
    [Test] public void RangeDoesNotReplaceMovementBudget()
    {
        for (int z = 0; z < 6; z++) board.Cells[2, z].Ground = Ground.Water;
        unit.SpendMovement(2);
        Assert.IsFalse(board.TryMove(unit, new Vector2Int(3, 1), out _)); Assert.AreEqual(1, unit.Movement);
    }
    [Test] public void JumpHeightIsIndependentOfHorizontalRange()
    {
        board.Cells[2, 1].Height = 2;
        Assert.IsFalse(board.CanTraverse(unit, unit.Position, new Vector2Int(2, 1)));
        unit.JumpHeight = 2; Assert.IsTrue(board.CanTraverse(unit, unit.Position, new Vector2Int(2, 1)));
    }
    [Test] public void WaterObstacleOutOfBoundsAndSameCellAreInvalidDestinations()
    {
        board.Cells[2, 1].Ground = Ground.Water; board.Cells[1, 2].Ground = Ground.Obstacle;
        Assert.IsFalse(board.TryMove(unit, new Vector2Int(2, 1), out _));
        Assert.IsFalse(board.TryMove(unit, new Vector2Int(1, 2), out _));
        Assert.IsFalse(board.TryMove(unit, new Vector2Int(-1, 1), out _));
        Assert.IsFalse(board.TryMove(unit, unit.Position, out _)); Assert.AreEqual(3, unit.Movement);
    }
    [Test] public void PathRoutesAroundObstacleWithinBudget()
    {
        board.Cells[2, 1].Ground = Ground.Obstacle;
        Assert.IsFalse(board.Reachable(unit, 3).ContainsKey(new Vector2Int(3, 1)));
        var routes = board.Reachable(unit, 4);
        Assert.AreEqual(4, routes[new Vector2Int(3, 1)].Cost);
    }
    [Test] public void HazardOnlyAppliesOnEnteredCells()
    {
        board.Cells[2, 1].Ground = Ground.Hazard;
        board.Reachable(unit, 3); Assert.AreEqual(100, unit.Health);
        board.TryMove(unit, new Vector2Int(2, 1), out _); Assert.AreEqual(90, unit.Health);
        Assert.IsFalse(board.TryMove(unit, unit.Position, out _)); Assert.AreEqual(90, unit.Health);
    }
}
