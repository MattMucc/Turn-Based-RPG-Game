using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.GridPrototype
{
    public sealed class GridPrototypeArena : MonoBehaviour
    {
        public Material SurfaceMaterial;
        public Camera ArenaCamera;
        public Shader OverlayShader;
        public float CellSize = 2f;
        public float ElevationStep = 1f;
        public GridBoard Board { get; private set; }
        public GridUnit Active => Board.Units[activeIndex];
        public bool Busy { get; private set; }
        private GridTerrainView terrainView;
        private GridCameraController cameraControls;
        private readonly Dictionary<Collider, Vector2Int> hits = new Dictionary<Collider, Vector2Int>();
        private readonly List<Transform> figures = new List<Transform>();
        private readonly List<Material> materials = new List<Material>();
        private Dictionary<Vector2Int, GridRoute> reachable;
        private LineRenderer routeLine;
        private Vector2Int selected;
        private readonly Vector2Int dummyPosition = new Vector2Int(7, 2);
        private int activeIndex, round = 1, dummyDamage;
        private bool attackMode;
        private string message = "Select a highlighted cell to preview; select again to move.";
        private CursorLockMode oldLock;
        private bool oldVisible;
        private Vector2 panelScroll;
        private float PanelWidth => Mathf.Min(330, Screen.width * 0.42f);

        private void Start()
        {
            oldLock = Cursor.lockState; oldVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            Board = new GridBoard(10, 8);
            for (int z = 0; z < 8; z++) Board.Cells[4, z].Ground = Ground.Water;
            for (int z = 6; z < 8; z++) Board.Cells[5, z].Ground = Ground.Water;
            Board.Cells[2, 3].Ground = Ground.Obstacle;
            Board.Cells[6, 3].Ground = Ground.Hazard;
            Board.Cells[6, 4].Ground = Ground.Hazard;
            for (int x = 7; x < 10; x++) for (int z = 4; z < 8; z++) Board.Cells[x, z].Height = x == 9 ? 2 : 1;
            // Dummy is solid terrain for movement, and a separately handled practice target.
            Board.At(dummyPosition).Ground = Ground.Obstacle;
            Board.Units.Add(new GridUnit("Scout", new Vector2Int(1, 1)));
            Board.Units.Add(new GridUnit("Vanguard", new Vector2Int(1, 3)));
            Board.Units.Add(new GridUnit("Arcanist", new Vector2Int(1, 5)));
            terrainView = new GameObject("Terrain and tactical overlay").AddComponent<GridTerrainView>();
            terrainView.transform.SetParent(transform, false);
            if (!OverlayShader) OverlayShader = Shader.Find("Game/GridOverlay");
            terrainView.Build(Board, CellSize, ElevationStep, SurfaceMaterial, OverlayShader);
            for (int x = 0; x < Board.Width; x++) for (int z = 0; z < Board.Depth; z++)
            {
                var p = new Vector2Int(x, z);
                if (Board.At(p).Ground == Ground.Obstacle)
                {
                    var obstacle = MakePrimitive(p == dummyPosition ? "Practice target" : "Solid obstacle", PrimitiveType.Cube,
                        World(p) + Vector3.up * 0.8f, new Vector3(1.2f, 1.6f, 1.2f), new Color(0.7f, 0.25f, 0.22f));
                    hits[obstacle.GetComponent<Collider>()] = p;
                }
            }
            Color[] colors = { Color.cyan, new Color(1, 0.75f, 0.2f), new Color(0.8f, 0.5f, 1) };
            for (int i = 0; i < Board.Units.Count; i++)
            {
                var figure = MakePrimitive(Board.Units[i].Name, PrimitiveType.Capsule,
                    World(Board.Units[i].Position) + Vector3.up, Vector3.one * 0.8f, colors[i]);
                // Cell raycasts must reach the tile below a figure.
                figure.GetComponent<Collider>().enabled = false;
                figures.Add(figure.transform);
            }
            routeLine = new GameObject("Route preview").AddComponent<LineRenderer>();
            routeLine.transform.SetParent(transform);
            routeLine.sharedMaterial = NewMaterial(Color.yellow);
            routeLine.startWidth = routeLine.endWidth = 0.1f;
            cameraControls = ArenaCamera.GetComponent<GridCameraController>();
            if (!cameraControls) cameraControls = ArenaCamera.gameObject.AddComponent<GridCameraController>();
            cameraControls.FocusTarget = figures[activeIndex];
            Active.BeginTurn(); selected = Active.Position; RefreshPreview();
        }

        private Material NewMaterial(Color color)
        {
            var material = new Material(SurfaceMaterial); material.color = color;
            materials.Add(material); return material;
        }

        private GameObject MakePrimitive(string label, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            var obj = GameObject.CreatePrimitive(type); obj.name = label;
            obj.transform.SetParent(transform); obj.transform.position = position; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = NewMaterial(color); return obj;
        }

        private Vector3 World(Vector2Int p) => new Vector3(p.x * CellSize, Board.At(p).Height * ElevationStep, p.y * CellSize);
        private void RefreshPreview()
        {
            reachable = Board.Reachable(Active, Active.Movement);
            terrainView.Highlight(reachable, selected);
            routeLine.positionCount = 0;
            if (reachable.TryGetValue(selected, out var route))
            {
                routeLine.positionCount = route.Stops.Count;
                for (int i = 0; i < route.Stops.Count; i++) routeLine.SetPosition(i, World(route.Stops[i]) + Vector3.up * 0.25f);
            }
        }

        private void Update()
        {
            if (Board == null) return;
            ArenaCamera.rect = new Rect(0, 0, 1 - PanelWidth / Screen.width, 1);
            // Consume clicks during movement too, so a drag/release never queues a move.
            bool clicked = cameraControls.ConsumeClick(out var pointer);
            if (Busy) return;
            var keyboard = Keyboard.current; var pad = Gamepad.current;
            Vector2Int shift = Vector2Int.zero;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame) shift.x--;
                if (keyboard.rightArrowKey.wasPressedThisFrame) shift.x++;
                if (keyboard.upArrowKey.wasPressedThisFrame) shift.y++;
                if (keyboard.downArrowKey.wasPressedThisFrame) shift.y--;
                if (keyboard.mKey.wasPressedThisFrame) attackMode = false;
                if (keyboard.rKey.wasPressedThisFrame) attackMode = true;
                if (keyboard.bKey.wasPressedThisFrame) Block();
                if (keyboard.tKey.wasPressedThisFrame) { EndTurn(); return; }
            }
            if (pad != null)
            {
                if (pad.dpad.left.wasPressedThisFrame) shift.x--;
                if (pad.dpad.right.wasPressedThisFrame) shift.x++;
                if (pad.dpad.up.wasPressedThisFrame) shift.y++;
                if (pad.dpad.down.wasPressedThisFrame) shift.y--;
                if (pad.buttonWest.wasPressedThisFrame) attackMode = !attackMode;
                if (pad.buttonNorth.wasPressedThisFrame) Block();
                if (pad.startButton.wasPressedThisFrame) { EndTurn(); return; }
            }
            if (shift != Vector2Int.zero)
            {
                selected = new Vector2Int(Mathf.Clamp(selected.x + shift.x, 0, Board.Width - 1),
                    Mathf.Clamp(selected.y + shift.y, 0, Board.Depth - 1)); RefreshPreview();
            }
            if ((keyboard != null && keyboard.enterKey.wasPressedThisFrame) || (pad != null && pad.buttonSouth.wasPressedThisFrame)) Confirm();
            if (clicked)
            {
                if (Physics.Raycast(ArenaCamera.ScreenPointToRay(pointer), out var hit, 500))
                {
                    Vector2Int p;
                    if (!hits.TryGetValue(hit.collider, out p)
                        && (hit.collider != terrainView.TerrainCollider || !terrainView.TryCell(hit.point, out p))) return;
                    if (p == selected) Confirm();
                    else { selected = p; RefreshPreview(); }
                }
            }
        }

        public void Confirm()
        {
            if (Busy) return;
            if (attackMode)
            {
                if (selected != dummyPosition || !AdjacentToDummy()) { message = "Attack requires an adjacent practice target."; return; }
                if (!Active.SpendAction()) { message = "Action already spent. You can still move or end your turn."; return; }
                dummyDamage += 10; message = "Practice attack: 10 damage. Remaining movement is still available.";
                attackMode = false; return;
            }
            if (!Board.TryMove(Active, selected, out var route)) { message = "Cannot move there: check budget, occupancy, gap range and elevation."; return; }
            StartCoroutine(AnimateRoute(route, figures[activeIndex]));
        }

        private bool AdjacentToDummy()
        {
            var delta = Active.Position - dummyPosition;
            return Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) == 1
                && Board.At(Active.Position).Height == Board.At(dummyPosition).Height;
        }

        private IEnumerator AnimateRoute(GridRoute route, Transform figure)
        {
            Busy = true;
            for (int i = 1; i < route.Stops.Count; i++)
            {
                Vector3 from = World(route.Stops[i - 1]) + Vector3.up, to = World(route.Stops[i]) + Vector3.up;
                bool jump = (route.Stops[i] - route.Stops[i - 1]).sqrMagnitude > 2 || from.y != to.y;
                for (float time = 0; time < 1; time += Time.deltaTime * 4)
                {
                    figure.position = Vector3.Lerp(from, to, time) + Vector3.up * (jump ? Mathf.Sin(time * Mathf.PI) : 0);
                    yield return null;
                }
                figure.position = to;
            }
            Busy = false; message = $"Moved for {route.Cost} points. End Turn when you are ready."; RefreshPreview();
        }

        public void Block()
        {
            if (Busy || !Active.SpendAction()) return;
            Active.Blocking = true;
            message = "Blocking until next turn. Movement still available; hazard damage halved in this sandbox.";
        }

        public void EndTurn()
        {
            if (Busy) return;
            Active.EndTurn(); activeIndex = (activeIndex + 1) % Board.Units.Count;
            if (activeIndex == 0) round++;
            Active.BeginTurn(); selected = Active.Position; attackMode = false;
            cameraControls.FocusTarget = figures[activeIndex];
            message = $"{Active.Name}: saved movement + 3. Action refreshed."; RefreshPreview();
        }

        private void OnGUI()
        {
            if (Board == null) return;
            GUILayout.BeginArea(new Rect(Screen.width - PanelWidth + 8, 8, PanelWidth - 16, Screen.height - 16), GUI.skin.box);
            panelScroll = GUILayout.BeginScrollView(panelScroll);
            GUILayout.Label("GRID TRAVERSAL PROTOTYPE");
            GUILayout.Label($"Round {round} | {Active.Name}\nMovement: {Active.Movement} | Action: {(Active.ActionAvailable ? "Ready" : "Spent")}\nHealth: {Active.Health} | Jump: {Active.JumpRange} cells / {Active.JumpHeight} level");
            foreach (var unit in Board.Units) GUILayout.Label($"{(unit == Active ? "> " : "")}{unit.Name}: {unit.Movement} movement points banked");
            GUILayout.Space(8);
            GUI.enabled = !Busy;
            if (GUILayout.Button(attackMode ? "Move [M]" : "Move [M] - selected", GUILayout.Height(32))) attackMode = false;
            GUI.enabled = !Busy && Active.ActionAvailable;
            if (GUILayout.Button("Attack target [R]", GUILayout.Height(32))) attackMode = true;
            if (GUILayout.Button("Block [B]", GUILayout.Height(32))) Block();
            GUI.enabled = !Busy;
            if (GUILayout.Button("Confirm selected cell [Enter]", GUILayout.Height(32))) Confirm();
            if (GUILayout.Button("End Turn [T] - keep unused movement", GUILayout.Height(36))) EndTurn();
            GUI.enabled = true;
            if (GUILayout.Button("Focus active character [F]", GUILayout.Height(30))) cameraControls.FocusActive();
            GUILayout.Space(8);
            string cost = reachable.TryGetValue(selected, out var route) ? route.Cost.ToString() : "unreachable";
            GUILayout.Label($"Selected: {selected.x}, {selected.y}\nRoute cost: {cost}\nMode: {(attackMode ? "Attack" : "Move")}\nTarget damage dealt: {dummyDamage}");
            GUILayout.Label(message);
            GUILayout.Space(8);
            GUILayout.Label("Click to preview; click again to confirm. Dragging never selects. Arrows select cells.\n\nCamera: WASD fly, Q/E lower/raise, Shift faster. Right-drag look, left-drag pan, wheel dolly. F focuses active unit; middle-drag orbits character when focused, otherwise the view center.\n\nController: D-pad select; South confirm; West move/attack; North block; Start end turn.\n\nGreen: reachable | Blue: river\nOrange: hazard (10 damage per entry)\nRed: obstacle/target\n\nTraversal sandbox: no enemy AI, skills, inventory or timed defenses yet. Hazard cannot KO units here.");
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            foreach (var material in materials) if (material) Destroy(material);
            Cursor.lockState = oldLock; Cursor.visible = oldVisible;
        }
    }
}
