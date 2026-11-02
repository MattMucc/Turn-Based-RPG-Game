using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GridPrototype
{
    public enum BattleActionCategory { Attack, Skill, Item }
    public enum BattleTargetPattern { Single, Area, Piercing, Multiple }

    [Serializable]
    public sealed class BattleActionOption
    {
        public string Name;
        [Range(-1, 3)] public int FavoriteSlot = -1;
        [TextArea] public string Description;
        public BattleActionCategory Category;
        public BattleTargetPattern Pattern;
        [Min(1)] public int Reach = 1;
        [Min(0)] public int Radius = 1;
        [Min(1)] public int MaxTargets = 3;
        [Min(0)] public int Damage = 25;
        [Min(0)] public int Quantity;
        public bool Available => Category != BattleActionCategory.Item || Quantity > 0;
        public string Tooltip => $"<b>{Name}</b>\n{Description}\n\n<b>Reach</b>   {Reach} tiles from the user\n<b>Damage</b>   {Damage} per hit\n<b>Cost</b>   1 action\n\n<b>Targets</b>\n"
            + (Pattern == BattleTargetPattern.Area ? $"Everyone within {Radius} tile(s) of the chosen cell. Allies and destructible objects are also hit." :
               Pattern == BattleTargetPattern.Piercing ? "Hits characters and destructible objects in a straight or diagonal line, including allies. Stops at terrain." :
               Pattern == BattleTargetPattern.Multiple ? $"Assign {MaxTargets} hits to start. Repeated cells and allies are allowed. Hits resolve in selection order." : "One cell. One hit. Can hit allies or destructible objects.")
            + (Category == BattleActionCategory.Item ? $"\n\n<b>Carried</b>   {Quantity}\nUses one item when the action starts." : "")
            + "\nEmpty cells still spend the action.\n\n<color=#80BEFF>Blue tiles</color>: within reach\n<color=#FF9090>Red tiles</color>: affected by this action";

    }

    // Carried quantities belong to the character, not a newly created encounter.
    // These are demonstration options, not approved character kits or balance.
    public sealed class BattleActionLoadout : MonoBehaviour
    {
        [SerializeField] private bool favoritesInitialized;
        public void InitializeFavorites()
        {
            if (favoritesInitialized) return;
            favoritesInitialized = true;
            foreach (BattleActionCategory category in Enum.GetValues(typeof(BattleActionCategory)))
            {
                var used = new HashSet<int>();
                foreach (var option in Options) if (option != null && option.Category == category && option.FavoriteSlot >= 0)
                    if (!used.Add(option.FavoriteSlot)) option.FavoriteSlot = -1;
                foreach (var option in Options)
                {
                    if (option == null || option.Category != category || option.FavoriteSlot >= 0) continue;
                    for (int slot = 0; slot < 4; slot++) if (used.Add(slot)) { option.FavoriteSlot = slot; break; }
                }
            }
        }
        public BattleActionOption Favorite(BattleActionCategory category, int slot)
        { InitializeFavorites(); return slot < 0 || slot >= 4 ? null : Options.Find(o => o != null && o.Category == category && o.FavoriteSlot == slot); }
        public bool AssignFavorite(BattleActionOption option, int slot)
        {
            InitializeFavorites();
            if (option == null || !Options.Contains(option) || slot < -1 || slot >= 4) return false;
            if (slot >= 0) foreach (var other in Options)
                if (other != null && other.Category == option.Category && other.FavoriteSlot == slot) other.FavoriteSlot = -1;
            option.FavoriteSlot = slot; return true;
        }
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
        public static bool Clear(GridBoard board, Vector2Int from, Vector2Int to, bool allowBlockedDestination = false)
        {
            if (!board.Contains(from) || !board.Contains(to)) return false;
            int steps = Distance(from, to);
            Vector2Int last = from;
            for (int i = 1; i <= steps; i++)
            {
                var p = Vector2Int.RoundToInt(Vector2.Lerp(from, to, i / (float)steps));
                if (!board.Contains(p) || (board.At(p).Ground == Ground.Obstacle && !(p == to && allowBlockedDestination))) return false;
                if (p.x != last.x && p.y != last.y &&
                    (board.At(new Vector2Int(last.x, p.y)).Ground == Ground.Obstacle ||
                     board.At(new Vector2Int(p.x, last.y)).Ground == Ground.Obstacle)) return false;
                last = p;
            }
            return true;
        }

        public static bool CanTarget(GridBoard board, Vector2Int from, Vector2Int to, BattleActionOption action,
            Func<Vector2Int, Vector2Int, bool> sight = null, Func<Vector2Int, bool> targetableObstacle = null)
        {
            int distance = Distance(from, to);
            if ((distance == 0 && action.Pattern == BattleTargetPattern.Piercing) || distance > action.Reach || !board.Contains(to)) return false;
            var d = to - from;
            if (action.Pattern == BattleTargetPattern.Piercing && d.x != 0 && d.y != 0 && Mathf.Abs(d.x) != Mathf.Abs(d.y)) return false;
            return Clear(board, from, to, targetableObstacle?.Invoke(to) ?? false) && (sight == null || sight(from, to));
        }

        public static HashSet<Vector2Int> Footprint(GridBoard board, Vector2Int from, Vector2Int target,
            BattleActionOption action, Func<Vector2Int, Vector2Int, bool> sight = null, Func<Vector2Int, bool> targetableObstacle = null)
        {
            var result = new HashSet<Vector2Int>();
            if (!CanTarget(board, from, target, action, sight, targetableObstacle)) return result;
            if (action.Pattern == BattleTargetPattern.Piercing)
            {
                var delta = target - from;
                var step = new Vector2Int(Math.Sign(delta.x), Math.Sign(delta.y));
                for (int i = 1; i <= action.Reach; i++)
                {
                    var p = from + step * i;
                    if (!CanTarget(board, from, p, action, sight, targetableObstacle)) break;
                    result.Add(p);
                    if (board.At(p).Ground == Ground.Obstacle) break;
                }
            }
            else if (action.Pattern == BattleTargetPattern.Area)
            {
                for (int x = -action.Radius; x <= action.Radius; x++) for (int y = -action.Radius; y <= action.Radius; y++)
                {
                    var p = target + new Vector2Int(x, y);
                    if (board.Contains(p) && Clear(board, target, p, targetableObstacle?.Invoke(p) ?? false) && (sight == null || sight(target, p))) result.Add(p);
                }
            }
            else result.Add(target);
            return result;
        }
    }
}
