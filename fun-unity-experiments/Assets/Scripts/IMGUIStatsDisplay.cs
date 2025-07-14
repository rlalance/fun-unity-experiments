using UnityEngine;
using System.Linq; // For FindObjectsOfType, if used

/// <summary>
/// Displays real-time performance and game statistics directly on screen using IMGUI.
/// </summary>
public class IMGUIStatsDisplay : MonoBehaviour
{
    [Tooltip("How often (in seconds) to update the stats display.")]
    public float updateInterval = 0.2f;

    [Header("GUI Styling")]
    public Color textColor = Color.white;
    public int fontSize = 20; // Pixels
    public TextAnchor textAlignment = TextAnchor.UpperLeft; // Position within the Rect

    private float _timeSinceLastUpdate = 0f;
    private int _frameCount = 0;
    private float _fps = 0f;
    private float _ms = 0f;

    // Game stats
    private int _totalEnemies = 0;
    private int _registeredEnemiesSH = 0;
    private int _activeSHCells = 0;

    private GUIStyle _textStyle; // To store our custom GUI style

    void Awake()
    {
        // Initialize the GUIStyle once
        _textStyle = new GUIStyle();
        _textStyle.normal.textColor = textColor;
        _textStyle.fontSize = fontSize;
        _textStyle.alignment = textAlignment;

        // Optionally add an outline/shadow for better readability
        _textStyle.normal.background = null; // No background image
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

            // Gather game-specific stats (same logic as before)
            _totalEnemies = FindObjectsOfType<Enemy>().Length; // Still potentially slow for huge numbers

            if (SpatialHashingManager.Instance != null)
            {
                _registeredEnemiesSH = SpatialHashingManager.Instance.GetRegisteredEnemyCount();
                _activeSHCells = SpatialHashingManager.Instance.GetActiveCellCount();
            }

            // Reset counters for next interval
            _frameCount = 0;
            _timeSinceLastUpdate = 0f;
        }

        // Update text style color in case it changes in inspector during runtime
        if (_textStyle.normal.textColor != textColor || _textStyle.fontSize != fontSize || _textStyle.alignment != textAlignment)
        {
            _textStyle.normal.textColor = textColor;
            _textStyle.fontSize = fontSize;
            _textStyle.alignment = textAlignment;
        }
    }

    // OnGUI is called multiple times per frame for GUI events
    void OnGUI()
    {
        // Define the rectangle where the text will be drawn (top-left corner, width, height)
        // Adjust these values to position your text on screen.
        // Screen.width and Screen.height give you the current screen resolution.
        float panelWidth = 300; // Pixels
        float panelHeight = 150; // Pixels
        float padding = 10; // Pixels from the edge

        Rect textRect = new Rect(padding, padding, panelWidth, panelHeight);

        // Format the stats string
        string statsString =
            $"FPS: {_fps:F1}\n" +
            $"MS: {_ms:F1}\n" +
            $"-------------------\n" +
            $"Total Enemies: {_totalEnemies}\n" +
            $"Registered Enemies (SH): {_registeredEnemiesSH}\n" +
            $"Active SH Cells: {_activeSHCells}";

        // Draw the text using the custom style
        GUI.Label(textRect, statsString, _textStyle);
    }
}