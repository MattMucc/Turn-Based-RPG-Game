using UnityEngine;
using System.Collections.Generic;
using Game.GridPrototype;

[DefaultExecutionOrder(-60)]
public partial class GameManager : MonoBehaviour
{
    [Tooltip("Default active team (up to three). First entry is its captain. Temporary battle swaps do not modify this list.")]
    public List<BattleCharacterInfo> BattleParty = new List<BattleCharacterInfo>();
    [Tooltip("The other three cast members, in reserve captain order.")]
    public List<BattleCharacterInfo> ReserveParty = new List<BattleCharacterInfo>();
    public List<BattleCharacterInfo> GetBattleParty()
    {
        var result = new List<BattleCharacterInfo>();
        foreach (var member in BattleParty)
            if (member && !result.Contains(member) && result.Count < 3) result.Add(member);
        if (result.Count == 0 && PossessedCharacter) result.Add(PossessedCharacter.GetComponent<BattleCharacterInfo>());
        return result;
    }
    public PartyInventory Inventory = new PartyInventory();
    private void OnValidate() { Inventory?.NotifyChanged(); }
    public PartyProgression Progression = new PartyProgression();
    public ProgressionMenu ProgressionMenu { get; private set; }
    public WorldManipulationController WorldTools { get; private set; }
    SaveGameService saves;
    public SaveGameService Saves => saves?saves:(saves=GetComponent<SaveGameService>()??gameObject.AddComponent<SaveGameService>());
    public bool WorldInputBlocked => (saves&&saves.Busy) || (WorldTools && WorldTools.BlocksWorldInput) || IsSwitchingCharacter || switchCompletedFrame == Time.frameCount || (ProgressionMenu && ProgressionMenu.BlocksWorldInput);
    public static GameManager Instance { get; private set; }
    public PlayerInputReader Input { get; private set; }
    public PlayerMovement PossessedCharacter { get; private set; }
    public PlayerRotation PlayerRotation { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else { Destroy(gameObject); return; }

        WorldTools = GetComponent<WorldManipulationController>();
        ProgressionMenu = GetComponent<ProgressionMenu>();
        if (!ProgressionMenu) ProgressionMenu = gameObject.AddComponent<ProgressionMenu>();
        foreach (var member in BattleParty) Progression.Register(member);
        foreach (var member in ReserveParty) Progression.Register(member);
        foreach (var member in Progression.Members.ToArray()) Progression.Register(member);
        if (!Input) Input = GetComponent<PlayerInputReader>();
        if (!PlayerRotation) PlayerRotation = Camera.main.transform.root.GetComponent<PlayerRotation>();
        if (!PossessedCharacter) PossessCharacter(GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>());
    }

    public void PossessCharacter(PlayerMovement character)
    {
        if (PossessedCharacter != null)
            PossessedCharacter.Unpossess();

        PossessedCharacter = character;
        character.gameObject.SetActive(true); character.enabled = true;
        var controlledFollower = character.GetComponent<PartyFollower>(); if (controlledFollower) controlledFollower.enabled = false;
        character.ResetMotion();
        var info = character.GetComponent<BattleCharacterInfo>();
        if (!info) info = character.gameObject.AddComponent<BattleCharacterInfo>();
        Progression.Register(info);
        PossessedCharacter.Possess();
        if (PlayerRotation) PlayerRotation.SetFollowTarget(PossessedCharacter.gameObject.transform);
        ConfigureFollowers();
    }

    public void ConfigureFollowers()
    {
        var party = GetBattleParty(); Transform preceding = PossessedCharacter ? PossessedCharacter.transform : null;
        int slot = 0;
        foreach (var member in party) foreach (var deviceOwner in member.GetComponentsInChildren<UnityEngine.InputSystem.PlayerInput>(true)) deviceOwner.enabled = false;
        foreach (var member in party)
        {
            if (!member || member.transform == preceding) continue;
            if (PossessedCharacter && member.transform == PossessedCharacter.transform) continue;
            var movement = member.GetComponent<PlayerMovement>();
            if (movement) { movement.Unpossess(); movement.enabled = false; }
            var follower = member.GetComponent<PartyFollower>();
            if (!follower) follower = member.gameObject.AddComponent<PartyFollower>();
            member.gameObject.SetActive(true); follower.enabled = true; follower.SetCompanionSlot(slot++); follower.Follow(preceding);
        }
        foreach (var member in ReserveParty) if (member && !party.Contains(member)) member.gameObject.SetActive(false);
        // Party bodies must not trap one another at a narrow turn in the trail.
        for (int i = 0; i < party.Count; i++) for (int j = i + 1; j < party.Count; j++)
            foreach (var a in party[i].GetComponentsInChildren<Collider>(true)) foreach (var b in party[j].GetComponentsInChildren<Collider>(true)) Physics.IgnoreCollision(a, b);
    }

    public List<BattleCharacterInfo> GetEncounterParty()
    {
        var party = GetBattleParty(); var controlled = PossessedCharacter ? PossessedCharacter.GetComponent<BattleCharacterInfo>() : null;
        if (controlled && party.Remove(controlled)) party.Insert(0, controlled);
        return party;
    }
}
