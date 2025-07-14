using UnityEngine;
using TMPro; // Required for TextMeshProUGUI
using System.Linq; // For FindObjectsOfType, if used

/// <summary>
/// Displays real-time performance and game statistics on a TextMeshProUGUI component.
/// Attach this to a GameObject that has a TextMeshProUGUI component as a child
/// or assign the TextMeshProUGUI component in the Inspector.
/// </summary>
public class PerformanceStatsDisplay : MonoBehaviour
{
    [Tooltip("Assign the TextMeshProUGUI component from your World Space Canvas here.")]
    public TextMeshProUGUI statsText;

    [Tooltip("How often (in seconds) to update the stats display.")]
    public float updateInterval = 0.2f;

    private float _timeSinceLastUpdate = 0f;
    private int _frameCount = 0;
    private float _fps = 0f;
    private float _ms = 0f;

    void Awake()
    {
        if (statsText == null)
        {
            statsText = GetComponent<TextMeshProUGUI>();
            if (statsText == null)
            {
                statsText = GetComponentInChildren<TextMeshProUGUI>();
            }
            if (statsText == null)
            {
                Debug.LogError("PerformanceStatsDisplay: No TextMeshProUGUI component assigned or found on this GameObject/children.", this);
                enabled = false; // Disable script if no text component
                return;
            }
        }
    }

    void Update()
    {
        _frameCount++;
        _timeSinceLastUpdate += Time.unscaledDeltaTime; // Use unscaledDeltaTime to ignore Time.timeScale

        if (_timeSinceLastUpdate >= updateInterval)
        {
            // Calculate FPS and MS
            _fps = _frameCount / _timeSinceLastUpdate;
            _ms = (1f / _fps) * 1000f;

            // Gather game-specific stats
            int totalEnemies = 0;
            int spatialHashCells = 0;
            int registeredEnemies = 0;

            // Note: FindObjectsOfType<Enemy>() can be slow for very large numbers of enemies.
            // If EnemySpawner had a public list of active enemies, use that for better performance.
            totalEnemies = FindObjectsOfType<Enemy>().Length;


            if (SpatialHashingManager.Instance != null)
            {
                registeredEnemies = SpatialHashingManager.Instance.GetRegisteredEnemyCount();
                spatialHashCells = SpatialHashingManager.Instance.GetActiveCellCount();
            }

            // --- Apply TextMeshPro Rich Text Styling ---
            statsText.text =
                "<align=\"center\"><b><color=#005A9C>PERFORMANCE STATS</color></b></align>\n" +
                "<line-height=1.1>\n" + // Add some line spacing for readability
                "<align=\"left\">\n" +
                "<b><color=#2E2E2E>FPS:</color></b> <color=#005A9C>" + _fps.ToString("F1") + "</color>\n" +
                "<b><color=#2E2E2E>MS:</color></b> <color=#005A9C>" + _ms.ToString("F1") + "</color>\n" +
                "\n" + // Blank line for separation
                "<b><color=#005A9C>GAME STATS</color></b>\n" +
                "<b><color=#2E2E2E>Total Enemies:</color></b> <color=#005A9C>" + totalEnemies + "</color>\n" +
                "<b><color=#2E2E2E>Registered Enemies (SH):</color></b> <color=#005A9C>" + registeredEnemies + "</color>\n" +
                "<b><color=#2E2E2E>Active SH Cells:</color></b> <color=#005A9C>" + spatialHashCells + "</color>\n" +
                "</align>"; // Close align tag

            // Reset counters for next interval
            _frameCount = 0;
            _timeSinceLastUpdate = 0f;
        }
    }
}