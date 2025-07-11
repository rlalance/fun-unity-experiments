using UnityEngine;

/// <summary>
/// Spawns enemies from a pool and manages their initial setup.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; } // Singleton pattern for easy access

    public GameObject enemyPrefab;
    public int numberOfEnemies = 100;
    public float spawnRadius = 50f;
    private const string ENEMY_TAG = "Enemy"; // Tag for enemies in the ObjectPooler

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Enemy Prefab is not assigned in EnemySpawner!", this);
            return;
        }

        SpawnInitialEnemies();
    }

    private void SpawnInitialEnemies()
    {
        for (int i = 0; i < numberOfEnemies; i++)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                0f,
                Random.Range(-spawnRadius, spawnRadius)
            );
            SpawnEnemyAt(randomPos);
        }
        Debug.Log($"Spawned {numberOfEnemies} initial enemies.");
    }

    /// <summary>
    /// Spawns an enemy at a specific world position using the ObjectPooler.
    /// </summary>
    /// <param name="position">The world position to spawn the enemy.</param>
    /// <returns>The spawned Enemy GameObject, or null if pool retrieval fails.</returns>
    public GameObject SpawnEnemyAt(Vector3 position)
    {
        if (ObjectPooler.Instance == null)
        {
            Debug.LogError("ObjectPooler.Instance is null. Ensure ObjectPooler is in the scene and initialized.");
            return null;
        }

        // Get an enemy from the pool
        GameObject enemyGO = ObjectPooler.Instance.GetFromPool(ENEMY_TAG, position, Quaternion.identity);

        if (enemyGO != null)
        {
            enemyGO.SetActive(true); // Ensure it's active
            // The Enemy.cs script on the prefab will handle registration with SpatialHashingManager
            Debug.Log($"Enemy spawned at {position} via bullet collision.");
        }
        else
        {
            Debug.LogWarning($"Failed to retrieve enemy from pool with tag: {ENEMY_TAG}. Pool might be exhausted or not set up correctly.");
        }
        return enemyGO;
    }
}