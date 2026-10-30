using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tenebrasia.GridPrototype
{
    public enum BattleActionCategory { Attack, Skill, Item }
    public enum BattleTargetPattern { Single, Area, Piercing, Multiple }

    [Serializable]
    public sealed class BattleActionOption
    {
        public string Name;
        [TextArea] public string Description;
        public BattleActionCategory Category;
        public BattleTargetPattern Pattern;
        [Min(1)] public int Reach = 1;
        [Min(0)] public int Radius = 1;
        [Min(1)] public int MaxTargets = 3;
        [Min(0)] public int Damage = 25;
        [Min(0)] public int Quantity;
        public bool Available => Category != BattleActionCategory.Item || Quantity > 0;
        public string Tooltip => $"{Name}\n{Description}\n\n{Reach} tile(s) of reach · 1 action · {Damage} damage per enemy\n"
            + (Pattern == BattleTargetPattern.Area ? $"Area: {Radius}-tile radius around the target. Enemies only." :
               Pattern == BattleTargetPattern.Piercing ? "Pierces a straight or diagonal line, stopping at terrain obstruction." :
               Pattern == BattleTargetPattern.Multiple ? $"Choose up to {MaxTargets} different enemies." : "One enemy.")
            + (Category == BattleActionCategory.Item ? $"\nCarried: {Quantity}. Consumes one on confirmation." : "")
            + "\nBlue: reach. Red: affected tiles. No accuracy/status roll in this prototype.";
    }

    // Carried quantities belong to the character, not a newly created encounter.
    // These are demonstration options, not approved character kits or balance.
    public sealed class BattleActionLoadout : MonoBehaviour
    {
        public List<BattleActionOption> Options = new List<BattleActionOption>
        {
            new BattleActionOption { Name = "Strike", Description = "Prototype adjacent attack." },
            new BattleActionOption { Name = "Piercing Attack", Description = "Prototype line targeting.", Pattern = BattleTargetPattern.Piercing, Reach = 4, Damage = 18 },
            new BattleActionOption { Name = "Area Skill", Description = "Prototype area targeting.", Category = BattleActionCategory.Skill, Pattern = BattleTargetPattern.Area, Reach = 4, Damage = 18 },
            new BattleActionOption { Name = "Multi-target Skill", Description = "Prototype independent target selection.", Category = BattleActionCategory.Skill, Pattern = BattleTargetPattern.Multiple, Reach = 4, Damage = 12 },
            new BattleActionOption { Name = "Blast Flask", Description = "Prototype offensive consumable.", Category = BattleActionCategory.Item, Pattern = BattleTargetPattern.Area, Reach = 3, Damage = 20, Quantity = 3 }
        };
    }

    public static class GridTargeting
    {
        public static Quaternion Facing(Vector3 direction)
        {
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            return Quaternion.Euler(0, Mathf.Round(yaw / 45f) * 45f, 0);
        }

        public static int Distance(Vector2Int a, Vector2Int b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

        // Supercover line: diagonals cannot shoot through two touching wall corners.
        public static bool Clear(GridBoard board, Vector2Int from, Vector2Int to)
        {
            int steps = Distance(from, to);
            Vector2Int last = from;
            for (int i = 1; i <= steps; i++)
            {
                var p = Vector2Int.RoundToInt(Vector2.Lerp(from, to, i / (float)steps));
                if (!board.Contains(p) || board.At(p).Ground == Ground.Obstacle) return false;
                if (p.x != last.x && p.y != last.y &&
                    (board.At(new Vector2Int(last.x, p.y)).Ground == Ground.Obstacle ||
                     board.At(new Vector2Int(p.x, last.y)).Ground == Ground.Obstacle)) return false;
                last = p;
            }
            return true;
        }

        public static bool CanTarget(GridBoard board, Vector2Int from, Vector2Int to, BattleActionOption action,
            Func<Vector2Int, Vector2Int, bool> sight = null)
        {
            int distance = Distance(from, to);
            if (distance < 1 || distance > action.Reach || !board.Contains(to)) return false;
            var d = to - from;
            if (action.Pattern == BattleTargetPattern.Piercing && d.x != 0 && d.y != 0 && Mathf.Abs(d.x) != Mathf.Abs(d.y)) return false;
            return Clear(board, from, to) && (sight == null || sight(from, to));
        }

        public static HashSet<Vector2Int> Footprint(GridBoard board, Vector2Int from, Vector2Int target,
            BattleActionOption action, Func<Vector2Int, Vector2Int, bool> sight = null)
        {
            var result = new HashSet<Vector2Int>();
            if (!CanTarget(board, from, target, action, sight)) return result;
            if (action.Pattern == BattleTargetPattern.Piercing)
            {
                var delta = target - from;
                var step = new Vector2Int(Math.Sign(delta.x), Math.Sign(delta.y));
                for (int i = 1; i <= action.Reach; i++)
                {
                    var p = from + step * i;
                    if (!CanTarget(board, from, p, action, sight)) break;
                    result.Add(p);
                }
            }
            else if (action.Pattern == BattleTargetPattern.Area)
            {
                for (int x = -action.Radius; x <= action.Radius; x++) for (int y = -action.Radius; y <= action.Radius; y++)
                {
                    var p = target + new Vector2Int(x, y);
                    if (board.Contains(p) && Clear(board, target, p) && (sight == null || sight(target, p))) result.Add(p);
                }
            }
            else result.Add(target);
            return result;
        }
    }
}
