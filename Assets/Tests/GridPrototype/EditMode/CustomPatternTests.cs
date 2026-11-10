using NUnit.Framework;
using UnityEngine;
using Game.GridPrototype;

public class CustomPatternTests
{
    [Test] public void CustomAimMaskConstrainsTargetsAndSurvivesSerialization()
    {
        var action = new BattleActionOption { Pattern = BattleTargetPattern.CustomSingle, Reach = 2 };
        action.DisabledPatternCells.Add(Vector2Int.right);
        action = JsonUtility.FromJson<BattleActionOption>(JsonUtility.ToJson(action));
        var board = new GridBoard(9, 9); var origin = new Vector2Int(4, 4);
        Assert.IsFalse(GridTargeting.CanTarget(board, origin, origin + Vector2Int.right, action));
        Assert.IsTrue(GridTargeting.CanTarget(board, origin, origin + Vector2Int.right * 2, action));
        Assert.IsFalse(GridTargeting.CanTarget(board, origin, origin + Vector2Int.right * 3, action));
        Assert.IsEmpty(GridTargeting.Footprint(board, origin, origin + Vector2Int.right, action));
        action.Reach = 1;
        Assert.IsFalse(GridTargeting.CanTarget(board, origin, origin + Vector2Int.right, action));
        Assert.IsFalse(GridTargeting.CanTarget(board, origin, origin + Vector2Int.right * 2, action));
    }

    [Test] public void CustomAreaUsesSeparateAimReachAndRotatesThreeCellSweep()
    {
        var action = new BattleActionOption { Pattern = BattleTargetPattern.CustomArea, Reach = 2, Radius = 1, RotateAreaTowardTarget = true };
        for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++)
            if (y != 0) action.DisabledPatternCells.Add(new Vector2Int(x, y));
        var board = new GridBoard(9, 9); var origin = new Vector2Int(4, 4);
        CollectionAssert.AreEquivalent(new[] { new Vector2Int(3, 5), new Vector2Int(4, 5), new Vector2Int(5, 5) },
            GridTargeting.Footprint(board, origin, origin + Vector2Int.up, action));
        CollectionAssert.AreEquivalent(new[] { new Vector2Int(5, 3), new Vector2Int(5, 4), new Vector2Int(5, 5) },
            GridTargeting.Footprint(board, origin, origin + Vector2Int.right, action));
        Assert.IsTrue(GridTargeting.CanTarget(board, origin, origin + Vector2Int.up * 2, action));
        Assert.IsFalse(GridTargeting.CanTarget(board, origin, origin + Vector2Int.up * 3, action));
    }

    [Test] public void CustomAreaClipsBoundsAndSightAndCanBeCompletelyDisabled()
    {
        var action = new BattleActionOption { Pattern = BattleTargetPattern.CustomArea, Reach = 2, Radius = 1 };
        var board = new GridBoard(4, 4);
        Assert.AreEqual(4, GridTargeting.Footprint(board, Vector2Int.one, Vector2Int.zero, action).Count);
        Assert.AreEqual(3, GridTargeting.Footprint(board, Vector2Int.one, Vector2Int.zero, action,
            (a, b) => b != Vector2Int.right).Count);
        for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++) action.DisabledPatternCells.Add(new Vector2Int(x, y));
        Assert.IsEmpty(GridTargeting.Footprint(board, Vector2Int.one, Vector2Int.zero, action));
    }
}
