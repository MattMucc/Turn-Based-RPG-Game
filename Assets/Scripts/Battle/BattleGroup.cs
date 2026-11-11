using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct EnemySpawnEntry
{
    [HideInInspector] public string SaveKey;
    public GameObject enemyPrefab;
    public int spawnAmount;
}

public partial class BattleGroup : MonoBehaviour
{
    [Header("Prototype victory reward")]
    [SerializeField, Min(0)] private int victoryExperience = 50;
    private bool experienceClaimed;
    public int ClaimVictoryExperience()
    {
        if (experienceClaimed) return 0;
        experienceClaimed = true; return Mathf.Max(0, victoryExperience);
    }
    [Header("Enemy Settings")]
    [SerializeField] private EnemySpawnEntry[] enemySpawnEntries;
    [SerializeField] private LayerMask groundLayer = ~0;

    [Header("Battle Positions")]
    [SerializeField] private Transform[] enemyBattlePositions;
    [SerializeField] private Transform playerBattlePosition;
    [SerializeField] private Transform cameraBattlePosition;
    [SerializeField] private Transform cameraTarget;
    [Header("Same-scene Grid")]
    [Tooltip("Optional: center the grid on a suitable authored patch of terrain instead of the player.")]
    [SerializeField] private Transform gridCenter;
    public Transform GridCenter => gridCenter;
    [Tooltip("Optional companion formation preferences: first companion, then second. Invalid or unreachable cells fall back to nearby safe ground.")]
    [SerializeField] private Transform[] companionFormationMarkers;
    public Transform[] CompanionFormationMarkers => companionFormationMarkers;
    public int GroundLayers => groundLayer.value;

    [Header("References")]
    [SerializeField] private BoxCollider spawnArea;

    [Header("Debug")]
    [SerializeField] private bool displayBoundsDebug = false;
    [SerializeField] private bool displayBattlePositionsDebug = false;

    private List<GameObject> spawnedEnemies = new();
    private List<EnemyMovement> enemyMovements = new();
    private bool battleTriggered = false;
    private float retryAfter;

    private void Awake()
    {
        if (!spawnArea) spawnArea = GetComponent<BoxCollider>();
    }

    public bool IsPlayerInBounds(Vector3 position)
    {
        return spawnArea.bounds.Contains(position);
    }

    public void AlertGroup()
    {
        foreach (EnemyMovement movement in enemyMovements)
            movement.StartChasing();
    }

    public void TriggerBattle(bool preemptive = false)
    {
        if (battleTriggered || Time.time < retryAfter || !BattleManager.Instance || BattleManager.Instance.InBattle) return;
        battleTriggered = BattleManager.Instance.TryStartBattle(this, spawnedEnemies, preemptive);
        if (!battleTriggered) retryAfter = Time.time + 3;
    }

    public void CompleteEncounter(bool victory)
    {
        retryAfter = Time.time + 5;
        battleTriggered = victory;
        if (victory)
        {
            experienceClaimed=true;
            foreach (var enemy in spawnedEnemies) if (enemy) Destroy(enemy);
            spawnedEnemies.Clear(); enemyMovements.Clear(); spawnedKeys.Clear();
        }
        else foreach (var movement in enemyMovements) if (movement) movement.StartRoaming();
    }

    private void OnTriggerExit(Collider other)
    {
        if (battleTriggered) return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (!player || !player.IsPossessed)
            return;

        foreach (EnemyMovement movement in enemyMovements)
            movement.StartRoaming();
    }

    private void OnDrawGizmos()
    {
        if (displayBattlePositionsDebug)
        {
            // Draw enemy battle positions
            for (int i = 0; i < enemyBattlePositions.Length; i++)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(enemyBattlePositions[i].position, 0.5f);
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(enemyBattlePositions[i].position, enemyBattlePositions[i].position + enemyBattlePositions[i].forward);
            }

            // Draw player battle position
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(playerBattlePosition.position, 0.5f);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(playerBattlePosition.position, playerBattlePosition.position + playerBattlePosition.forward);

            // Draw camera battle position
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(cameraBattlePosition.position, 0.5f);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(cameraBattlePosition.position, cameraBattlePosition.position + cameraBattlePosition.forward);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(cameraBattlePosition.position, cameraTarget.position);

            // Draw camera target position
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(cameraTarget.position, 0.5f);
        }

        if (displayBoundsDebug && spawnArea)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, spawnArea.size);
        }
    }
}
