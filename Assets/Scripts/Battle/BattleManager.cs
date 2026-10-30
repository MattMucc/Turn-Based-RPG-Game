using System;
using System.Collections.Generic;
using UnityEngine;
using Tenebrasia.GridPrototype;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }
    public bool InBattle { get; private set; }
    [SerializeField] private BattleMenuUI battleMenu;
    [Header("Same-scene battle grid")]
    [SerializeField, Range(6, 24)] private int gridWidth = 14;
    [SerializeField, Range(6, 24)] private int gridDepth = 12;
    [SerializeField, Min(1)] private float cellSize = 2;
    [SerializeField] private Shader gridOverlayShader;
    private readonly List<ActorSnapshot> actors = new();
    private readonly Dictionary<Behaviour, bool> behaviours = new();
    private BattleGroup group;
    private GameObject encounterRoot;
    private Camera worldCamera;
    private bool cameraWasEnabled, oldCursorVisible;
    private CursorLockMode oldCursorLock;
    private PlayerInputReader input;
    private bool inputWasEnabled;
    private string notice;
    private float noticeUntil;

    private sealed class ActorSnapshot
    {
        public readonly Transform Transform;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly bool Active;
        public readonly float FootOffset;
        private readonly Dictionary<Collider, bool> colliders = new();
        private readonly List<(Rigidbody body, bool kinematic, Vector3 velocity, Vector3 angular)> bodies = new();
        public ActorSnapshot(Transform actor)
        {
            Transform = actor; Position = actor.position; Rotation = actor.rotation; Active = actor.gameObject.activeSelf;
            float lowest = float.PositiveInfinity;
            foreach (var collider in actor.GetComponentsInChildren<Collider>(true))
            {
                colliders[collider] = collider.enabled;
                if (collider.enabled && !collider.isTrigger && collider.gameObject.activeInHierarchy)
                    lowest = Mathf.Min(lowest, collider.bounds.min.y);
            }
            FootOffset = float.IsPositiveInfinity(lowest) ? 1 : Mathf.Max(0, Position.y - lowest);
            foreach (var body in actor.GetComponentsInChildren<Rigidbody>(true))
                bodies.Add((body, body.isKinematic, body.linearVelocity, body.angularVelocity));
        }
        public IEnumerable<Collider> Colliders => colliders.Keys;
        public void Suspend()
        {
            foreach (var pair in colliders) if (pair.Key) pair.Key.enabled = false;
            foreach (var state in bodies) if (state.body) state.body.isKinematic = true;
        }
        public void Restore()
        {
            if (!Transform) return;
            Transform.SetPositionAndRotation(Position, Rotation); Transform.gameObject.SetActive(Active);
            foreach (var state in bodies) if (state.body)
            {
                state.body.isKinematic = state.kinematic;
                if (!state.kinematic) { state.body.linearVelocity = state.velocity; state.body.angularVelocity = state.angular; }
            }
            foreach (var pair in colliders) if (pair.Key) pair.Key.enabled = pair.Value;
        }
    }

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
    }

    public bool TryStartBattle(BattleGroup source, List<GameObject> enemies)
    {
        if (InBattle || !source || !GameManager.Instance || !GameManager.Instance.PossessedCharacter) return false;
        var player = GameManager.Instance.PossessedCharacter;
        input = GameManager.Instance.Input; worldCamera = Camera.main;
        Shader shader = gridOverlayShader ? gridOverlayShader : Shader.Find("Tenebrasia/GridOverlay");
        if (!worldCamera || !input || !shader) { Notify("Battle cannot start: missing camera, input or grid shader."); return false; }
        actors.Clear(); actors.Add(new ActorSnapshot(player.transform));
        var seen = new HashSet<GameObject> { player.gameObject };
        foreach (var enemy in enemies)
            if (enemy && enemy.activeInHierarchy && seen.Add(enemy)) actors.Add(new ActorSnapshot(enemy.transform));
        if (actors.Count < 2) { actors.Clear(); return false; }
        var ignored = new List<Collider>(); var desired = new List<Vector3>();
        float radius = 0.5f, height = 2;
        foreach (var actor in actors)
        {
            ignored.AddRange(actor.Colliders); desired.Add(actor.Position);
            foreach (var controller in actor.Transform.GetComponentsInChildren<CharacterController>())
            {
                radius = Mathf.Max(radius, controller.radius * Mathf.Max(Mathf.Abs(controller.transform.lossyScale.x), Mathf.Abs(controller.transform.lossyScale.z)));
                height = Mathf.Max(height, controller.height * Mathf.Abs(controller.transform.lossyScale.y));
            }
        }
        Physics.SyncTransforms();
        Vector3 center = source.GridCenter ? source.GridCenter.position : player.transform.position;
        var grid = new TerrainBattleGrid(center, gridWidth, gridDepth, cellSize, source.GroundLayers, ignored, radius, height);
        // With an authored center, use formation preferences there, not a distant player's old position.
        if (source.GridCenter)
        {
            Vector3 shift = center - desired[0];
            for (int i = 0; i < desired.Count; i++) desired[i] += shift;
        }
        if (!grid.TryPlace(desired, out var positions))
        {
            actors.Clear(); Notify("Not enough connected, clear terrain for this group. Move to open ground or assign its Grid Center marker.");
            return false;
        }
        group = source; InBattle = true;
        oldCursorLock = Cursor.lockState; oldCursorVisible = Cursor.visible;
        cameraWasEnabled = worldCamera.enabled; inputWasEnabled = input.enabled;
        try
        {
            Suspend(player); if (GameManager.Instance.PlayerRotation) Suspend(GameManager.Instance.PlayerRotation);
            foreach (var enemy in FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None)) Suspend(enemy);
            foreach (var actor in actors) actor.Suspend();
            player.ResetMotion();
            if (battleMenu) battleMenu.Hide();
            input.SwitchToBattle(); input.enabled = false;
            worldCamera.enabled = false;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            encounterRoot = new GameObject("Runtime same-scene battle");
            var encounter = encounterRoot.AddComponent<InSceneGridBattle>();
            var figures = new List<Transform>(); var offsets = new List<float>();
            foreach (var actor in actors) { figures.Add(actor.Transform); offsets.Add(actor.FootOffset); }
            encounter.Initialize(grid, figures, offsets, positions, worldCamera, shader, CompleteBattle);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception); CompleteBattle(false);
            Notify("Battle setup failed; overworld state restored. Check the Console."); return false;
        }
    }

    private void Suspend(Behaviour component)
    { if (!behaviours.ContainsKey(component)) behaviours[component] = component.enabled; component.enabled = false; }

    public void EndBattle() => CompleteBattle(false);

    private void CompleteBattle(bool victory)
    {
        if (!InBattle) return;
        if (encounterRoot) { encounterRoot.SetActive(false); Destroy(encounterRoot); encounterRoot = null; }
        // Keep the encounter guard raised until all collider/behaviour state is restored.
        foreach (var actor in actors) actor.Restore();
        foreach (var pair in behaviours) if (pair.Key) pair.Key.enabled = pair.Value;
        behaviours.Clear();
        if (GameManager.Instance && GameManager.Instance.PossessedCharacter) GameManager.Instance.PossessedCharacter.ResetMotion();
        if (input) { input.SwitchToWorld(); input.enabled = inputWasEnabled; }
        if (worldCamera) worldCamera.enabled = cameraWasEnabled;
        Cursor.lockState = oldCursorLock; Cursor.visible = oldCursorVisible;
        if (group) group.CompleteEncounter(victory);
        actors.Clear(); group = null; InBattle = false;
        Notify(victory ? "Victory — enemy group cleared. Back in the overworld." : "Back in the overworld. Encounter reset; re-entry cooldown active.");
    }

    private void Notify(string text) { notice = text; noticeUntil = Time.unscaledTime + 7; Debug.Log("[Battle] " + text); }
    private void OnGUI()
    {
        if (!InBattle && Time.unscaledTime < noticeUntil)
            GUI.Box(new Rect(20, Screen.height - 80, Mathf.Min(720, Screen.width - 40), 60), notice);
    }
    private void OnDestroy()
    {
        if (Instance != this) return;
        if (InBattle) CompleteBattle(false);
        Instance = null;
    }
}
