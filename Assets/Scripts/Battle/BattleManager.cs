using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }
    
    public bool InBattle { get; private set; }

    [Header("References")]
    [SerializeField] private BattleMenuUI battleMenu;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
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
    }
}