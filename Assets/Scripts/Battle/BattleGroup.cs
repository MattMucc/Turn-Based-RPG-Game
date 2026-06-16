using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct EnemySpawnEntry
{
    public GameObject enemyPrefab;
    public int spawnAmount;
}

public class BattleGroup : MonoBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField] private EnemySpawnEntry[] enemySpawnEntries;
    [SerializeField] private LayerMask groundLayer = ~0;

    [Header("Battle Positions")]
    [SerializeField] private Transform[] enemyBattlePositions;
    [SerializeField] private Transform playerBattlePosition;
    [SerializeField] private Transform cameraBattlePosition;
    [SerializeField] private Transform cameraTarget;

    private BoxCollider spawnArea;
    private List<GameObject> spawnedEnemies = new();
    private bool battleTriggered = false;

    private void Awake()
    {
        spawnArea = GetComponent<BoxCollider>();
    }

    private void Start()
    {
        SpawnEnemies();
    }

    private void SpawnEnemies()
    {
        foreach (EnemySpawnEntry entry in enemySpawnEntries)
        {
            if (!entry.enemyPrefab)
            {
                Debug.LogWarning($"[BattleGroup] An enemy entry has no prefab assigned. Skipping...");
                continue;
            }

            for (int i = 0; i < entry.spawnAmount; i++)
            {
                Vector3 spawnPos = GetRandomPosition();
                GameObject enemy = Instantiate(entry.enemyPrefab, spawnPos, Quaternion.identity);

                // Initialize the enemy
                EnemyTrigger trigger = enemy.GetComponent<EnemyTrigger>();
                if (trigger != null)
                    trigger.Initialize(this);
                else
                    Debug.LogWarning($"[BattleGroup] {entry.enemyPrefab.name} has no EnemyTrigger attached!");

                spawnedEnemies.Add(enemy);
            }
        }

        Debug.Log($"[BattleGroup] Spawned {spawnedEnemies.Count} enemies!");
    }

    private Vector3 GetRandomPosition()
    {
        Bounds bounds = spawnArea.bounds;
        for (int attempt = 0; attempt < 10; attempt++)
        {
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float z = Random.Range(bounds.min.z, bounds.max.z);

            // Cast a ray downward from above the bounds to find the ground surface
            Vector3 origin = new Vector3(x, bounds.max.y + 1f, z);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, bounds.size.y + 2f, groundLayer, QueryTriggerInteraction.Ignore))
                return hit.point + new Vector3(0f, 1f, 0f);
        }

        Debug.LogWarning($"[BattleGroup] Could not find a valid surface position. Using bounds center...");
        return bounds.center;
    }

    public void TriggerBattle()
    {
        if (battleTriggered) return;

        battleTriggered = true;
        BattleManager.Instance.StartBattle(spawnedEnemies, enemyBattlePositions, playerBattlePosition, cameraBattlePosition, cameraTarget);
    }
}