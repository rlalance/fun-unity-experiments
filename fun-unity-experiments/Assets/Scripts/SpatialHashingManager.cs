using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages enemies in a spatial hash grid for efficient proximity queries.
/// This is a MonoBehaviour Singleton.
/// </summary>
public class SpatialHashingManager : MonoBehaviour
{
    public static SpatialHashingManager Instance { get; private set; }

    [Tooltip("Size of each grid cell. Choose a value appropriate for your world and query radius.")]
    public float cellSize = 10f;

    // The main spatial hash map: keys are cell coordinates, values are lists of enemies in that cell.
    private Dictionary<Vector3Int, List<Enemy>> spatialHashMap = new Dictionary<Vector3Int, List<Enemy>>();

    // A list of all registered enemies, useful for initial population or general overview.
    private List<Enemy> allEnemies = new List<Enemy>(); // Keep this for now for internal management

    // NEW: Public read-only property to expose the spatialHashMap
    // This allows reading the dictionary and its lists, but not adding/removing cells from the dictionary itself.
    public IReadOnlyDictionary<Vector3Int, List<Enemy>> SpatialGrid => spatialHashMap;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    /// <summary>
    /// Calculates the 3D cell coordinates for a given world position.
    /// </summary>
    public Vector3Int GetCellCoordinates(Vector3 position)
    {
        // Use Mathf.FloorToInt to ensure negative coordinates are handled correctly
        return new Vector3Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.y / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }

    /// <summary>
    /// Registers an enemy with the spatial hash map. Call this when an enemy spawns.
    /// </summary>
    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null || allEnemies.Contains(enemy)) return;

        allEnemies.Add(enemy);
        AddEnemyToCell(enemy, GetCellCoordinates(enemy.transform.position));
    }

    /// <summary>
    /// Unregisters an enemy from the spatial hash map. Call this when an enemy despawns/dies.
    /// </summary>
    public void UnregisterEnemy(Enemy enemy)
    {
        if (enemy == null || !allEnemies.Contains(enemy)) return;

        allEnemies.Remove(enemy);
        RemoveEnemyFromCell(enemy, GetCellCoordinates(enemy.transform.position));
    }

    /// <summary>
    /// Updates an enemy's position in the spatial hash map. Call this when an enemy moves significantly.
    /// </summary>
    /// <param name="enemy">The enemy that moved.</param>
    /// <param name="oldPosition">The enemy's position *before* it moved.</param>
    public void UpdateEnemyPosition(Enemy enemy, Vector3 oldPosition)
    {
        if (enemy == null) return;

        Vector3Int oldCell = GetCellCoordinates(oldPosition);
        Vector3Int newCell = GetCellCoordinates(enemy.transform.position);

        if (oldCell != newCell)
        {
            RemoveEnemyFromCell(enemy, oldCell);
            AddEnemyToCell(enemy, newCell);
        }
    }

    private void AddEnemyToCell(Enemy enemy, Vector3Int cellCoords)
    {
        if (!spatialHashMap.ContainsKey(cellCoords))
        {
            spatialHashMap[cellCoords] = new List<Enemy>();
        }
        spatialHashMap[cellCoords].Add(enemy);
    }

    private void RemoveEnemyFromCell(Enemy enemy, Vector3Int cellCoords)
    {
        if (spatialHashMap.TryGetValue(cellCoords, out List<Enemy> enemiesInCell))
        {
            enemiesInCell.Remove(enemy);

            if (enemiesInCell.Count == 0)
            {
                spatialHashMap.Remove(cellCoords);
            }
        }
    }

    /// <summary>
    /// Finds the nearest enemy to a given query position within a specified search radius.
    /// </summary>
    /// <param name="queryPosition">The world position from which to search.</param>
    /// <param name="searchRadius">The maximum distance to search for enemies.</param>
    /// <param name="excludeEnemy">An optional enemy to exclude from the search results (e.g., the querying enemy itself).</param>
    /// <returns>The nearest Enemy object found, or null if none within radius (excluding the excluded enemy).</returns>
    public Enemy FindNearestEnemy(Vector3 queryPosition, float searchRadius, Enemy excludeEnemy = null)
    {
        if (searchRadius <= 0 || allEnemies.Count == 0) return null; // Early exit for invalid radius or no enemies

        Enemy nearestEnemy = null;
        float minDistanceSqr = searchRadius * searchRadius; // Use squared distance for performance

        Vector3Int queryCell = GetCellCoordinates(queryPosition);

        // --- FIX STARTS HERE ---
        // Calculate the range of cells to check based on searchRadius and cellSize
        // We use Mathf.CeilToInt to ensure we check enough cells even if searchRadius is slightly over a multiple of cellSize.
        int cellSearchRange = Mathf.CeilToInt(searchRadius / cellSize);

        // Iterate through cells in a cube extending outwards by cellSearchRange
        for (int xOffset = -cellSearchRange; xOffset <= cellSearchRange; xOffset++)
        {
            for (int yOffset = -cellSearchRange; yOffset <= cellSearchRange; yOffset++)
            {
                for (int zOffset = -cellSearchRange; zOffset <= cellSearchRange; zOffset++)
                {
                    Vector3Int currentCell = new Vector3Int(
                        queryCell.x + xOffset,
                        queryCell.y + yOffset,
                        queryCell.z + zOffset
                    );

                    // If the cell exists in our map and contains enemies
                    if (spatialHashMap.TryGetValue(currentCell, out List<Enemy> enemiesInCell))
                    {
                        // Iterate through enemies in this specific cell
                        // Use a reverse loop or ToList().Reverse() if you anticipate removing enemies mid-loop,
                        // though for FindNearest, it's usually fine as we just read.
                        foreach (Enemy enemy in enemiesInCell)
                        {
                            // Important: Check if enemy is still valid (not destroyed but still in list)
                            // and if it's the enemy to exclude.
                            if (enemy == null || enemy == excludeEnemy) continue;

                            float distSqr = (enemy.transform.position - queryPosition).sqrMagnitude;

                            if (distSqr < minDistanceSqr)
                            {
                                minDistanceSqr = distSqr;
                                nearestEnemy = enemy;
                            }
                        }
                    }
                }
            }
        }
        return nearestEnemy;
    }

    // Public getter for the count of all enemies registered with the manager
    public int GetRegisteredEnemyCount()
    {
        return allEnemies.Count;
    }

    // Public getter for the number of currently active cells in the hash map
    public int GetActiveCellCount()
    {
        return spatialHashMap.Count;
    }
    
    // Get a read only list of all enemies currently registered
    public IReadOnlyList<Enemy> GetAllEnemies()
    {
        return allEnemies.AsReadOnly();
    }
    

    // Optional: For debugging, visualize cells in Editor (Gizmos)
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || Instance == null || spatialHashMap == null) return;

        Gizmos.color = new Color(0, 1, 1, 0.2f); // Cyan, semi-transparent

        foreach (var kvp in spatialHashMap)
        {
            Vector3Int cellCoords = kvp.Key;
            Vector3 center = new Vector3(
                cellCoords.x * cellSize + cellSize / 2f,
                cellCoords.y * cellSize + cellSize / 2f,
                cellCoords.z * cellSize + cellSize / 2f
            );
            Gizmos.DrawWireCube(center, Vector3.one * cellSize);
        }
    }
}