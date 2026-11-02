using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.GridPrototype
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
        private Shader overlayShader;
        private BattleTargetHighlight targetHighlight;
        private readonly List<BattleTarget> battleTargets = new();
        public IReadOnlyList<BattleTarget> Targetables => battleTargets;
        public BattleTargetFocus TargetFocus { get; } = new BattleTargetFocus();
        private Action<bool> finished;
        private Dictionary<Vector2Int, GridRoute> reachable;
        private Vector2Int selected;
        private int actorIndex, round = 1;
        private bool busy, closing, playerTurning;
        private Quaternion playerTurnStart, playerTurnEnd;
        private float playerTurnElapsed;
        private const float PlayerTurnDuration = 0.3f;
        private string message;
        public BattleActionLoadout Loadout { get; private set; }
        public IReadOnlyList<GridUnit> Combatants => turnOrder;
        public Camera BattleCamera => view;
        public int CurrentActorIndex => actorIndex;
        public int Round => round;
        public bool AutoCamera { get; private set; } = true;
        public DefensePrototypeSettings DefenseSettings { get; private set; }
        public DefenseImpact CurrentImpact { get; private set; }
        public bool SignatureActive { get; private set; }
        public PressQte CurrentQte { get; private set; }
        public string SignatureName { get; private set; }
        public int SignatureDamage { get; private set; }
        private readonly PressQteInput qteInput = new PressQteInput();
        private DefenseVisual signatureVisual;
        private float signatureFeedbackElapsed;
        private int qteOpenedFrame;
        public bool DefenseActive { get; private set; }
        public string DefenseCaption { get; private set; }
        public string DefenseFeedback { get; private set; }
        private int defenseDamage;
        private readonly DefenseSlowMotion defenseSlowMotion = new DefenseSlowMotion();
        private bool perfectParries, cuePlayed;
        private DefenseVisual attackerVisual, defenderVisual;
        private AudioSource defenseAudio;
        private AudioClip windupCue, readyCue, successCue, failedCue;
        public BattleActionOption HoveredAction { get; private set; }
        private readonly Dictionary<int, int> hitDamage = new();
        private readonly Dictionary<int, float> hitTime = new();
        public Vector3 HealthBarPosition(int index) => actors[index].position + Vector3.up * (offsets[index] + 0.7f);
        public string DisplayName(int index) => (index == 0 ? "" : $"Enemy {index}: ") + turnOrder[index].Name.Replace("(Clone)", "").Trim();
        public int AssignedHits(int index) => targets.FindAll(t => t == turnOrder[index].Position).Count;
        public int RecentDamage(int index) => hitTime.TryGetValue(index, out float time) && Time.time - time < 1.2f ? hitDamage[index] : 0;
        public List<int> UpcomingTurns()
        {
            var result = new List<int>();
            for (int offset = 0; offset < turnOrder.Count; offset++)
            { int i = (actorIndex + offset) % turnOrder.Count; if (turnOrder[i].Health > 0) result.Add(i); }
            return result;
        }
        public void SetAutoCamera(bool value)
        { AutoCamera = value; if (value) FocusActor(actorIndex); else cameraControls.ReleaseFocus(); }
        private void FocusActor(int index)
        { cameraControls.FocusTarget = actors[index]; if (AutoCamera) cameraControls.SmoothFocus(actors[index]); }
        public void FocusCharacter(int index)
        { if (index >= 0 && index < actors.Count) cameraControls.SmoothFocus(actors[index]); }
        public Transform Actor(int index) => actors[index];
        public BattleCharacterInfo Info(int index) => actors[index].GetComponent<BattleCharacterInfo>();
        public event Action<int, Vector2> ContextRequested;
        public bool InspectionOpen;
        public bool ExternalControllerActive;
        public bool ControllerInputLocked => busy || closing || playerTurning || SignatureActive || DefenseActive;
        public event Action<BattleTarget, Vector2> TargetContextRequested;
        public bool ReadyToResolve => ChosenAction != null && targets.Count > 0 &&
            (ChosenAction.Pattern != BattleTargetPattern.Multiple || targets.Count == TargetLimit);
        private void RecordHit(int index, int damage)
        { hitDamage[index] = damage; hitTime[index] = Time.time; }
        public BattleActionOption ChosenAction { get; private set; }
        private readonly List<Vector2Int> targets = new List<Vector2Int>();
        public bool GridInputEnabled = true;
        public bool KeyboardGridInputEnabled = true;
        public Func<Vector2, bool> PointerOverUI;
        public bool PlayerReady => !busy && !closing && !playerTurning && !SignatureActive && actorIndex == 0;
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
            Grid = grid; finished = onFinished; overlayShader = shader;
            // Pick the closest encounter enemy before grid placement moves either actor.
            int nearestEnemy = -1; float nearestDistance = float.PositiveInfinity;
            for (int i = 1; i < figures.Count; i++)
            {
                Vector3 separation = figures[i].position - figures[0].position; separation.y = 0;
                if (separation.sqrMagnitude < nearestDistance)
                { nearestDistance = separation.sqrMagnitude; nearestEnemy = i; }
            }
            for (int i = 0; i < figures.Count; i++)
            {
                actors.Add(figures[i]); offsets.Add(footOffsets[i]);
                if (!figures[i].GetComponent<BattleCharacterInfo>()) figures[i].gameObject.AddComponent<BattleCharacterInfo>();
                var unit = new GridUnit(figures[i].name, positions[i]); turnOrder.Add(unit); Grid.Board.Units.Add(unit);
                battleTargets.Add(new BattleTarget(figures[i], unit, i));
                figures[i].position = Grid.Point(positions[i]) + Vector3.up * footOffsets[i];
            }
            for (int i = 1; i < actors.Count; i++)
                actors[i].rotation = GridTargeting.Facing(Grid.Point(positions[0]) - Grid.Point(positions[i]));
            if (nearestEnemy >= 0)
            {
                BeginPlayerFacing(actors[nearestEnemy].position);
            }
            DefenseSettings = figures[0].GetComponent<DefensePrototypeSettings>();
            if (!DefenseSettings) DefenseSettings = figures[0].gameObject.AddComponent<DefensePrototypeSettings>();
            Loadout = figures[0].GetComponent<BattleActionLoadout>();
            if (!Loadout) Loadout = figures[0].gameObject.AddComponent<BattleActionLoadout>();
            var cameraObject = new GameObject("Encounter camera", typeof(Camera)); cameraObject.transform.SetParent(transform);
            view = cameraObject.GetComponent<Camera>(); view.CopyFrom(sourceCamera); view.enabled = true; view.rect = new Rect(0, 0, 1, 1);
            Vector3 center = Grid.Point(positions[0]);
            view.transform.position = center + new Vector3(0, 15, -14); view.transform.LookAt(center);
            cameraControls = cameraObject.AddComponent<GridCameraController>(); cameraControls.FocusTarget = actors[0];
            cameraControls.PointerOverUI = p => InspectionOpen || (PointerOverUI?.Invoke(p) ?? false);
            cameraControls.ContextClick = p => { if (TryPickActor(p, out int i)) ContextRequested?.Invoke(i, p); };
            foreach (var prop in FindObjectsByType<BattleDestructible>(FindObjectsSortMode.None)) RegisterDestructible(prop);
            RebuildOverlay();
            targetHighlight = new GameObject("Hovered battle target").AddComponent<BattleTargetHighlight>();
            targetHighlight.transform.SetParent(transform); targetHighlight.Initialize(shader);
            Active.BeginTurn(); selected = Active.Position;
            message = "Choose a command or move to a green tile."; Refresh();
        }

        public void RegisterDestructible(BattleDestructible prop)
        {
            if (!prop || battleTargets.Exists(t => t.Destructible == prop)) return;
            var target = new BattleTarget(prop);
            if (!Grid.TryCell(target.Bounds.center, out _)) return;
            battleTargets.Add(target);
            if (overlay) { Grid.RefreshSurfaces(); RebuildOverlay(); Refresh(); }
        }
        private void RebuildOverlay()
        {
            if (overlay) { overlay.gameObject.SetActive(false); if (Application.isPlaying) Destroy(overlay.gameObject); else DestroyImmediate(overlay.gameObject); }
            overlay = new GameObject("Grid over world terrain").AddComponent<SampledGridOverlay>();
            overlay.transform.SetParent(transform); overlay.Build(Grid, overlayShader);
        }
        private bool TargetableObstacle(Vector2Int cell) => Grid.Board.Contains(cell) && Grid.HasSurface[cell.x, cell.y]
            && battleTargets.Exists(t => t.Alive && t.Destructible && t.Occupies(Grid, cell));
        public bool CanAim(Vector2Int cell, BattleActionOption action) => action != null && Grid.Board.Contains(cell)
            && Grid.HasSurface[cell.x, cell.y] && GridTargeting.CanTarget(Grid.Board, Active.Position, cell, action, Sight, TargetableObstacle);
        public HashSet<Vector2Int> ActionFootprint(Vector2Int cell, BattleActionOption action)
            => GridTargeting.Footprint(Grid.Board, Active.Position, cell, action, Sight, TargetableObstacle);

        private void Refresh()
        {
            if (ChosenAction != null) { Preview(); return; }
            if (HoveredAction != null && ActionReady) { PreviewReach(HoveredAction); return; }
            reachable = Grid.Board.Reachable(Active, Active.Movement);
            overlay.Show(actorIndex == 0 ? reachable : new Dictionary<Vector2Int, GridRoute>(), selected);
        }

        private void BeginPlayerFacing(Vector3 target)
        {
            Vector3 direction = target - actors[0].position; direction.y = 0;
            if (direction.sqrMagnitude <= 0.0001f) return;
            playerTurnStart = actors[0].rotation; playerTurnEnd = Quaternion.LookRotation(direction, Vector3.up);
            playerTurnElapsed = 0;
            playerTurning = Quaternion.Angle(playerTurnStart, playerTurnEnd) > 0.1f;
        }
        public void AdvanceFacing(float deltaTime)
        {
            if (!playerTurning || closing) return;
            playerTurnElapsed += Mathf.Max(0, deltaTime);
            float t = Mathf.Clamp01(playerTurnElapsed / PlayerTurnDuration);
            actors[0].rotation = Quaternion.Slerp(playerTurnStart, playerTurnEnd, t * t * (3 - 2 * t));
            if (t >= 1) playerTurning = false;
        }

        private void Update()
        {
            if (Grid == null || closing) return;
            if (Application.isFocused) AdvanceFacing(Time.deltaTime);
            if (DefenseActive && Application.isFocused && !InspectionOpen)
            {
                var keyboard = Keyboard.current; var gamepad = Gamepad.current;
                bool dodge = (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
                    || (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);
                bool parry = (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                    || (gamepad != null && gamepad.rightShoulder.wasPressedThisFrame);
                AdvanceDefense(Time.deltaTime, dodge, parry);
            }
            if (SignatureActive && CurrentQte != null && Application.isFocused && !InspectionOpen)
            {
                qteInput.Read(Keyboard.current, Gamepad.current, out bool correct, out bool wrong, CurrentQte.Expected, CurrentQte.Mode == QteMode.DualInput ? CurrentQte.Secondary : (QteButton?)null);
                AdvanceSignature(Time.unscaledDeltaTime, Time.frameCount > qteOpenedFrame && correct, Time.frameCount > qteOpenedFrame && wrong, PressQteInput.IsHeld(Keyboard.current, Gamepad.current, CurrentQte.Expected), CurrentQte.Mode == QteMode.DualInput && PressQteInput.IsHeld(Keyboard.current, Gamepad.current, CurrentQte.Secondary));
            }
            if (ExternalControllerActive)
            {
                if (targetHighlight) targetHighlight.Show(TargetFocus.Hovered, Grid);
                return; // Controller navigation supplies its own pointer and consumes gameplay input once.
            }
            bool click = cameraControls.ConsumeClick(out var pointer);
            UpdateTargetFocus(click);
            if (targetHighlight) targetHighlight.Show(TargetFocus.Hovered, Grid);
            if (!PlayerReady || !GridInputEnabled || InspectionOpen) return;
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
            if (KeyboardGridInputEnabled && ((k != null && k.enterKey.wasPressedThisFrame) || (pad != null && !pad.leftTrigger.isPressed && pad.buttonSouth.wasPressedThisFrame))) Confirm();
            if (Mouse.current != null && !(PointerOverUI?.Invoke(Mouse.current.position.ReadValue()) ?? false)
                && TryPick(Mouse.current.position.ReadValue(), out var hovered))
            {
                if (ChosenAction != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0)
                { selected = hovered; Preview(); }
                if (click)
                {
                    if (ChosenAction != null) { selected = hovered; Confirm(); }
                    else if (TargetFocus.Hovered != null) { selected = hovered; Refresh(); }
                    else if (selected == hovered) Confirm();
                    else { selected = hovered; Refresh(); }
                }
            }
        }

        public void ControllerPoint(Vector2 pointer, bool overUI, bool activate)
        {
            if (ControllerInputLocked || InspectionOpen) { TargetFocus.Hover(null); return; }
            BattleTarget target = null;
            if (!overUI && TryPickTarget(pointer, out var picked, out var targetCell)
                && (ChosenAction == null || CanAim(targetCell, ChosenAction))) target = picked;
            TargetFocus.Hover(target);
            if (overUI) return;
            if (!TryPick(pointer, out var cell)) { if (activate) TargetFocus.Dismiss(); return; }
            if (ChosenAction != null)
            {
                selected = cell; Preview();
                if (activate && PlayerReady) Confirm();
                return;
            }
            if (!activate) return;
            TargetFocus.Pin(target);
            if (target != null) { TargetContextRequested?.Invoke(target, pointer); return; }
            if (!PlayerReady || !GridInputEnabled) return;
            if (selected == cell) Confirm();
            else { selected = cell; Refresh(); }
        }

        public void ControllerGridStep(Vector2Int delta, bool activate = false)
        {
            if (!PlayerReady || InspectionOpen) return;
            selected = new Vector2Int(Mathf.Clamp(selected.x + delta.x, 0, Grid.Board.Width - 1),
                Mathf.Clamp(selected.y + delta.y, 0, Grid.Board.Depth - 1));
            Refresh();
            var target = battleTargets.Find(t => t.Alive && t.Occupies(Grid, selected));
            TargetFocus.Hover(ChosenAction == null || CanAim(selected, ChosenAction) ? target : null);
            if (!activate) return;
            if (ChosenAction != null) Confirm();
            else if (target != null)
            {
                TargetFocus.Pin(target);
                TargetContextRequested?.Invoke(target, view.WorldToScreenPoint(target.Bounds.center));
            }
            else Confirm();
        }

        private void UpdateTargetFocus(bool click)
        {
            var mouse = Mouse.current;
            if (InspectionOpen || mouse == null) { TargetFocus.Hover(null); return; }
            Vector2 pointer = mouse.position.ReadValue();
            bool overUI = PointerOverUI?.Invoke(pointer) ?? false;
            BattleTarget hovered = null;
            if (!overUI && TryPickTarget(pointer, out var target, out var cell)
                && (ChosenAction == null || CanAim(cell, ChosenAction))) hovered = target;
            TargetFocus.Hover(hovered);
            if (click) TargetFocus.Pin(hovered);
            if (overUI && mouse.leftButton.wasPressedThisFrame) TargetFocus.Dismiss();
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) TargetFocus.Dismiss();
        }
        public bool TryPickTarget(Vector2 pointer, out BattleTarget target, out Vector2Int cell)
        {
            Ray ray = view.ScreenPointToRay(pointer); target = null; cell = default;
            float nearest = Physics.Raycast(ray, out var hit, 500, ~0, QueryTriggerInteraction.Ignore) ? hit.distance + 0.03f : 500;
            foreach (var candidate in battleTargets)
            {
                if (!candidate.Alive) continue;
                foreach (var renderer in candidate.Renderers)
                {
                    if (!renderer || !renderer.enabled || !renderer.bounds.IntersectRay(ray, out float distance) || distance > nearest) continue;
                    Vector2Int candidateCell;
                    if (candidate.Unit != null) candidateCell = candidate.Unit.Position;
                    else if (!Grid.TryCell(ray.GetPoint(distance + 0.02f), out candidateCell)) continue;
                    if (!Grid.Board.Contains(candidateCell) || !candidate.Occupies(Grid, candidateCell)) continue;
                    nearest = distance; cell = candidateCell; target = candidate;
                }
            }
            return target != null;
        }
        public bool TryPickActor(Vector2 pointer, out int index)
        {
            index = -1;
            if (TryPickTarget(pointer, out var target, out _)) index = target.ActorIndex;
            return index >= 0;
        }
        private bool TryPick(Vector2 pointer, out Vector2Int cell)
        {
            if (TryPickTarget(pointer, out _, out cell)) return true;
            cell = default;
            return Physics.Raycast(view.ScreenPointToRay(pointer), out var hit, 500, ~0, QueryTriggerInteraction.Ignore)
                && Grid.TryCell(hit.point, out cell);
        }
        private bool Sight(Vector2Int from, Vector2Int to)
        {
            if (from == to) return true;
            Vector3 start = Grid.Point(from) + Vector3.up, end = Grid.Point(to) + Vector3.up;
            var delta = end - start;
            foreach (var hit in Physics.RaycastAll(start, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                var prop = hit.collider.GetComponentInParent<BattleDestructible>();
                // The intended object can be hit, but other intervening scenery still blocks the shot.
                if (prop && battleTargets.Exists(t => t.Destructible == prop && (t.Occupies(Grid, from) || t.Occupies(Grid, to)))) continue;
                return false;
            }
            return true;
        }

        public bool ChooseAction(BattleActionOption option)
        {
            if (!ActionReady || option == null || !option.Available || !Loadout.Options.Contains(option)) return false;
            HoveredAction = null; ChosenAction = option; targets.Clear(); selected = Active.Position;
            message = "Choose any cell in reach, including empty cells or allies. Multi-target actions start when all hits are assigned."; Refresh(); return true;
        }

        public void CancelAction()
        { if (busy && actorIndex == 0 && !Active.ActionAvailable) return; HoveredAction = null; ChosenAction = null; targets.Clear(); if (Grid != null && !closing) Refresh(); }

        public void HoverAction(BattleActionOption option)
        {
            if (ChosenAction != null) return;
            HoveredAction = ActionReady && option != null && option.Available && Loadout.Options.Contains(option) ? option : null;
            Refresh();
        }
        private HashSet<Vector2Int> Reach(BattleActionOption option)
        {
            var result = new HashSet<Vector2Int>();
            for (int x = 0; x < Grid.Board.Width; x++) for (int y = 0; y < Grid.Board.Depth; y++)
            {
                var p = new Vector2Int(x, y);
                if (CanAim(p, option)) result.Add(p);
            }
            return result;
        }
        private void PreviewReach(BattleActionOption option)
        { overlay.ShowTargeting(Reach(option), new HashSet<Vector2Int>(), new Vector2Int(-1, -1)); }

        private void Preview()
        {
            var range = Reach(ChosenAction);
            var impact = new HashSet<Vector2Int>();
            foreach (var cell in targets) impact.UnionWith(ActionFootprint(cell, ChosenAction));
            // Once an anchor is selected its footprint stays fixed until another click.
            if (targets.Count == 0)
                impact.UnionWith(ActionFootprint(selected, ChosenAction));
            overlay.ShowTargeting(range, impact, selected);
        }

        public bool ToggleTarget(Vector2Int cell)
        {
            if (!ActionReady || ChosenAction == null) return false;
            if (!CanAim(cell, ChosenAction)) { message = "Choose a cell in reach with a clear attack path."; return false; }
            if (ChosenAction.Pattern != BattleTargetPattern.Multiple)
            { bool alreadySelected = targets.Contains(cell); targets.Clear(); if (!alreadySelected) targets.Add(cell); }
            else if (targets.Count < TargetLimit) targets.Add(cell);
            else { message = "All hits assigned. Undo Hit or Clear Targets to revise."; return false; }
            selected = cell;
            message = ChosenAction.Pattern == BattleTargetPattern.Multiple
                ? "Click any reachable cell to add a hit. Undo Hit removes the last assignment."
                : "Target selected.";
            Preview(); return true;
        }

        public void UndoTarget()
        { if (!ActionReady || targets.Count == 0) return; targets.RemoveAt(targets.Count - 1); Preview(); }
        public void ClearTargets()
        { if (!ActionReady || ChosenAction == null) return; targets.Clear(); Preview(); }

        public bool CommitAction()
        {
            if (!TryPrepareAction(out var sequence)) return false;
            StartCoroutine(sequence); return true;
        }

        // Preparation is atomic. The returned sequence is also stepped in EditMode tests.
        public bool TryPrepareAction(out IEnumerator sequence)
        {
            sequence = null;
            if (!ActionReady || !ReadyToResolve || !ChosenAction.Available) return false;
            foreach (var cell in targets) if (!CanAim(cell, ChosenAction)) return false;
            var action = ChosenAction;
            var aimedCells = new List<Vector2Int>(targets);
            var batches = new List<List<BattleTarget>>();
            foreach (var cell in aimedCells)
            {
                var footprint = ActionFootprint(cell, action);
                var hit = new List<BattleTarget>();
                foreach (var target in battleTargets)
                    if (target.Alive && footprint.Overlaps(TargetCells(target))) hit.Add(target);
                batches.Add(hit);
            }
            if (!Active.SpendAction()) return false;
            if (action.Category == BattleActionCategory.Item) action.Quantity--;
            busy = true;
            if (action.SignatureQte)
            {
                SignatureActive = true; InspectionOpen = false; SignatureName = action.Name;
                cameraControls.KeyboardInputBlocked = true;
                sequence = ResolveSignature(batches, aimedCells, action);
            }
            else sequence = ResolveHits(batches, aimedCells, action.Damage, action.Pattern == BattleTargetPattern.Multiple);
            return true;
        }
        public void AdvanceSignature(float deltaTime, bool correct = false, bool wrong = false, bool held = false, bool secondaryHeld = false)
        {
            if (!SignatureActive || CurrentQte == null || closing) return;
            bool wasComplete = CurrentQte.Complete;
            CurrentQte.Advance(deltaTime, correct, wrong, held, secondaryHeld);
            if (!wasComplete && CurrentQte.Complete)
                PlayDefenseCue(CurrentQte.Result == PressQteResult.Success ? successCue : failedCue);
            if (wasComplete) signatureFeedbackElapsed += Mathf.Max(0, deltaTime);
            if (CurrentQte.Complete) signatureVisual?.Impact(signatureFeedbackElapsed);
            else signatureVisual?.Windup(0.5f + (1 - CurrentQte.Fraction) * 0.5f);
        }

        private IEnumerator ResolveSignature(List<List<BattleTarget>> batches, List<Vector2Int> cells, BattleActionOption action)
        {
            Transform frameTarget = null;
            GameObject emptyAnchor = null;
            try
            {
                BeginPlayerFacing(Grid.Point(cells[0]));
                if (batches[0].Count > 0) frameTarget = batches[0][0].Transform;
                else
                {
                    emptyAnchor = new GameObject("Signature target cell");
                    emptyAnchor.transform.SetParent(transform);
                    emptyAnchor.transform.position = Grid.Point(cells[0]);
                    frameTarget = emptyAnchor.transform;
                }
                if (AutoCamera) cameraControls.SmoothDefenseFrame(actors[0], frameTarget);
                while (playerTurning || (AutoCamera && cameraControls.Transitioning)) yield return null;
                if (closing) yield break;
                signatureVisual = new DefenseVisual(actors[0]);
                EnsureDefenseAudio(); PlayDefenseCue(windupCue);
                CurrentQte = new PressQte(action.QteDuration, inputs: action.QteSequence, mode: action.QteType, requiredTaps: action.QteRequiredTaps, holdDuration: action.QteHoldDuration, progressDrainPerSecond: action.QteProgressDrainPerSecond, tapDrainDelay: action.QteTapDrainDelay);
                qteInput.Reset(Gamepad.current); qteOpenedFrame = Time.frameCount;
                signatureFeedbackElapsed = 0;
                while (!CurrentQte.Complete) yield return null;
                SignatureDamage = Mathf.Max(0, action.Damage) +
                    (CurrentQte.Result == PressQteResult.Success ? Mathf.Max(0, action.QteBonusDamage) : 0);
                while (signatureFeedbackElapsed < 0.45f) yield return null;
                signatureVisual.Reset();
                var hits = ResolveHits(batches, cells, SignatureDamage, action.Pattern == BattleTargetPattern.Multiple);
                while (hits.MoveNext()) yield return hits.Current;
            }
            finally
            {
                if (!closing && AutoCamera && cameraControls.CharacterFocused) cameraControls.SmoothFocus(actors[0]);
                ResetSignature();
                if (emptyAnchor) { if (Application.isPlaying) Destroy(emptyAnchor); else DestroyImmediate(emptyAnchor); }
            }
        }

        private void ResetSignature()
        {
            signatureVisual?.Reset(); signatureVisual = null;
            SignatureActive = false; CurrentQte = null; signatureFeedbackElapsed = 0;
            if (cameraControls) cameraControls.KeyboardInputBlocked = false;
        }

        private IEnumerable<Vector2Int> TargetCells(BattleTarget target)
        {
            if (target.Unit != null) { yield return target.Unit.Position; yield break; }
            for (int x = 0; x < Grid.Board.Width; x++) for (int y = 0; y < Grid.Board.Depth; y++)
            { var cell = new Vector2Int(x, y); if (target.Occupies(Grid, cell)) yield return cell; }
        }
        private IEnumerator ResolveHits(List<List<BattleTarget>> batches, List<Vector2Int> cells, int damage, bool sequential)
        {
            for (int i = 0; i < batches.Count; i++)
            {
                if (closing) yield break;
                Vector3 direction = Grid.Point(cells[i]) - Grid.Point(turnOrder[0].Position);
                if (direction.sqrMagnitude > 0.001f) actors[0].rotation = GridTargeting.Facing(direction);
                bool broken = false;
                foreach (var target in batches[i])
                {
                    if (!target.Alive) continue;
                    if (target.Unit != null)
                    {
                        var victim = target.Unit; int dealt = victim.Blocking ? damage / 2 : damage;
                        victim.Health = Mathf.Max(0, victim.Health - dealt); RecordHit(target.ActorIndex, dealt);
                        if (victim.Health == 0) { Grid.Board.Units.Remove(victim); target.Transform.gameObject.SetActive(false); }
                    }
                    else { target.Destructible.TakeDamage(damage); broken |= !target.Alive; }
                }
                if (broken) { Physics.SyncTransforms(); Grid.RefreshSurfaces(); RebuildOverlay(); Refresh(); }
                // Empty hits still spend a slot and keep their place in the sequence.
                if (sequential) yield return new WaitForSeconds(0.4f);
            }
            if (!sequential) yield return new WaitForSeconds(0.4f);
            busy = false; CancelAction();
            if (turnOrder[0].Health <= 0) { Finish(false); yield break; }
            ActionResolved?.Invoke();
            if (!turnOrder.Exists(u => u != turnOrder[0] && u.Health > 0)) Finish(true);
        }

        private bool CanStrike(GridUnit attacker, GridUnit target)
        {
            var delta = target.Position - attacker.Position;
            return Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) == 1
                && Grid.Board.CanTraverse(target, attacker.Position, target.Position);
        }

        private void Confirm()
        {
            if (!PlayerReady) return;
            if (ChosenAction != null)
            {
                if (ToggleTarget(selected) && ReadyToResolve) CommitAction(); return;
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

        // Called once per rendered frame; tests supply time/input explicitly without Play Mode.
        public void AdvanceDefense(float deltaTime, bool dodge = false, bool parry = false)
        {
            var impact = CurrentImpact;
            if (!DefenseActive || impact == null || closing) return;
            // Input responds to the cursor shown last frame, before this frame advances it.
            if (dodge || parry) impact.Attempt(dodge && parry ? DefenseChoice.Both : dodge ? DefenseChoice.Dodge : DefenseChoice.Parry);
            impact.Advance(defenseSlowMotion.Advance(deltaTime));
            if (!cuePlayed && impact.Elapsed >= impact.Duration - impact.ParryWindow)
            { cuePlayed = true; PlayDefenseCue(readyCue); }
            if (impact.Resolve())
            {
                var defender = turnOrder[0]; bool eligible = !defender.ReactionsDisabled && defender.Health > 0;
                int damage = impact.Damage(defenseDamage, defender.Blocking, eligible);
                var result = impact.ForTarget(eligible);
                perfectParries &= result == DefenseResult.Parried;
                defender.Health = Mathf.Max(0, defender.Health - damage);
                if (damage > 0) RecordHit(0, damage);
                DefenseFeedback = ResultLabel(result) + (damage > 0 ? $"  -{damage} HP" + (defender.Blocking ? " (Block)" : "") : "");
                PlayDefenseCue(damage == 0 ? successCue : failedCue);
                if (result == DefenseResult.Dodged && DefenseSettings.DodgeSlowMotion)
                    defenseSlowMotion.Begin(DefenseSettings.DodgeSlowMotionDuration, DefenseSettings.DodgeSlowMotionSpeed);
                if (result == DefenseResult.Parried && DefenseSettings.ParrySparks && Application.isPlaying)
                {
                    Vector3 contact = battleTargets[0].Bounds.center + actors[0].forward * 0.65f;
                    Vector3 towardCamera = (BattleCamera.transform.position - contact).normalized;
                    contact += towardCamera * 0.3f;
                    var burst = new GameObject("Successful parry sparks").AddComponent<ParryBurst>();
                    burst.transform.SetParent(transform, false); burst.Initialize(contact, towardCamera, Resources.Load<Shader>("ParryFlash") ?? overlayShader);
                }
            }
            if (!impact.Resolved)
            {
                attackerVisual?.Windup(impact.Elapsed / impact.Duration);
                if (impact.Attempted)
                {
                    var result = impact.ForTarget(!turnOrder[0].ReactionsDisabled);
                    DefenseFeedback = result == DefenseResult.Dodged ? "Dodge committed" : result == DefenseResult.Parried ? "Parry committed" : ResultLabel(result);
                }
            }
            else attackerVisual?.Impact(impact.Elapsed - impact.ResolutionTime);
            if (impact.Attempted && !turnOrder[0].ReactionsDisabled && impact.Choice != DefenseChoice.Both)
                defenderVisual?.React(impact.Choice, impact.Elapsed - impact.AttemptTime);
        }
        private static string ResultLabel(DefenseResult result)
        {
            switch (result)
            {
                case DefenseResult.Dodged: return "Dodged";
                case DefenseResult.Parried: return "Parried";
                case DefenseResult.TooEarly: return "Too early";
                case DefenseResult.TooLate: return "Too late";
                case DefenseResult.Unparryable: return "Cannot parry - dodge this hit";
                case DefenseResult.ConflictingInput: return "Choose one defense";
                case DefenseResult.Unable: return "Cannot dodge or parry";
                default: return "Watch the wind-up";
            }
        }
        private IEnumerator EnemyAttack(int index, bool combo)
        {
            busy = true; perfectParries = true; InspectionOpen = false;
            actors[index].rotation = GridTargeting.Facing(Grid.Point(turnOrder[0].Position) - Grid.Point(turnOrder[index].Position));
            BeginPlayerFacing(actors[index].position);
            if (AutoCamera) cameraControls.SmoothDefenseFrame(actors[0], actors[index]);
            // No defense clock runs while either character facing or camera framing is settling.
            while (playerTurning || (AutoCamera && cameraControls.Transitioning)) yield return null;
            DefenseActive = true; InspectionOpen = false;
            attackerVisual = new DefenseVisual(actors[index]); defenderVisual = new DefenseVisual(actors[0]);
            EnsureDefenseAudio();
            float[] windups = combo ? new[] { DefenseSettings.ComboFirstWindup, DefenseSettings.ComboSecondWindup, DefenseSettings.ComboThirdWindup }
                : new[] { DefenseSettings.SingleWindup };
            int hitDamage = combo ? DefenseSettings.ComboHitDamage : DefenseSettings.SingleDamage;
            for (int hit = 0; hit < windups.Length; hit++)
            {
                attackerVisual.Reset(); defenderVisual.Reset();
                CurrentImpact = DefenseSettings.CreateImpact(windups[hit], impactPosition: DefenseSettings.ImpactPositionFor(combo, hit)); defenseDamage = hitDamage; cuePlayed = false;
                DefenseCaption = $"{DisplayName(index)} > {DisplayName(0)}  |  Hit {hit + 1}/{windups.Length}";
                DefenseFeedback = turnOrder[0].ReactionsDisabled ? "Cannot dodge or parry" : "Watch the wind-up";
                PlayDefenseCue(windupCue);
                while (!CurrentImpact.Resolved) yield return null;
                while (CurrentImpact.Elapsed < CurrentImpact.ResolutionTime + 0.4f) yield return null;
                if (turnOrder[0].Health <= 0) { ResetDefense(); Finish(false); yield break; }
            }
            attackerVisual.Reset(); defenderVisual.Reset(); CurrentImpact = null;
            if (perfectParries && CanStrike(turnOrder[0], turnOrder[index]) && turnOrder[index].Health > 0)
            {
                int damage = Mathf.Max(0, DefenseSettings.CounterDamage);
                actors[0].rotation = GridTargeting.Facing(Grid.Point(turnOrder[index].Position) - Grid.Point(turnOrder[0].Position));
                turnOrder[index].Health = Mathf.Max(0, turnOrder[index].Health - damage); RecordHit(index, damage);
                DefenseFeedback = $"Counterattack!  {damage} damage"; PlayDefenseCue(successCue);
                if (turnOrder[index].Health == 0) { Grid.Board.Units.Remove(turnOrder[index]); actors[index].gameObject.SetActive(false); }
                yield return new WaitForSeconds(0.5f);
            }
            else if (perfectParries)
            { DefenseFeedback = "Perfect parry - counter out of reach"; yield return new WaitForSeconds(0.5f); }
            ResetDefense();
            if (!turnOrder.Exists(u => u != turnOrder[0] && u.Health > 0)) Finish(true);
        }
        private void ResetDefense()
        {
            attackerVisual?.Reset(); defenderVisual?.Reset(); attackerVisual = defenderVisual = null;
            defenseSlowMotion.Clear();
            DefenseActive = false; CurrentImpact = null;
        }
        private void EnsureDefenseAudio()
        {
            if (defenseAudio || !Application.isPlaying) return;
            defenseAudio = gameObject.AddComponent<AudioSource>(); defenseAudio.playOnAwake = false; defenseAudio.spatialBlend = 0;
            windupCue = CreateCue(330); readyCue = CreateCue(660); successCue = CreateCue(880); failedCue = CreateCue(160);
        }
        private static AudioClip CreateCue(float frequency)
        {
            const int rate = 22050, count = 1764;
            var samples = new float[count];
            for (int i = 0; i < count; i++) samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * i / rate) * Mathf.Sin(Mathf.PI * i / count) * 0.4f;
            var clip = AudioClip.Create("Prototype defense cue", count, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        private void PlayDefenseCue(AudioClip clip) { if (defenseAudio && clip) defenseAudio.PlayOneShot(clip, DefenseSettings.CueVolume); }
        private void OnApplicationFocus(bool focused)
        { if (!focused) qteInput.Reset(Gamepad.current); }
        private void OnDisable() { playerTurning = false; ResetDefense(); ResetSignature(); }
        private void OnDestroy()
        {
            ResetDefense(); ResetSignature();
            foreach (var clip in new[] { windupCue, readyCue, successCue, failedCue })
                if (clip) { if (Application.isPlaying) Destroy(clip); else DestroyImmediate(clip); }
        }

        public void Block()
        {
            if (PlayerReady && Active.SpendAction())
            { CancelAction(); Active.Blocking = true; ActionResolved?.Invoke(); message = "Blocking until your next turn. Movement remains available."; }
        }

        public void EndTurn()
        {
            if (!PlayerReady) return;
            CancelAction(); Active.EndTurn(); StartCoroutine(EnemyRound()); ActionResolved?.Invoke();
        }

        private IEnumerator EnemyRound()
        {
            busy = true;
            for (int i = 1; i < turnOrder.Count; i++)
            {
                if (turnOrder[i].Health <= 0) continue;
                actorIndex = i; Active.BeginTurn(); selected = Active.Position; FocusActor(i); Refresh();
                yield return new WaitForSeconds(0.6f);
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
                if (CanStrike(Active, turnOrder[0]) && Active.SpendAction())
                    yield return EnemyAttack(i, (i + round) % 2 != 0);
                if (closing) yield break;
                Active.EndTurn(); yield return new WaitForSeconds(0.9f);
            }
            actorIndex = 0; round++; Active.BeginTurn(); selected = Active.Position; FocusActor(0);
            busy = false; message = "Your turn: saved movement + 3, action refreshed."; Refresh();
        }

        public void Finish(bool victory)
        {
            if (closing) return;
            closing = true; playerTurning = false; ResetDefense(); ResetSignature(); StopAllCoroutines(); finished?.Invoke(victory);
        }

    }
}
