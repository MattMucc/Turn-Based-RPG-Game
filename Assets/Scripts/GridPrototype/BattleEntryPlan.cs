using System.Collections.Generic;
using UnityEngine;

namespace Game.GridPrototype
{
    // Validated before any actor, input map or camera is changed.
    public sealed class BattleEntryPlan
    {
        public readonly List<Vector2Int> Cells = new();
        public readonly List<List<Vector3>> Routes = new();
        public float Duration = 2f;
        public Vector3 Facing;
        public bool PreserveWorldPositions;
    }

    public static class BattleFormation
    {
        public static bool TryGridBounds(IReadOnlyList<Vector3> feet, int partySize, float cellSize, int minimumWidth, int minimumDepth,
            Vector3? authoredCenter, out Vector3 center, out int width, out int depth, bool includeParty = false)
        {
            center = Vector3.zero; width = depth = 0;
            if (feet.Count == 0 || cellSize <= 0) return false;
            var bounds = new Bounds(feet[0], Vector3.zero);
            for (int i = includeParty ? 1 : partySize; i < feet.Count; i++) bounds.Encapsulate(feet[i]);
            center = authoredCenter ?? bounds.center;
            if (!authoredCenter.HasValue)
            {
                center.x = Mathf.Round(center.x / cellSize) * cellSize;
                center.z = Mathf.Round(center.z / cellSize) * cellSize;
            }
            center.y = Mathf.Max(center.y, bounds.max.y);
            float horizontal = Mathf.Max(Mathf.Abs(bounds.min.x - center.x), Mathf.Abs(bounds.max.x - center.x));
            float vertical = Mathf.Max(Mathf.Abs(bounds.min.z - center.z), Mathf.Abs(bounds.max.z - center.z));
            width = Mathf.Max(minimumWidth, Mathf.CeilToInt(horizontal / cellSize) * 2 + 5);
            depth = Mathf.Max(minimumDepth, Mathf.CeilToInt(vertical / cellSize) * 2 + 5);
            return width <= 64 && depth <= 64;
        }
        public static bool TryPlan(TerrainBattleGrid grid, IReadOnlyList<Vector3> feet, int partySize,
            Vector3 fallbackForward, IReadOnlyList<Vector3?> companionMarkers, out BattleEntryPlan plan)
            => TryPlan(grid, feet, partySize, fallbackForward, companionMarkers, out plan, out _);

        public static bool TryPlan(TerrainBattleGrid grid, IReadOnlyList<Vector3> feet, int partySize,
            Vector3 fallbackForward, IReadOnlyList<Vector3?> companionMarkers, out BattleEntryPlan plan, out string failure, bool preserveWorldPositions = false)
        {
            plan = null; failure = "Invalid encounter roster.";
            if (partySize < 1 || partySize > 3 || feet.Count <= partySize) return false;
            var result = new BattleEntryPlan { PreserveWorldPositions = preserveWorldPositions }; var taken = new HashSet<Vector2Int>();
            for (int i = 0; i < feet.Count; i++) { result.Cells.Add(default); result.Routes.Add(null); }
            // Reserve enemy cells before companions, so formation cannot displace enemies.
            for (int i = 0; i < feet.Count; i++)
            {
                if (!preserveWorldPositions && i > 0 && i < partySize) continue;
                float best = float.PositiveInfinity; Vector2Int pick = default;
                for (int x = 0; x < grid.Board.Width; x++) for (int y = 0; y < grid.Board.Depth; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (taken.Contains(cell) || grid.Board.At(cell).Ground != Ground.Land) continue;
                    float distance = (grid.Point(cell) - feet[i]).sqrMagnitude;
                    if (distance >= best || distance > grid.CellSize * grid.CellSize * 2.25f || !grid.ClearEntryApproach(feet[i], cell)) continue;
                    best = distance; pick = cell;
                }
                if (float.IsPositiveInfinity(best))
                { failure = i == 0 ? "No supported, clear starting cell near the captain." : i < partySize ? $"Companion {i} has no supported, clear starting cell near their current position." : $"Enemy {i - partySize + 1} has no supported, clear starting cell near its position."; return false; }
                result.Cells[i] = pick; taken.Add(pick);
                result.Routes[i] = new List<Vector3> { feet[i], grid.Point(pick) };
            }
            Vector3 facing = fallbackForward; float nearest = float.PositiveInfinity;
            for (int i = partySize; i < feet.Count; i++)
            {
                Vector3 delta = feet[i] - feet[0]; delta.y = 0;
                if (delta.sqrMagnitude < nearest && delta.sqrMagnitude > .001f) { nearest = delta.sqrMagnitude; facing = delta; }
            }
            facing.y = 0; facing = facing.sqrMagnitude > .001f ? facing.normalized : Vector3.forward; result.Facing = facing;
            if (preserveWorldPositions) { plan = result; failure = null; return true; }
            Vector3 right = Vector3.Cross(Vector3.up, facing), captain = grid.Point(result.Cells[0]);
            var routes = WalkRoutes(grid, result.Cells[0], taken);
            for (int member = 1; member < partySize; member++)
            {
                // Adjacent safe ground only: entry does not grant a gap jump or a distant flanking position.
                Vector3 preference = captain - facing * grid.CellSize + right * (member == 1 ? -grid.CellSize : grid.CellSize);
                bool authored = companionMarkers != null && member - 1 < companionMarkers.Count && companionMarkers[member - 1].HasValue;
                if (authored) preference = companionMarkers[member - 1].Value;
                float best = float.PositiveInfinity; List<Vector2Int> path = null; Vector2Int pick = default;
                foreach (var pair in routes)
                {
                    if (taken.Contains(pair.Key)) continue;
                    Vector3 point = grid.Point(pair.Key);
                    float ahead = Mathf.Max(0, Vector3.Dot(point - captain, facing));
                    float score = (point - preference).sqrMagnitude + pair.Value.Count * .1f + (authored ? 0 : ahead * grid.CellSize * 4);
                    if (score < best) { best = score; path = pair.Value; pick = pair.Key; }
                }
                if (path == null) { failure = $"Companion {member} has no safe route to a free starting cell near the captain."; return false; }
                taken.Add(pick); result.Cells[member] = pick;
                var route = new List<Vector3>();
                for (int i = 0; i < path.Count; i++)
                {
                    if (i == 0) route.Add(grid.Point(path[i]));
                    else for (int step = 1; step <= 8; step++) route.Add(grid.RoutePoint(path[i - 1], path[i], step / 8f));
                }
                result.Routes[member] = route;
            }
            plan = result; failure = null; return true;
        }

        private static Dictionary<Vector2Int, List<Vector2Int>> WalkRoutes(TerrainBattleGrid grid, Vector2Int start, HashSet<Vector2Int> occupied)
        {
            var routes = new Dictionary<Vector2Int, List<Vector2Int>> { [start] = new List<Vector2Int> { start } };
            var queue = new Queue<Vector2Int>(); queue.Enqueue(start);
            var probe = new GridUnit("Formation", start) { JumpRange = 1, JumpHeight = 1 };
            while (queue.Count > 0)
            {
                var current = queue.Dequeue(); if (routes[current].Count > 4) continue;
                for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var next = current + new Vector2Int(dx, dy);
                    if (!grid.Board.Contains(next) || routes.ContainsKey(next) || occupied.Contains(next) || grid.Board.At(next).Ground != Ground.Land) continue;
                    if (dx != 0 && dy != 0 && (occupied.Contains(current + new Vector2Int(dx, 0)) || occupied.Contains(current + new Vector2Int(0, dy)))) continue;
                    if (!grid.Board.CanTraverse(probe, current, next)) continue;
                    var path = new List<Vector2Int>(routes[current]) { next }; routes[next] = path; queue.Enqueue(next);
                }
            }
            return routes;
        }

        public static Vector3 RoutePosition(IReadOnlyList<Vector3> route, float progress)
        {
            float length = 0; for (int i = 1; i < route.Count; i++) length += Vector3.Distance(route[i - 1], route[i]);
            float remaining = length * Mathf.Clamp01(progress);
            for (int i = 1; i < route.Count; i++)
            {
                float segment = Vector3.Distance(route[i - 1], route[i]);
                if (segment > .0001f && remaining < segment) return Vector3.Lerp(route[i - 1], route[i], remaining / segment);
                remaining -= segment;
            }
            return route[route.Count - 1];
        }
    }
}
