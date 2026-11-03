using UnityEngine;
using System.Collections.Generic;
using Game.GridPrototype;

public class GameManager : MonoBehaviour
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
    public PartyProgression Progression = new PartyProgression();
    public ProgressionMenu ProgressionMenu { get; private set; }
    public bool WorldInputBlocked => ProgressionMenu && ProgressionMenu.BlocksWorldInput;
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
        var info = character.GetComponent<BattleCharacterInfo>();
        if (!info) info = character.gameObject.AddComponent<BattleCharacterInfo>();
        Progression.Register(info);
        PossessedCharacter.Possess();
        PlayerRotation.SetFollowTarget(PossessedCharacter.gameObject.transform);
    }
}
