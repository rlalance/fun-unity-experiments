using System;
using UnityEngine;
using System.Collections; // Required for IEnumerator
using System.Collections.Generic;
using System.Linq; // For FindObjectsOfType, though consider alternative for large N

/// <summary>
/// A manager that iterates through all enemies and finds the nearest enemy for each.
/// This centralizes the query logic for better performance with many enemies,
/// distributing the work over multiple frames using a coroutine.
/// </summary>
public class EnemyProximityDetector : MonoBehaviour
{
    [SerializeField]
    private float searchRadiusPerEnemy = 25f; // The radius each enemy will search within

    [SerializeField]
    private Color detectionLineColor = Color.blue; // Color for lines drawn in Playmode (main)

    [SerializeField, Tooltip("The maximum number of enemy queries to process per frame. Set to 0 or less to process all in one frame (not recommended for many enemies).")]
    private int enemiesPerFrameSlice = 10; // Process 10 enemies per frame slice by default

    // Stores the results: Key = Querying Enemy, Value = Nearest Enemy Found
    private Dictionary<Enemy, Enemy> nearestEnemyPairs = new();

    private Coroutine _proximityCoroutine = null; // Reference to the running coroutine

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
        // Debug.DrawLine is called every frame, displaying the latest available detection results.
        // This makes the lines appear smooth and continuous, even though the underlying
        // detection logic is spread over multiple frames.
        DrawDetectionLines();
    }

    /// <summary>
    /// Coroutine that processes enemy proximity queries over multiple frames.
    /// It iterates through all active enemies, finds their nearest neighbor, and yields
    /// control back to Unity after processing a specified number of enemies per frame.
    /// </summary>
    private IEnumerator ProcessAllEnemyProximityQueries()
    {
        while (true) // Loop indefinitely to continuously update proximity detection
        {
            // Safeguard: Wait if SpatialHashingManager is not yet available.
            if (SpatialHashingManager.Instance == null)
            {
                Debug.LogWarning("EnemyProximityDetector: SpatialHashingManager not found. Waiting...");
                yield return new WaitForSeconds(1.0f); // Wait a bit before retrying
                continue;
            }

            // Clear results from the previous full cycle before starting a new one.
            nearestEnemyPairs.Clear();

            // IMPORTANT TODO: Optimize this! FindObjectsOfType<Enemy>() is inefficient
            // for very large numbers of enemies as it searches the entire scene.
            // For production, you should get this list from a centralized manager
            // (e.g., SpatialHashingManager.Instance.allEnemies if exposed, or an EnemyManager)
            // that maintains an up-to-date collection of active enemies.
            Enemy[] allActiveEnemies = FindObjectsOfType<Enemy>();

            int enemiesProcessedThisSlice = 0;
            for (int i = 0; i < allActiveEnemies.Length; i++)
            {
                Enemy queryingEnemy = allActiveEnemies[i];

                // Skip if the enemy was destroyed while the coroutine was paused.
                if (queryingEnemy == null)
                {
                    continue;
                }

                // Call the optimized FindNearestEnemy from SpatialHashingManager.
                // It excludes the querying enemy itself from the search.
                Enemy foundNearestEnemy = SpatialHashingManager.Instance.FindNearestEnemy(
                    queryingEnemy.transform.position,
                    searchRadiusPerEnemy,
                    queryingEnemy // Pass the enemy to exclude itself
                );

                // This check (foundNearestEnemy == queryingEnemy) is technically redundant
                // because FindNearestEnemy now handles the exclusion.
                // Keeping it as per your original code's explicit check.
                if (foundNearestEnemy == queryingEnemy)
                {
                    foundNearestEnemy = null;
                }

                // If a valid nearest enemy was found, store the pair.
                if (foundNearestEnemy != null)
                {
                    nearestEnemyPairs[queryingEnemy] = foundNearestEnemy;
                }

                enemiesProcessedThisSlice++;

                // Yield control back to Unity if we've processed enough enemies for this frame slice.
                // Or if enemiesPerFrameSlice is 0 or less, process all in one go (not recommended).
                if (enemiesPerFrameSlice > 0 && enemiesProcessedThisSlice >= enemiesPerFrameSlice)
                {
                    yield return null; // Pause execution here and resume on the next frame.
                    enemiesProcessedThisSlice = 0; // Reset counter for the next slice of work.
                }
            }

            // After processing all enemies in the current full cycle, yield one last time.
            // This ensures there's at least one frame break between complete cycles
            // and prevents the coroutine from hogging the CPU continuously.
            yield return null;
        }
    }

    /// <summary>
    /// Draws debug lines between enemies and their detected nearest neighbors.
    /// This is called every frame to ensure continuous visualization.
    /// </summary>
    private void DrawDetectionLines()
    {
        foreach (var pair in nearestEnemyPairs)
        {
            Enemy queryingEnemy = pair.Key;
            Enemy nearestEnemy = pair.Value;

            // Only draw if both enemies are still valid (not destroyed).
            if (queryingEnemy != null && nearestEnemy != null)
            {
                queryingEnemy.DrawLine(nearestEnemy.transform.position, detectionLineColor);
                
                Debug.DrawLine(queryingEnemy.transform.position, nearestEnemy.transform.position, detectionLineColor);
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