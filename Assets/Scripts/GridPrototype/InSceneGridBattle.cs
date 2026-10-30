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
        private bool busy, attackMode, closing;
        private string message;
        private Vector2 scroll;
        private GridUnit Active => turnOrder[actorIndex];
        private float PanelWidth => Mathf.Min(330, Screen.width * 0.42f);

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
            var cameraObject = new GameObject("Encounter camera", typeof(Camera)); cameraObject.transform.SetParent(transform);
            view = cameraObject.GetComponent<Camera>(); view.CopyFrom(sourceCamera); view.enabled = true;
            Vector3 center = Grid.Point(positions[0]);
            view.transform.position = center + new Vector3(0, 15, -14); view.transform.LookAt(center);
            cameraControls = cameraObject.AddComponent<GridCameraController>(); cameraControls.FocusTarget = actors[0];
            overlay = new GameObject("Grid over world terrain").AddComponent<SampledGridOverlay>();
            overlay.transform.SetParent(transform); overlay.Build(Grid, shader);
            Active.BeginTurn(); selected = Active.Position;
            message = "Encounter started on the existing terrain. Select a cell, then confirm."; Refresh();
        }

        private void Refresh()
        {
            reachable = Grid.Board.Reachable(Active, Active.Movement);
            overlay.Show(actorIndex == 0 ? reachable : new Dictionary<Vector2Int, GridRoute>(), selected);
        }

        private void Update()
        {
            if (Grid == null || closing) return;
            view.rect = new Rect(0, 0, 1 - PanelWidth / Screen.width, 1);
            bool click = cameraControls.ConsumeClick(out var pointer);
            if (busy || actorIndex != 0) return;
            var k = Keyboard.current; var pad = Gamepad.current; Vector2Int step = Vector2Int.zero;
            if (k != null)
            {
                if (k.leftArrowKey.wasPressedThisFrame) step.x--;
                if (k.rightArrowKey.wasPressedThisFrame) step.x++;
                if (k.upArrowKey.wasPressedThisFrame) step.y++;
                if (k.downArrowKey.wasPressedThisFrame) step.y--;
                if (k.mKey.wasPressedThisFrame) attackMode = false;
                if (k.rKey.wasPressedThisFrame) attackMode = true;
                if (k.bKey.wasPressedThisFrame) Block();
                if (k.tKey.wasPressedThisFrame) { EndTurn(); return; }
            }
            if (pad != null)
            {
                if (pad.dpad.left.wasPressedThisFrame) step.x--;
                if (pad.dpad.right.wasPressedThisFrame) step.x++;
                if (pad.dpad.up.wasPressedThisFrame) step.y++;
                if (pad.dpad.down.wasPressedThisFrame) step.y--;
                if (pad.buttonWest.wasPressedThisFrame) attackMode = !attackMode;
                if (pad.buttonNorth.wasPressedThisFrame) Block();
                if (pad.startButton.wasPressedThisFrame) { EndTurn(); return; }
            }
            if (step != Vector2Int.zero)
            {
                selected = new Vector2Int(Mathf.Clamp(selected.x + step.x, 0, Grid.Board.Width - 1),
                    Mathf.Clamp(selected.y + step.y, 0, Grid.Board.Depth - 1)); Refresh();
            }
            if ((k != null && k.enterKey.wasPressedThisFrame) || (pad != null && pad.buttonSouth.wasPressedThisFrame)) Confirm();
            if (click && Physics.Raycast(view.ScreenPointToRay(pointer), out var hit, 500, ~0, QueryTriggerInteraction.Ignore)
                && Grid.TryCell(hit.point, out var cell))
            {
                if (selected == cell) Confirm(); else { selected = cell; Refresh(); }
            }
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
            if (attackMode)
            {
                int target = turnOrder.FindIndex(u => u != Active && u.Health > 0 && u.Position == selected);
                if (target < 0 || !CanStrike(Active, turnOrder[target])) { message = "Select an adjacent enemy with a clear attack path."; return; }
                if (!Active.SpendAction()) { message = "Action spent. You may still move or end your turn."; return; }
                Strike(target); attackMode = false;
                if (!closing) Refresh(); return;
            }
            if (!Grid.Board.TryMove(Active, selected, out var path)) { message = "Cell unavailable: check terrain, occupancy, jump range and movement points."; return; }
            StartCoroutine(Move(path, actorIndex));
        }

        private IEnumerator Move(GridRoute path, int index)
        {
            busy = true;
            for (int i = 1; i < path.Stops.Count; i++)
            {
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
            var target = turnOrder[targetIndex]; int damage = target.Blocking ? 12 : 25;
            target.Health = Mathf.Max(0, target.Health - damage);
            message = $"{Active.Name} hits {target.Name} for {damage}.";
            if (target.Health > 0) return;
            Grid.Board.Units.Remove(target);
            if (targetIndex == 0) { Finish(false); return; }
            actors[targetIndex].gameObject.SetActive(false);
            if (!turnOrder.Exists(u => u != turnOrder[0] && u.Health > 0)) Finish(true);
        }

        private void Block()
        {
            if (!busy && actorIndex == 0 && Active.SpendAction())
            { Active.Blocking = true; message = "Blocking until your next turn. Movement remains available."; }
        }

        private void EndTurn()
        {
            if (busy || closing || actorIndex != 0) return;
            Active.EndTurn(); StartCoroutine(EnemyRound());
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
            actorIndex = 0; round++; Active.BeginTurn(); selected = Active.Position; attackMode = false;
            busy = false; message = "Your turn: saved movement + 3, action refreshed."; Refresh();
        }

        public void Finish(bool victory)
        {
            if (closing) return;
            closing = true; StopAllCoroutines(); finished?.Invoke(victory);
        }

        private void OnGUI()
        {
            if (Grid == null || closing) return;
            GUILayout.BeginArea(new Rect(Screen.width - PanelWidth + 8, 8, PanelWidth - 16, Screen.height - 16), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label($"WORLD GRID ENCOUNTER | Round {round}\n{Active.Name}'s turn\nMovement: {Active.Movement} | Action: {(Active.ActionAvailable ? "Ready" : "Spent")}");
            for (int i = 0; i < turnOrder.Count; i++) GUILayout.Label($"{(i == 0 ? "Player" : "Enemy")}: {turnOrder[i].Name} — {turnOrder[i].Health} HP");
            GUI.enabled = !busy && actorIndex == 0;
            if (GUILayout.Button("Move [M]", GUILayout.Height(30))) attackMode = false;
            GUI.enabled = !busy && actorIndex == 0 && Active.ActionAvailable;
            if (GUILayout.Button("Attack [R]", GUILayout.Height(30))) attackMode = true;
            if (GUILayout.Button("Block [B]", GUILayout.Height(30))) Block();
            GUI.enabled = !busy && actorIndex == 0;
            if (GUILayout.Button("Confirm [Enter]", GUILayout.Height(30))) Confirm();
            if (GUILayout.Button("End Turn [T] — bank movement", GUILayout.Height(30))) EndTurn();
            GUI.enabled = true;
            if (GUILayout.Button("Focus player [F]", GUILayout.Height(30))) cameraControls.FocusActive();
            if (GUILayout.Button("Leave battle / return to overworld", GUILayout.Height(30))) Finish(false);
            GUILayout.Label($"Mode: {(attackMode ? "Attack" : "Move")} | Cell {selected}\n{message}");
            GUILayout.Label("Click to select, again to confirm. Camera: WASD, right-drag look, left-drag pan, middle-drag orbit, wheel dolly.\n\nPrototype damage/HP and simple enemy turns. No skills, items, timed defense or rewards yet. Leaving/defeat resets the encounter; victory removes this enemy group.");
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
    }
}
