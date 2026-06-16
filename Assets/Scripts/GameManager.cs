using UnityEngine;

public class GameManager : MonoBehaviour
{
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
        else
            Destroy(gameObject);

        if (!Input) Input = GetComponent<PlayerInputReader>();
        if (!PlayerRotation) PlayerRotation = Camera.main.transform.root.GetComponent<PlayerRotation>();
        if (!PossessedCharacter) PossessCharacter(GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>());
    }

    public void PossessCharacter(PlayerMovement character)
    {
        if (PossessedCharacter != null)
            PossessedCharacter.Unpossess();

        PossessedCharacter = character;
        PossessedCharacter.Possess();
        PlayerRotation.SetFollowTarget(PossessedCharacter.gameObject.transform);
    }
}