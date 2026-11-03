using System;
using System.Collections.Generic;
using UnityEngine;
using Game.GridPrototype;

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
    [Header("Battle entry")]
    [SerializeField, Range(.4f, 3f)] private float battleEntryDuration = 2f;
    private readonly List<ActorSnapshot> actors = new();
    private readonly List<ActorSnapshot> reserveActors = new();
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
            var shape = actor.GetComponent<CharacterController>();
            FootOffset = float.IsPositiveInfinity(lowest) ? (shape ? Mathf.Max(0, (shape.height * .5f - shape.center.y) * Mathf.Abs(actor.lossyScale.y)) : 1) : Mathf.Max(0, Position.y - lowest);
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

    public bool TryStartBattle(BattleGroup source, List<GameObject> enemies, bool preemptive = false)
    {
        if (InBattle || !source || !GameManager.Instance || GameManager.Instance.WorldInputBlocked || !GameManager.Instance.PossessedCharacter) return false;
        var player = GameManager.Instance.PossessedCharacter;
        input = GameManager.Instance.Input; worldCamera = Camera.main;
        Shader shader = gridOverlayShader ? gridOverlayShader : Shader.Find("Game/GridOverlay");
        if (!worldCamera || !input || !shader) { Notify("Battle cannot start: missing camera, input or grid shader."); return false; }
        actors.Clear(); reserveActors.Clear();
        var party = GameManager.Instance.GetEncounterParty();
        foreach (var member in party) actors.Add(new ActorSnapshot(member.transform));
        int partySize = actors.Count;
        foreach (var member in GameManager.Instance.ReserveParty)
            if (member && !party.Contains(member) && reserveActors.Count < 3 && reserveActors.Count + partySize < 6 && !reserveActors.Exists(a => a.Transform == member.transform))
                reserveActors.Add(new ActorSnapshot(member.transform));
        var seen = new HashSet<GameObject>();
        foreach (var member in party) seen.Add(member.gameObject);
        foreach (var enemy in enemies)
            if (enemy && enemy.activeInHierarchy && seen.Add(enemy)) actors.Add(new ActorSnapshot(enemy.transform));
        if (actors.Count <= partySize) { actors.Clear(); return false; }
        var ignored = new List<Collider>(); var desired = new List<Vector3>();
        float radius = 0.5f, height = 2;
        foreach (var reserve in reserveActors) ignored.AddRange(reserve.Colliders);
        foreach (var actor in actors)
        {
            ignored.AddRange(actor.Colliders); desired.Add(actor.Position - Vector3.up * actor.FootOffset);
            foreach (var controller in actor.Transform.GetComponentsInChildren<CharacterController>(true))
            {
                radius = Mathf.Max(radius, controller.radius * Mathf.Max(Mathf.Abs(controller.transform.lossyScale.x), Mathf.Abs(controller.transform.lossyScale.z)));
                height = Mathf.Max(height, controller.height * Mathf.Abs(controller.transform.lossyScale.y));
            }
        }
        desired[0] = player.transform.position - Vector3.up * actors[0].FootOffset;
        Physics.SyncTransforms();
        if (!BattleFormation.TryGridBounds(desired, partySize, cellSize, gridWidth, gridDepth,
            source.GridCenter ? source.GridCenter.position : (Vector3?)null, out var center, out int width, out int depth, true))
        {
            actors.Clear(); reserveActors.Clear(); Notify("Enemy group is too spread out for one battle grid. Bring the group closer together."); return false;
        }
        var grid = new TerrainBattleGrid(center, width, depth, cellSize, source.GroundLayers, ignored, radius, height);
        var markers = new List<Vector3?>();
        if (source.CompanionFormationMarkers != null)
            foreach (var marker in source.CompanionFormationMarkers) markers.Add(marker ? marker.position : (Vector3?)null);
        if (!BattleFormation.TryPlan(grid, desired, partySize, player.transform.forward, markers, out var entry, out string failure, true))
        {
            actors.Clear(); reserveActors.Clear(); Notify("Battle could not start: " + failure);
            return false;
        }
        if (GameManager.Instance.ProgressionMenu) GameManager.Instance.ProgressionMenu.Close();
        group = source; InBattle = true;
        oldCursorLock = Cursor.lockState; oldCursorVisible = Cursor.visible;
        cameraWasEnabled = worldCamera.enabled; inputWasEnabled = input.enabled;
        try
        {
            foreach (var member in party)
            {
                var movement = member.GetComponent<PlayerMovement>();
                if (movement) { Suspend(movement); movement.ResetMotion(); }
                var follower = member.GetComponent<PartyFollower>(); if (follower) Suspend(follower);
            }
            if (GameManager.Instance.PlayerRotation) Suspend(GameManager.Instance.PlayerRotation);
            foreach (var enemy in FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None)) Suspend(enemy);
            foreach (var actor in actors) actor.Suspend();
            foreach (var actor in reserveActors)
            {
                actor.Suspend();
                var movement = actor.Transform.GetComponent<PlayerMovement>();
                if (movement) { Suspend(movement); movement.ResetMotion(); }
            }
            foreach (var member in party) member.gameObject.SetActive(true);
            player.ResetMotion();
            if (battleMenu) battleMenu.Hide();
            input.SwitchToBattle(); input.enabled = true;
            worldCamera.enabled = false;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            encounterRoot = new GameObject("Runtime same-scene battle");
            var encounter = encounterRoot.AddComponent<InSceneGridBattle>();
            var figures = new List<Transform>(); var offsets = new List<float>();
            foreach (var actor in actors) { figures.Add(actor.Transform); offsets.Add(actor.FootOffset); }
            entry.Duration = battleEntryDuration;
            encounter.Initialize(grid, figures, offsets, entry.Cells, worldCamera, shader, CompleteBattle, preemptive, partySize, entry);
            foreach (var reserve in reserveActors) encounter.AddReserve(reserve.Transform.GetComponent<BattleCharacterInfo>(), reserve.FootOffset);
            if (battleMenu) battleMenu.Bind(encounter);
            Debug.Log(preemptive ? "[Battle] Pre-emptive strike: controlled character acts first this round." : "[Battle] Initiative determines turn order.");
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
        var encounter = encounterRoot ? encounterRoot.GetComponent<InSceneGridBattle>() : null;
        var participants = encounter ? new List<BattleCharacterInfo>(encounter.Participants) : new List<BattleCharacterInfo>();
        bool totalDefeat = encounter && encounter.TotalDefeat;
        if (encounter) encounter.StorePartyHealth();
        if (battleMenu) battleMenu.Unbind();
        if (encounterRoot) { encounterRoot.SetActive(false); Destroy(encounterRoot); encounterRoot = null; }
        // Keep the encounter guard raised until all collider/behaviour state is restored.
        foreach (var actor in actors) actor.Restore();
        foreach (var actor in reserveActors) actor.Restore();
        reserveActors.Clear();
        foreach (var pair in behaviours) if (pair.Key) pair.Key.enabled = pair.Value;
        behaviours.Clear();
        if (GameManager.Instance && GameManager.Instance.PossessedCharacter) GameManager.Instance.PossessedCharacter.ResetMotion();
        if (input) { input.SwitchToWorld(); input.enabled = inputWasEnabled; }
        if (worldCamera) worldCamera.enabled = cameraWasEnabled;
        Cursor.lockState = oldCursorLock; Cursor.visible = oldCursorVisible;
        int experienceReward = 0, gainedLevels = 0;
        if (victory && group && GameManager.Instance)
        {
            experienceReward = group.ClaimVictoryExperience();
            gainedLevels = GameManager.Instance.Progression.AwardExperience(experienceReward, participants);
        }
        if (group) group.CompleteEncounter(victory);
        actors.Clear(); group = null; InBattle = false;
        if (GameManager.Instance) GameManager.Instance.ConfigureFollowers();
        Notify(victory ? $"Victory: {experienceReward} XP for participants; half for unused reserves." + (gainedLevels > 0 ? " Level-ups available: spend points with Tab / Y / Triangle." : " Back in the overworld.") : totalDefeat ? "Party defeated. Checkpoint loading is not implemented; prototype returned to the overworld." : "Back in the overworld. Encounter reset; re-entry cooldown active.");
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
