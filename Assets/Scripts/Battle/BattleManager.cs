using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }
    
    public bool InBattle { get; private set; }

    [Header("References")]
    [SerializeField] private BattleMenuUI battleMenu;
    private PlayerInputReader input;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);

        if (!input) input = GameManager.Instance.Input;
    }

    private void Update()
    {
        if (!InBattle) return;

        if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.5f)
            SetCursorVisible(true);

        if (input.NavUpPressed || input.NavDownPressed || input.NavLeftPressed || input.NavRightPressed)
            SetCursorVisible(false);
    }

    private void SetUpCursor()
    {
        UpdateCursor(input.CurrentScheme);
        input.OnControlSchemeChanged += UpdateCursor;
    }

    private void UpdateCursor(PlayerInputReader.ControlScheme scheme)
    {
        SetCursorVisible(scheme == PlayerInputReader.ControlScheme.Keyboard);
    }

    private void SetCursorVisible(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.Confined : CursorLockMode.Locked;
    }

    public void StartBattle(List<GameObject> enemies, Transform[] enemyBattlePositions, Transform playerBattlePosition, Transform cameraBattlePosition, Transform cameraTarget)
    {
        if (InBattle) return;

        InBattle = true;

        // Stop player movement
        PlayerMovement player = GameManager.Instance.PossessedCharacter; // We know that the PlayerMovement script is attached to the player's root GameObject
        player.enabled = false;

        // Reposition
        player.transform.position = playerBattlePosition.position;
        player.transform.rotation = playerBattlePosition.rotation;

        for (int i = 0; i < enemies.Count; i++)
        {
            if (i >= enemyBattlePositions.Length)
            {
                Debug.LogError($"[BattleManager] No battle positions assigned for enemy {i}. Skipping...");
                continue;
            }

            enemies[i].transform.SetPositionAndRotation(enemyBattlePositions[i].position, enemyBattlePositions[i].rotation);
        }

        // Stop camera rotation
        GameManager.Instance.PlayerRotation.enabled = false;
        Camera.main.transform.SetPositionAndRotation(cameraBattlePosition.position, cameraBattlePosition.rotation);
        Camera.main.transform.LookAt(cameraTarget);

        GameManager.Instance.Input.SwitchToBattle();
        battleMenu.Show();
        SetUpCursor();
    }

    public void EndBattle()
    {
        input.OnControlSchemeChanged -= UpdateCursor;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}