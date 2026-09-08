using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyAI enemyPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private Transform patrolLeft;
    [SerializeField] private Transform patrolRight;
    [SerializeField] private BoxCollider2D spawnZone;
    [SerializeField] private LayerMask groundLayer;

    [Header("Spawn Settings")]
    [Min(1)]
    [SerializeField] private int targetEnemyCount = 3;
    [Min(0f)]
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private float spawnHeight = 6f;
    [SerializeField] private float spawnEdgePadding = 3f;
    [SerializeField] private float minimumEnemySpacing = 8f;
    [SerializeField] private int spawnPositionAttempts = 20;

    private readonly List<EnemyAI> aliveEnemies = new();
    private float nextRespawnTime;

    private void Start()
    {
        for (int i = 0; i < targetEnemyCount; i++)
            SpawnEnemy();

        nextRespawnTime = Time.time + respawnDelay;
    }

    private void Update()
    {
        aliveEnemies.RemoveAll(enemy => enemy == null);

        if (aliveEnemies.Count < targetEnemyCount &&
            Time.time >= nextRespawnTime)
        {
            SpawnEnemy();
            nextRespawnTime = Time.time + respawnDelay;
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null ||
            player == null ||
            patrolLeft == null ||
            patrolRight == null ||
            spawnZone == null)
        {
            return;
        }

        Bounds bounds = spawnZone.bounds;

        float minX = bounds.min.x + spawnEdgePadding;
        float maxX = bounds.max.x - spawnEdgePadding;

        if (minX > maxX)
        {
            minX = bounds.center.x;
            maxX = bounds.center.x;
        }

        if (!TryGetSpawnX(minX, maxX, out float spawnX))
            return;

        Vector3 spawnPosition = new Vector3(
            spawnX,
            bounds.max.y + spawnHeight,
            0f
        );

        EnemyAI enemy = Instantiate(
            enemyPrefab,
            spawnPosition,
            Quaternion.identity
        );

        enemy.Configure(
            player,
            patrolLeft,
            patrolRight,
            groundLayer
        );

        aliveEnemies.Add(enemy);
    }

    private bool TryGetSpawnX(
    float minX,
    float maxX,
    out float spawnX
)
    {
        for (int attempt = 0;
             attempt < spawnPositionAttempts;
             attempt++)
        {
            float candidateX = Random.Range(minX, maxX);
            bool hasEnoughSpace = true;

            foreach (EnemyAI enemy in aliveEnemies)
            {
                if (enemy != null &&
                    Mathf.Abs(enemy.transform.position.x - candidateX) <
                    minimumEnemySpacing)
                {
                    hasEnoughSpace = false;
                    break;
                }
            }

            if (hasEnoughSpace)
            {
                spawnX = candidateX;
                return true;
            }
        }

        spawnX = 0f;
        return false;
    }
}