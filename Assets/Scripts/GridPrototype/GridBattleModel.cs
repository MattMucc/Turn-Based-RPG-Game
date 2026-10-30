using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tenebrasia.GridPrototype
{
    public enum Ground { Land, Water, Obstacle, Hazard }

    public sealed class GridCell
    {
        public Ground Ground;
        public int Height;
        public float? SampledHeight;
        public float TraversalHeight => SampledHeight ?? Height;
        public GridCell(Ground ground = Ground.Land, int height = 0)
        { Ground = ground; Height = height; }
    }

    public sealed class GridUnit
    {
        public readonly string Name;
        public Vector2Int Position;
        public int Movement { get; private set; }
        public bool ActionAvailable { get; private set; }
        public bool TurnOpen { get; private set; }
        public int MaxHealth = 100;
        public int JumpRange = 2;
        public int JumpHeight = 1;
        public int Health = 100;
        public bool Blocking;

        public GridUnit(string name, Vector2Int position) { Name = name; Position = position; }

        // The caller decides whether a future status permits a turn or movement grant.
        public void BeginTurn(int movementGrant = 3)
        {
            if (TurnOpen) throw new InvalidOperationException("Turn already started.");
            if (movementGrant < 0) throw new ArgumentOutOfRangeException(nameof(movementGrant));
            Movement = checked(Movement + movementGrant);
            ActionAvailable = true;
            Blocking = false;
            TurnOpen = true;
        }

        public bool SpendMovement(int amount)
        {
            if (!TurnOpen || amount <= 0 || amount > Movement) return false;
            Movement -= amount;
            return true;
        }

        public bool SpendAction()
        {
            if (!TurnOpen || !ActionAvailable) return false;
            ActionAvailable = false;
            return true;
        }

        public void EndTurn() { TurnOpen = false; ActionAvailable = false; }
    }

    public sealed class GridRoute
    {
        public readonly List<Vector2Int> Stops;
        public readonly int Cost;
        public GridRoute(List<Vector2Int> stops, int cost) { Stops = stops; Cost = cost; }
    }

    public sealed class GridBoard
    {
        public readonly int Width, Depth;
        public readonly GridCell[,] Cells;
        public readonly List<GridUnit> Units = new List<GridUnit>();
        public Func<GridUnit, Vector2Int, Vector2Int, bool> TraversalFilter;

        public GridBoard(int width, int depth)
        {
            Width = width; Depth = depth;
            Cells = new GridCell[width, depth];
            for (int x = 0; x < width; x++)
                for (int z = 0; z < depth; z++) Cells[x, z] = new GridCell();
        }

        public bool Contains(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < Width && p.y < Depth;
        public GridCell At(Vector2Int p) => Cells[p.x, p.y];
        public bool IsLanding(Vector2Int p, GridUnit mover)
        {
            if (!Contains(p) || At(p).Ground == Ground.Water || At(p).Ground == Ground.Obstacle) return false;
            return !Units.Exists(u => u != mover && u.Position == p);
        }

        // Eight directions, unit-cost diagonals; no cutting through blocked side cells.
        public bool CanTraverse(GridUnit unit, Vector2Int from, Vector2Int to)
        {
            if (!Contains(from) || !IsLanding(to, unit)) return false;
            Vector2Int delta = to - from;
            int dx = Math.Abs(delta.x), dz = Math.Abs(delta.y);
            int distance = Math.Max(dx, dz);
            if (distance == 0 || (dx != 0 && dz != 0 && dx != dz)) return false;
            if (Math.Abs(At(to).TraversalHeight - At(from).TraversalHeight) > unit.JumpHeight + 0.001f) return false;
            var step = new Vector2Int(Math.Sign(delta.x), Math.Sign(delta.y));
            if (distance > 1 && distance > unit.JumpRange) return false;
            for (int i = 1; i <= distance; i++)
            {
                Vector2Int prev = from + step * (i - 1);
                Vector2Int next = from + step * i;
                if (step.x != 0 && step.y != 0)
                {
                    // Water is clear air during a gap jump, but never a walking corner.
                    if (!SideClear(prev + new Vector2Int(step.x, 0), unit, distance > 1, At(from).TraversalHeight)
                        || !SideClear(prev + new Vector2Int(0, step.y), unit, distance > 1, At(from).TraversalHeight)) return false;
                }
                // Long edges are jumps across water only, not shortcuts through units/walls.
                if (i < distance && (!Contains(next) || At(next).Ground != Ground.Water
                    || Units.Exists(u => u != unit && u.Position == next))) return false;
            }
            return TraversalFilter == null || TraversalFilter(unit, from, to);
        }

        private bool SideClear(Vector2Int p, GridUnit unit, bool jumping, float height)
        {
            if (!Contains(p) || At(p).Ground == Ground.Obstacle
                || At(p).TraversalHeight > height + unit.JumpHeight
                || Units.Exists(u => u != unit && u.Position == p)) return false;
            return jumping || At(p).Ground != Ground.Water;
        }

        public Dictionary<Vector2Int, GridRoute> Reachable(GridUnit unit, int budget)
        {
            var costs = new Dictionary<Vector2Int, int> { [unit.Position] = 0 };
            // Movement costs are still whole cells. Break equal-cost ties by actual
            // distance so a straight route wins over an equally priced zigzag.
            var lengths = new Dictionary<Vector2Int, float> { [unit.Position] = 0 };
            var parents = new Dictionary<Vector2Int, Vector2Int>();
            var open = new List<Vector2Int> { unit.Position };
            while (open.Count > 0)
            {
                int best = 0;
                for (int i = 1; i < open.Count; i++) if (costs[open[i]] < costs[open[best]]) best = i;
                var current = open[best]; open.RemoveAt(best);
                for (int dx = -1; dx <= 1; dx++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        for (int distance = 1; distance <= Math.Max(1, unit.JumpRange); distance++)
                        {
                            var next = current + new Vector2Int(dx, dz) * distance;
                            int cost = costs[current] + distance;
                            if (cost > budget || !CanTraverse(unit, current, next)) continue;
                            float length = lengths[current] + Vector2Int.Distance(current, next);
                            if (costs.TryGetValue(next, out int previous)
                                && (previous < cost || (previous == cost && lengths[next] <= length + 0.0001f))) continue;
                            costs[next] = cost; lengths[next] = length; parents[next] = current;
                            if (!open.Contains(next)) open.Add(next);
                        }
                    }
            }
            var result = new Dictionary<Vector2Int, GridRoute>();
            foreach (var entry in costs)
            {
                var stops = new List<Vector2Int> { entry.Key };
                while (stops[stops.Count - 1] != unit.Position) stops.Add(parents[stops[stops.Count - 1]]);
                stops.Reverse(); result[entry.Key] = new GridRoute(stops, entry.Value);
            }
            return result;
        }

        public bool TryMove(GridUnit unit, Vector2Int destination, out GridRoute route)
        {
            route = null;
            if (!unit.TurnOpen || !Reachable(unit, unit.Movement).TryGetValue(destination, out route)
                || !unit.SpendMovement(route.Cost)) return false;
            for (int i = 1; i < route.Stops.Count; i++)
                if (At(route.Stops[i]).Ground == Ground.Hazard)
                    unit.Health = Math.Max(1, unit.Health - (unit.Blocking ? 5 : 10));
            unit.Position = destination;
            return true;
        }
    }
}
