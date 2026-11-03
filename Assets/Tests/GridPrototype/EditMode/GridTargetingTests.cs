using NUnit.Framework;
using UnityEngine;
using Game.GridPrototype;

public class GridTargetingTests
{
    [TestCase(60, 45)] [TestCase(100, 90)] [TestCase(170, 180)] [TestCase(225, 225)] [TestCase(350, 0)]
    public void FacingSnapsToEightGridDirections(float original, float expected)
    {
        var direction = Quaternion.Euler(0, original, 0) * Vector3.forward;
        Assert.That(Quaternion.Angle(GridTargeting.Facing(direction), Quaternion.Euler(0, expected, 0)), Is.LessThan(0.01f));
    }
    [Test] public void RangeAllowsSelfButRejectsOutOfBoundsAndZeroLengthPiercing()
    {
        var board = new GridBoard(8, 8); var action = new BattleActionOption { Reach = 2 };
        Assert.IsTrue(GridTargeting.CanTarget(board, Vector2Int.zero, new Vector2Int(2, 2), action));
        Assert.IsFalse(GridTargeting.CanTarget(board, Vector2Int.zero, new Vector2Int(3, 0), action));
        Assert.IsTrue(GridTargeting.CanTarget(board, Vector2Int.zero, Vector2Int.zero, action));
        action.Pattern = BattleTargetPattern.Piercing;
        Assert.IsFalse(GridTargeting.CanTarget(board, Vector2Int.zero, Vector2Int.zero, action));
        Assert.IsFalse(GridTargeting.CanTarget(board, Vector2Int.zero, new Vector2Int(-1, 0), action));
    }
    [Test] public void PiercingStopsAtWallAndRejectsNonAlignedTargets()
    {
        var board = new GridBoard(8, 8); var action = new BattleActionOption { Reach = 6, Pattern = BattleTargetPattern.Piercing };
        board.Cells[4, 0].Ground = Ground.Obstacle;
        var footprint = GridTargeting.Footprint(board, Vector2Int.zero, Vector2Int.right, action);
        CollectionAssert.AreEquivalent(new[] { new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0) }, footprint);
        Assert.IsFalse(GridTargeting.CanTarget(board, Vector2Int.zero, new Vector2Int(2, 1), action));
    }
    [Test] public void DiagonalAttackCannotCutBlockedCorner()
    {
        var board = new GridBoard(4, 4); board.Cells[1, 0].Ground = Ground.Obstacle;
        Assert.IsFalse(GridTargeting.CanTarget(board, Vector2Int.zero, Vector2Int.one, new BattleActionOption()));
    }
    [Test] public void AreaClipsToBoardAndHonorsPhysicalSight()
    {
        var board = new GridBoard(4, 4); var action = new BattleActionOption { Reach = 4, Pattern = BattleTargetPattern.Area };
        var result = GridTargeting.Footprint(board, new Vector2Int(3, 3), Vector2Int.zero, action);
        Assert.AreEqual(4, result.Count);
        Assert.IsEmpty(GridTargeting.Footprint(board, new Vector2Int(3, 3), Vector2Int.zero, action, (a, b) => false));
    }
    [TestCase(0, 1)] [TestCase(0, -1)] [TestCase(1, 0)] [TestCase(-1, 0)]
    public void SweepRotatesThreeWideRowWithoutHittingCaster(int x, int y)
    {
        var board = new GridBoard(9, 9); var origin = new Vector2Int(4, 4);
        var aim = origin + new Vector2Int(x, y) * 2;
        var side = new Vector2Int(y, -x);
        var action = new BattleActionOption { Reach = 2, Radius = 1, Pattern = BattleTargetPattern.Sweep };
        CollectionAssert.AreEquivalent(new[] { aim - side, aim, aim + side }, GridTargeting.Footprint(board, origin, aim, action));
        Assert.IsFalse(GridTargeting.CanTarget(board, origin, origin, action));
        Assert.IsFalse(GridTargeting.CanTarget(board, origin, origin + Vector2Int.one, action));
    }
}
