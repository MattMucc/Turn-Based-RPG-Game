using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GridPrototype
{
    public sealed partial class InSceneGridBattle
    {
        public bool TryActionApproach(BattleActionOption action, IReadOnlyList<Vector2Int> cells, out GridRoute route)
        {
            route = null;
            if (action == null || cells.Count == 0 || !ActionReady) return false;
            bool Fits(Vector2Int origin)
            {
                foreach (var cell in cells)
                    if (!Grid.Board.Contains(cell) || !Grid.HasSurface[cell.x, cell.y] || !GridTargeting.CanTarget(Grid.Board, origin, cell, action, Sight, TargetableObstacle)) return false;
                return true;
            }
            if (Fits(Active.Position)) return true;
            foreach (var pair in Grid.Board.Reachable(Active, Active.Movement))
            {
                if (pair.Value.Cost == 0 || (route != null && pair.Value.Cost >= route.Cost) || !Fits(pair.Key)) continue;
                route = pair.Value;
            }
            return route != null;
        }
        public bool CanSelectActionCell(Vector2Int cell, BattleActionOption action)
        {
            var selection = action != null && action.Pattern == BattleTargetPattern.Multiple ? new List<Vector2Int>(targets) : new List<Vector2Int>();
            selection.Add(cell); return TryActionApproach(action, selection, out _);
        }
        public int PreviewApproachCost
        {
            get
            {
                if (ChosenAction == null || !ActionReady) return 0;
                var selection = targets.Count > 0 ? new List<Vector2Int>(targets) : new List<Vector2Int> { selected };
                return TryActionApproach(ChosenAction, selection, out var path) && path != null ? path.Cost : 0;
            }
        }
        private IEnumerator ResolveApproach(GridRoute route, IEnumerator attack)
        {
            var movement = Move(route, actorIndex);
            while (movement.MoveNext()) yield return movement.Current;
            busy = true;
            while (!closing && attack.MoveNext()) yield return attack.Current;
        }
    }
}
