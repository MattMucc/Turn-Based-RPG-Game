using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GridPrototype
{
    public sealed class TerrainBattleGrid
    {
        public readonly GridBoard Board;
        public readonly Vector3 Origin;
        public readonly float CellSize;
        public readonly Vector3[,] Centers;
        public readonly Vector3[,][] Corners;
        public readonly bool[,] HasSurface;
        private readonly HashSet<Collider> ignored;
        private readonly Dictionary<(Vector2Int, Vector2Int), bool> edgeCache = new Dictionary<(Vector2Int, Vector2Int), bool>();
        private readonly float radius, bodyHeight;
        private readonly int groundMask;
        private readonly float rayTop;

        public TerrainBattleGrid(Vector3 center, int width, int depth, float cellSize, int groundLayers,
            IEnumerable<Collider> ignoredColliders, float actorRadius = 0.5f, float actorHeight = 2f)
        {
            Board = new GridBoard(width, depth); CellSize = cellSize;
            Origin = new Vector3(center.x - (width / 2) * cellSize, 0, center.z - (depth / 2) * cellSize);
            Centers = new Vector3[width, depth]; Corners = new Vector3[width, depth][]; HasSurface = new bool[width, depth];
            ignored = new HashSet<Collider>(ignoredColliders); radius = actorRadius; bodyHeight = actorHeight;
            groundMask = groundLayers | LayerMask.GetMask("Water"); rayTop = center.y + 12;
            Sample(); Board.TraversalFilter = ClearEdge;
        }

        public Vector3 Point(Vector2Int cell) => Centers[cell.x, cell.y];
        public bool TryCell(Vector3 world, out Vector2Int cell)
        {
            cell = new Vector2Int(Mathf.FloorToInt((world.x - Origin.x) / CellSize + 0.5f),
                Mathf.FloorToInt((world.z - Origin.z) / CellSize + 0.5f));
            return Board.Contains(cell) && HasSurface[cell.x, cell.y];
        }

        private bool GroundAt(Vector3 point, out RaycastHit result)
        {
            var hits = Physics.RaycastAll(new Vector3(point.x, rayTop, point.z), Vector3.down, 30,
                groundMask, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits) if (!ignored.Contains(hit.collider) && !hit.collider.GetComponentInParent<BattleDestructible>()) { result = hit; return true; }
            result = default; return false;
        }

        public void RefreshSurfaces() { edgeCache.Clear(); Sample(); }

        private void Sample()
        {
            float half = CellSize * 0.49f;
            var offsets = new[] { new Vector3(-half, 0, -half), new Vector3(-half, 0, half),
                new Vector3(half, 0, half), new Vector3(half, 0, -half) };
            for (int x = 0; x < Board.Width; x++) for (int z = 0; z < Board.Depth; z++)
            {
                var cell = Board.Cells[x, z]; cell.Ground = Ground.Obstacle;
                HasSurface[x, z] = false; Corners[x, z] = null;
                Vector3 point = Origin + new Vector3(x * CellSize, 0, z * CellSize);
                if (!GroundAt(point, out var center)) continue;
                Centers[x, z] = center.point; HasSurface[x, z] = true;
                cell.SampledHeight = center.point.y; cell.Height = Mathf.RoundToInt(center.point.y);
                bool water = center.collider.gameObject.layer == LayerMask.NameToLayer("Water");
                bool valid = Vector3.Angle(center.normal, Vector3.up) <= 35;
                var corners = new Vector3[4];
                for (int i = 0; i < 4; i++)
                {
                    if (GroundAt(point + offsets[i], out var corner))
                    {
                        corners[i] = corner.point;
                        valid &= Vector3.Angle(corner.normal, Vector3.up) <= 35
                            && Mathf.Abs(corner.point.y - center.point.y) <= CellSize * 0.4f
                            && (corner.collider.gameObject.layer == LayerMask.NameToLayer("Water")) == water;
                    }
                    else { corners[i] = center.point + offsets[i]; valid = false; }
                }
                Corners[x, z] = corners;
                if (water && valid) cell.Ground = Ground.Water;
                else if (valid && ClearBody(center.point)) cell.Ground = Ground.Land;
            }
        }

        private bool ClearBody(Vector3 foot)
        {
            Vector3 bottom = foot + Vector3.up * (radius + 0.08f);
            Vector3 top = foot + Vector3.up * Mathf.Max(radius + 0.08f, bodyHeight - radius);
            foreach (var hit in Physics.OverlapCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore))
                if (!ignored.Contains(hit)) return false;
            return true;
        }

        public Vector3 RoutePoint(Vector2Int from, Vector2Int to, float t)
        {
            Vector3 a = Point(from), b = Point(to);
            bool jump = (to - from).sqrMagnitude > 2 || Mathf.Abs(a.y - b.y) > 0.35f;
            return Vector3.Lerp(a, b, t) + Vector3.up * (jump ? Mathf.Sin(t * Mathf.PI) : 0);
        }

        private bool ClearEdge(GridUnit unit, Vector2Int from, Vector2Int to)
        {
            if (edgeCache.TryGetValue((from, to), out bool cached)) return cached;
            int steps = Mathf.CeilToInt(Vector3.Distance(Point(from), Point(to)) / 0.2f);
            for (int i = 1; i < steps; i++)
                if (!ClearBody(RoutePoint(from, to, (float)i / steps)))
                { edgeCache[(from, to)] = false; return false; }
            edgeCache[(from, to)] = true; return true;
        }

        // All units share a connected component, and each receives a distinct nearest cell.
        // No actor is moved until the entire placement succeeds.
        public bool TryPlace(IReadOnlyList<Vector3> desired, out List<Vector2Int> placements)
        {
            placements = new List<Vector2Int>();
            if (desired.Count == 0) return false;
            var probe = new GridUnit("Placement", Vector2Int.zero);
            Vector2Int start = default; float best = float.PositiveInfinity; bool found = false;
            for (int x = 0; x < Board.Width; x++) for (int z = 0; z < Board.Depth; z++)
            {
                var p = new Vector2Int(x, z);
                if (!Board.IsLanding(p, probe)) continue;
                float distance = (Point(p) - desired[0]).sqrMagnitude;
                if (distance < best) { best = distance; start = p; found = true; }
            }
            if (!found || best > CellSize * CellSize * 9) return false;
            probe.Position = start;
            var component = Board.Reachable(probe, Board.Width * Board.Depth * 2);
            if (component.Count < desired.Count) return false;
            var taken = new HashSet<Vector2Int>();
            foreach (var target in desired)
            {
                best = float.PositiveInfinity; Vector2Int pick = default;
                foreach (var entry in component)
                {
                    if (taken.Contains(entry.Key)) continue;
                    float distance = (Point(entry.Key) - target).sqrMagnitude;
                    if (distance < best) { best = distance; pick = entry.Key; }
                }
                taken.Add(pick); placements.Add(pick);
            }
            return true;
        }
    }
}
