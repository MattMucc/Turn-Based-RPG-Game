using UnityEngine;
using Game.GridPrototype;

public class GameManager : MonoBehaviour
{
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