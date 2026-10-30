using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tenebrasia.GridPrototype
{
    // Prototype encounter controller. Unit zero is the currently possessed world
    // character; the rest are the actual spawned members of the triggering group.
    public sealed class InSceneGridBattle : MonoBehaviour
    {
        public TerrainBattleGrid Grid { get; private set; }
        private readonly List<Transform> actors = new List<Transform>();
        private readonly List<GridUnit> turnOrder = new List<GridUnit>();
        private readonly List<float> offsets = new List<float>();
        private Camera view;
        private GridCameraController cameraControls;
        private SampledGridOverlay overlay;
        private Action<bool> finished;
        private Dictionary<Vector2Int, GridRoute> reachable;
        private Vector2Int selected;
        private int actorIndex, round = 1;
        private bool busy, closing;
        private string message;
        public BattleActionLoadout Loadout { get; private set; }
        public BattleActionOption ChosenAction { get; private set; }
        private readonly List<int> targets = new List<int>();
        public bool GridInputEnabled = true;
        public bool KeyboardGridInputEnabled = true;
        public Func<Vector2, bool> PointerOverUI;
        public bool PlayerReady => !busy && !closing && actorIndex == 0;
        public bool ActionReady => PlayerReady && Active.ActionAvailable;
        public int Movement => turnOrder[0].Movement;
        public int TargetCount => targets.Count;
        public int TargetLimit => ChosenAction?.Pattern == BattleTargetPattern.Multiple ? ChosenAction.MaxTargets : 1;
        public string Message => message;
        public event Action ActionResolved;
        private GridUnit Active => turnOrder[actorIndex];


        public void Initialize(TerrainBattleGrid grid, IReadOnlyList<Transform> figures, IReadOnlyList<float> footOffsets,
            IReadOnlyList<Vector2Int> positions, Camera sourceCamera, Shader shader, Action<bool> onFinished)
        {
            Grid = grid; finished = onFinished;
            for (int i = 0; i < figures.Count; i++)
            {
                actors.Add(figures[i]); offsets.Add(footOffsets[i]);
                var unit = new GridUnit(figures[i].name, positions[i]); turnOrder.Add(unit); Grid.Board.Units.Add(unit);
                figures[i].position = Grid.Point(positions[i]) + Vector3.up * footOffsets[i];
            }
            for (int i = 0; i < actors.Count; i++)
                actors[i].rotation = GridTargeting.Facing(Grid.Point(positions[i == 0 ? 1 : 0]) - Grid.Point(positions[i]));
            Loadout = figures[0].GetComponent<BattleActionLoadout>();
            if (!Loadout) Loadout = figures[0].gameObject.AddComponent<BattleActionLoadout>();
            var cameraObject = new GameObject("Encounter camera", typeof(Camera)); cameraObject.transform.SetParent(transform);
            view = cameraObject.GetComponent<Camera>(); view.CopyFrom(sourceCamera); view.enabled = true; view.rect = new Rect(0, 0, 1, 1);
            Vector3 center = Grid.Point(positions[0]);
            view.transform.position = center + new Vector3(0, 15, -14); view.transform.LookAt(center);
            cameraControls = cameraObject.AddComponent<GridCameraController>(); cameraControls.FocusTarget = actors[0];
            cameraControls.PointerOverUI = p => PointerOverUI?.Invoke(p) ?? false;
            overlay = new GameObject("Grid over world terrain").AddComponent<SampledGridOverlay>();
            overlay.transform.SetParent(transform); overlay.Build(Grid, shader);
            Active.BeginTurn(); selected = Active.Position;
            message = "Choose a command or move to a green tile."; Refresh();
        }

        private void Refresh()
        {
            if (ChosenAction != null) { Preview(); return; }
            reachable = Grid.Board.Reachable(Active, Active.Movement);
            overlay.Show(actorIndex == 0 ? reachable : new Dictionary<Vector2Int, GridRoute>(), selected);
        }

        private void Update()
        {
            if (Grid == null || closing) return;
            bool click = cameraControls.ConsumeClick(out var pointer);
            if (!PlayerReady || !GridInputEnabled) return;
            var k = Keyboard.current; var pad = Gamepad.current; Vector2Int step = Vector2Int.zero;
            if (k != null && KeyboardGridInputEnabled)
            {
                if (k.leftArrowKey.wasPressedThisFrame) step.x--;
                if (k.rightArrowKey.wasPressedThisFrame) step.x++;
                if (k.upArrowKey.wasPressedThisFrame) step.y++;
                if (k.downArrowKey.wasPressedThisFrame) step.y--;

            }
            if (pad != null && KeyboardGridInputEnabled)
            {
                if (pad.dpad.left.wasPressedThisFrame) step.x--;
                if (pad.dpad.right.wasPressedThisFrame) step.x++;
                if (pad.dpad.up.wasPressedThisFrame) step.y++;
                if (pad.dpad.down.wasPressedThisFrame) step.y--;

            }
            if (step != Vector2Int.zero)
            {
                selected = new Vector2Int(Mathf.Clamp(selected.x + step.x, 0, Grid.Board.Width - 1),
                    Mathf.Clamp(selected.y + step.y, 0, Grid.Board.Depth - 1)); Refresh();
            }
            if (KeyboardGridInputEnabled && ((k != null && k.enterKey.wasPressedThisFrame) || (pad != null && pad.buttonSouth.wasPressedThisFrame))) Confirm();
            if (Mouse.current != null && !(PointerOverUI?.Invoke(Mouse.current.position.ReadValue()) ?? false)
                && TryPick(Mouse.current.position.ReadValue(), out var hovered))
            {
                if (ChosenAction != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0)
                { selected = hovered; Preview(); }
                if (click)
                {
                    if (ChosenAction != null) { selected = hovered; Confirm(); }
                    else if (selected == hovered) Confirm();
                    else { selected = hovered; Refresh(); }
                }
            }
        }

        private bool TryPick(Vector2 pointer, out Vector2Int cell)
        {
            Ray ray = view.ScreenPointToRay(pointer);
            float nearest = float.PositiveInfinity;
            cell = default;
            bool found = false;
            if (Physics.Raycast(ray, out var hit, 500, ~0, QueryTriggerInteraction.Ignore))
            { nearest = hit.distance; found = Grid.TryCell(hit.point, out cell); }
            if (ChosenAction == null) return found;
            for (int i = 1; i < actors.Count; i++)
            {
                if (turnOrder[i].Health <= 0) continue;
                foreach (var renderer in actors[i].GetComponentsInChildren<Renderer>())
                    if (renderer.enabled && renderer.bounds.IntersectRay(ray, out float distance) && distance < nearest)
                    { nearest = distance; cell = turnOrder[i].Position; found = true; }
            }
            return found;
        }

        private bool Sight(Vector2Int from, Vector2Int to)
        {
            if (from == to) return true;
            return !Physics.Linecast(Grid.Point(from) + Vector3.up, Grid.Point(to) + Vector3.up,
                ~0, QueryTriggerInteraction.Ignore);
        }

        private bool ValidTarget(int index) => index > 0 && index < turnOrder.Count && turnOrder[index].Health > 0
            && GridTargeting.CanTarget(Grid.Board, Active.Position, turnOrder[index].Position, ChosenAction, Sight);

        public bool ChooseAction(BattleActionOption option)
        {
            if (!ActionReady || option == null || !option.Available || !Loadout.Options.Contains(option)) return false;
            ChosenAction = option; targets.Clear(); selected = Active.Position;
            message = "Choose an enemy, then Confirm. Blue tiles show reach."; Refresh(); return true;
        }

        public void CancelAction()
        { ChosenAction = null; targets.Clear(); if (Grid != null && !closing) Refresh(); }

        private void Preview()
        {
            var range = new HashSet<Vector2Int>();
            for (int x = 0; x < Grid.Board.Width; x++) for (int y = 0; y < Grid.Board.Depth; y++)
            {
                var p = new Vector2Int(x, y);
                if (GridTargeting.CanTarget(Grid.Board, Active.Position, p, ChosenAction, Sight)) range.Add(p);
            }
            var impact = new HashSet<Vector2Int>();
            foreach (int index in targets)
                impact.UnionWith(GridTargeting.Footprint(Grid.Board, Active.Position, turnOrder[index].Position, ChosenAction, Sight));
            // Once an anchor is selected its footprint stays fixed until another click.
            if (targets.Count == 0)
                impact.UnionWith(GridTargeting.Footprint(Grid.Board, Active.Position, selected, ChosenAction, Sight));
            overlay.ShowTargeting(range, impact, selected);
        }

        public bool ToggleTarget(Vector2Int cell)
        {
            if (!ActionReady || ChosenAction == null) return false;
            int target = turnOrder.FindIndex(u => u != Active && u.Health > 0 && u.Position == cell);
            if (!ValidTarget(target)) { message = "Choose an enemy on a blue tile with a clear attack path."; return false; }
            if (targets.Contains(target)) targets.Remove(target);
            else if (ChosenAction.Pattern != BattleTargetPattern.Multiple) { targets.Clear(); targets.Add(target); }
            else if (targets.Count < TargetLimit) targets.Add(target);
            else { message = "Maximum targets selected. Deselect one to replace it."; return false; }
            selected = cell;
            message = "Confirm to use the action; click a selected enemy again to deselect.";
            Preview(); return true;
        }

        public bool CommitAction()
        {
            if (!ActionReady || ChosenAction == null || !ChosenAction.Available || targets.Count == 0) return false;
            foreach (int index in targets) if (!ValidTarget(index))
            { message = "Target is no longer in reach. Choose again."; return false; }
            var impact = new HashSet<Vector2Int>();
            foreach (int index in targets)
                impact.UnionWith(GridTargeting.Footprint(Grid.Board, Active.Position, turnOrder[index].Position, ChosenAction, Sight));
            if (!Active.SpendAction()) return false;
            if (ChosenAction.Category == BattleActionCategory.Item) ChosenAction.Quantity--;
            actors[0].rotation = GridTargeting.Facing(Grid.Point(turnOrder[targets[0]].Position) - Grid.Point(Active.Position));
            string actionName = ChosenAction.Name;
            int damage = ChosenAction.Damage;
            for (int i = 1; i < turnOrder.Count; i++)
            {
                var victim = turnOrder[i];
                if (victim.Health <= 0 || !impact.Contains(victim.Position)) continue;
                victim.Health = Mathf.Max(0, victim.Health - (victim.Blocking ? damage / 2 : damage));
                if (victim.Health == 0) { Grid.Board.Units.Remove(victim); actors[i].gameObject.SetActive(false); }
            }
            CancelAction(); message = actionName + " used. You can still move or end your turn.";
            ActionResolved?.Invoke();
            if (!turnOrder.Exists(u => u != turnOrder[0] && u.Health > 0)) Finish(true);
            return true;
        }

        private bool CanStrike(GridUnit attacker, GridUnit target)
        {
            var delta = target.Position - attacker.Position;
            return Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) == 1
                && Grid.Board.CanTraverse(target, attacker.Position, target.Position);
        }

        private void Confirm()
        {
            if (busy || closing || actorIndex != 0) return;
            if (ChosenAction != null)
            {
                ToggleTarget(selected); return;
            }
            if (!Grid.Board.TryMove(Active, selected, out var path)) { message = "That tile is not reachable."; return; }
            StartCoroutine(Move(path, actorIndex));
        }

        private IEnumerator Move(GridRoute path, int index)
        {
            busy = true;
            for (int i = 1; i < path.Stops.Count; i++)
            {
                actors[index].rotation = GridTargeting.Facing(Grid.Point(path.Stops[i]) - Grid.Point(path.Stops[i - 1]));
                for (float t = 0; t < 1; t += Time.deltaTime * 4)
                {
                    actors[index].position = Grid.RoutePoint(path.Stops[i - 1], path.Stops[i], t) + Vector3.up * offsets[index];
                    yield return null;
                }
                actors[index].position = Grid.Point(path.Stops[i]) + Vector3.up * offsets[index];
            }
            busy = false; Refresh();
        }

        private void Strike(int targetIndex)
        {
            actors[actorIndex].rotation = GridTargeting.Facing(Grid.Point(turnOrder[targetIndex].Position) - Grid.Point(Active.Position));
            var target = turnOrder[targetIndex]; int damage = target.Blocking ? 12 : 25;
            target.Health = Mathf.Max(0, target.Health - damage);
            message = $"{Active.Name} hits {target.Name} for {damage}.";
            if (target.Health > 0) return;
            Grid.Board.Units.Remove(target);
            if (targetIndex == 0) { Finish(false); return; }
            actors[targetIndex].gameObject.SetActive(false);
            if (!turnOrder.Exists(u => u != turnOrder[0] && u.Health > 0)) Finish(true);
        }

        public void Block()
        {
            if (!busy && actorIndex == 0 && Active.SpendAction())
            { CancelAction(); Active.Blocking = true; ActionResolved?.Invoke(); message = "Blocking until your next turn. Movement remains available."; }
        }

        public void EndTurn()
        {
            if (busy || closing || actorIndex != 0) return;
            CancelAction(); Active.EndTurn(); StartCoroutine(EnemyRound()); ActionResolved?.Invoke();
        }

        private IEnumerator EnemyRound()
        {
            busy = true;
            for (int i = 1; i < turnOrder.Count; i++)
            {
                if (turnOrder[i].Health <= 0) continue;
                actorIndex = i; Active.BeginTurn(); selected = Active.Position; Refresh();
                yield return new WaitForSeconds(0.25f);
                if (!CanStrike(Active, turnOrder[0]))
                {
                    var choices = Grid.Board.Reachable(Active, Active.Movement);
                    Vector2Int destination = Active.Position;
                    // Find a path toward a legal melee cell, then take the affordable prefix.
                    var all = Grid.Board.Reachable(Active, Grid.Board.Width * Grid.Board.Depth * 2);
                    GridRoute approach = null;
                    foreach (var option in all)
                    {
                        var old = Active.Position; Active.Position = option.Key;
                        bool adjacent = CanStrike(Active, turnOrder[0]); Active.Position = old;
                        if (adjacent && (approach == null || option.Value.Cost < approach.Cost)) approach = option.Value;
                    }
                    if (approach != null)
                        foreach (var stop in approach.Stops) if (choices.ContainsKey(stop)) destination = stop; else break;
                    if (destination != Active.Position && Grid.Board.TryMove(Active, destination, out var path))
                        yield return Move(path, actorIndex);
                }
                busy = true;
                if (CanStrike(Active, turnOrder[0]) && Active.SpendAction()) Strike(0);
                if (closing) yield break;
                Active.EndTurn(); yield return new WaitForSeconds(0.2f);
            }
            actorIndex = 0; round++; Active.BeginTurn(); selected = Active.Position;
            busy = false; message = "Your turn: saved movement + 3, action refreshed."; Refresh();
        }

        public void Finish(bool victory)
        {
            if (closing) return;
            closing = true; StopAllCoroutines(); finished?.Invoke(victory);
        }

    }
}
