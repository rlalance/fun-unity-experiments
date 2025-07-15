using UnityEngine;
using System.Collections; // Required for IEnumerator
using System.Collections.Generic;

/// <summary>
/// A manager that iterates through all enemies and finds the nearest enemy for each.
/// This centralizes the query logic for better performance with many enemies,
/// distributing the work over multiple frames using a coroutine.
/// </summary>
public class EnemyProximityDetector : MonoBehaviour
{
    [SerializeField]
    private float searchRadiusPerEnemy = 25f; // The radius each enemy will search within

    [SerializeField, Tooltip("The maximum number of enemy queries to process per frame. Set to 0 or less to process all in one frame (not recommended for many enemies).")]
    private int enemiesPerFrameSlice = 10; // Process 10 enemies per frame slice by default

    // Stores the results: Key = Querying Enemy, Value = Nearest Enemy Found
    private Dictionary<Enemy, Enemy> nearestEnemyPairs = new();

    private Coroutine _proximityCoroutine = null; // Reference to the running coroutine

    private Enemy[] cachedEnemyArray; // Reuse array to avoid allocations
    private int lastKnownEnemyCount = 0;
    private static Color detectionLineColor = Color.blue; // Color for lines drawn in Playmode (main)

    /// <summary>
    /// Called when the GameObject becomes enabled and active.
    /// Starts the proximity detection coroutine.
    /// </summary>
    void OnEnable()
    {
        // Start the coroutine when the object is enabled.
        // This ensures only one instance of the coroutine is running.
        if (_proximityCoroutine == null)
        {
            _proximityCoroutine = StartCoroutine(ProcessAllEnemyProximityQueries());
        }
    }

    /// <summary>
    /// Called when the GameObject becomes disabled or inactive.
    /// Stops the proximity detection coroutine to prevent errors or unnecessary work.
    /// </summary>
    void OnDisable()
    {
        // Stop the coroutine when the object is disabled to prevent errors.
        if (_proximityCoroutine != null)
        {
            StopCoroutine(_proximityCoroutine);
            _proximityCoroutine = null;
        }
    }

    /// <summary>
    /// Update is called once per frame.
    /// This method is primarily used here to continuously draw the debug lines
    /// based on the most recently calculated nearest enemy pairs.
    /// </summary>
    void Update()
    {
        foreach(var pair in nearestEnemyPairs)
        {
            Enemy queryingEnemy = pair.Key;
            Enemy nearestEnemy = pair.Value;

            if (queryingEnemy != null && nearestEnemy != null)
            {
                queryingEnemy.DrawLine(nearestEnemy.transform.position);
            }
        }
    }

    /// <summary>
    /// Coroutine that processes enemy proximity queries over multiple frames.
    /// It iterates through all active enemies, finds their nearest neighbor, and yields
    /// control back to Unity after processing a specified number of enemies per frame.
    /// </summary>
    private IEnumerator ProcessAllEnemyProximityQueries()
    {
        while (true)
        {
            if (SpatialHashingManager.Instance == null)
            {
                yield return new WaitForSeconds(1.0f);
                continue;
            }

            var allEnemies = SpatialHashingManager.Instance.GetAllEnemies();

            // Reuse array if enemy count hasn't changed
            if (cachedEnemyArray == null || allEnemies.Count != lastKnownEnemyCount)
            {
                cachedEnemyArray = new Enemy[allEnemies.Count];
                lastKnownEnemyCount = allEnemies.Count;
            }

            // Copy to array for faster iteration
            for (int i = 0; i < allEnemies.Count; i++)
            {
                cachedEnemyArray[i] = allEnemies[i];
            }

            // Process in chunks without dictionary clearing
            for (int i = 0; i < cachedEnemyArray.Length; i += enemiesPerFrameSlice)
            {
                int endIndex = Mathf.Min(i + enemiesPerFrameSlice, cachedEnemyArray.Length);

                for (int j = i; j < endIndex; j++)
                {
                    var enemy = cachedEnemyArray[j];
                    if (enemy == null) continue;

                    var nearest = SpatialHashingManager.Instance.FindNearestEnemy(
                        enemy.transform.position, searchRadiusPerEnemy, enemy);

                    if (nearest != null)
                        nearestEnemyPairs[enemy] = nearest;
                    else
                    {
                        nearestEnemyPairs.Remove(enemy); // Remove invalid entries
                        enemy.RemoveLine();
                    }
                }

                yield return null;
            }
        }
    }

    /// <summary>
    /// Draws editor Gizmos for debugging in the Scene view.
    /// </summary>
    void OnDrawGizmos()
    {
        // Only draw Gizmos when in Play mode for this specific logic,
        // as the 'nearestEnemyPairs' dictionary is populated at runtime.
        if (!Application.isPlaying) return;

        // Set the Gizmo color to match the detection lines.
        Gizmos.color = detectionLineColor;

        foreach (var pair in nearestEnemyPairs)
        {
            Enemy queryingEnemy = pair.Key;
            // The 'nearestEnemy' (pair.Value) isn't used directly in this Gizmo visualization,
            // only the querying enemy's position and search radius are visualized here.

            // Ensure the querying enemy is still valid before drawing.
            if (queryingEnemy != null)
            {
                // Draw a wire sphere around the querying enemy to visualize its search radius.
                // This helps to see the area within which the nearest enemy is being sought.
                Gizmos.DrawWireSphere(queryingEnemy.transform.position, searchRadiusPerEnemy);
            }
        }
    }
}