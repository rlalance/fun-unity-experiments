using UnityEngine;
using System.Collections.Generic; // Still needed if you used it before, though less directly for movement

/// <summary>
/// Represents an enemy in the game world, automatically registering with the SpatialHashingManager.
/// Moves in a random direction, changing direction every few seconds.
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Spatial Hashing Integration")]
    private Vector3 lastKnownPosition;
    [Tooltip("How far an enemy must move (squared distance) before its cell is re-evaluated in the spatial hash map.")]
    public float movementThresholdSqr = 0.01f; // (e.g., 0.1 units squared)

    [Header("Movement")]
    [Tooltip("Speed at which the enemy moves.")]
    public float moveSpeed = 3.0f;
    [Tooltip("Minimum time (seconds) before the enemy picks a new random direction.")]
    public float minDirectionChangeTime = 5.0f;
    [Tooltip("Maximum time (seconds) before the enemy picks a new random direction.")]
    public float maxDirectionChangeTime = 10.0f;

    private Vector3 currentMoveDirection; // The current random direction this enemy is moving in
    private float directionChangeTimer; // Timer to track when to change direction

    private LineRenderer lineRenderer;

    /// <summary>
    /// Called when the enemy GameObject is enabled (e.g., from pool or initial spawn).
    /// Registers with the SpatialHashingManager and sets initial position and movement.
    /// </summary>
    void OnEnable()
    {
        if (SpatialHashingManager.Instance != null)
        {
            SpatialHashingManager.Instance.RegisterEnemy(this);
            lastKnownPosition = transform.position;
        }
        else
        {
            Debug.LogError("Enemy: SpatialHashingManager not found! Ensure it's in the scene.", this);
        }

        // Initialize first random direction
        GenerateRandomDirection();

        lineRenderer = gameObject.GetComponent<LineRenderer>();
        if (lineRenderer)
        {
            lineRenderer.widthMultiplier = 0.1f; // Set line width
            lineRenderer.positionCount = 2; // We need 2 points for a line segment
            lineRenderer.enabled = false; // Initially disable the line renderer
        }
    }

    /// <summary>
    /// Called when the enemy GameObject is disabled (e.g., returned to pool).
    /// Unregisters from the SpatialHashingManager.
    /// </summary>
    void OnDisable()
    {
        if (SpatialHashingManager.Instance != null)
        {
            SpatialHashingManager.Instance.UnregisterEnemy(this);
        }
        // Cancel any pending invokes or timers if necessary (not strictly needed here due to Update logic)
    }

    /// <summary>
    /// Update is called once per frame. Handles movement and spatial hash updates.
    /// </summary>
    void Update()
    {
        // --- Spatial Hashing Update Logic ---
        // Check if enemy has moved significantly to update its position in the hash map.
        if ((transform.position - lastKnownPosition).sqrMagnitude > movementThresholdSqr)
        {
            if (SpatialHashingManager.Instance != null)
            {
                SpatialHashingManager.Instance.UpdateEnemyPosition(this, lastKnownPosition);
            }
            lastKnownPosition = transform.position;
        }

        // --- Random Movement Logic ---
        directionChangeTimer -= Time.deltaTime;

        if (directionChangeTimer <= 0f)
        {
            GenerateRandomDirection(); // Time to pick a new direction
        }

        // Move the enemy in the current random direction
        transform.position += currentMoveDirection * moveSpeed * Time.deltaTime;

        // Optional: Make enemy face its current movement direction (only rotate around Y axis)
        if (currentMoveDirection.sqrMagnitude > 0.001f) // Avoid looking at zero vector
        {
            Quaternion targetRotation = Quaternion.LookRotation(currentMoveDirection);
            // Only apply Y-axis rotation to keep the enemy upright
            transform.rotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
        }
    }

    /// <summary>
    /// Generates a new random direction for the enemy to move in and resets the timer.
    /// Movement is restricted to the XZ plane (Y remains constant).
    /// </summary>
    private void GenerateRandomDirection()
    {
        // Generate a random direction on the XZ plane
        currentMoveDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;

        // Set a new random time for the next direction change
        directionChangeTimer = Random.Range(minDirectionChangeTime, maxDirectionChangeTime);

        // Debug.Log($"{name} picked new random direction. Next change in {directionChangeTimer:F1}s.", this);
    }

    public void DrawLine(Vector3 transformPosition, Color detectionLineColor)
    {
        if (lineRenderer)
        {
            lineRenderer.enabled = true; // Ensure the line is visible
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, transformPosition);
            lineRenderer.startColor = detectionLineColor;
            lineRenderer.endColor = detectionLineColor;
        }
    }

    public void RemoveLine()
    {
        if (lineRenderer)
        {
            lineRenderer.enabled = false; // Disable the line renderer to hide the line
            lineRenderer.positionCount = 0; // Optionally reset the position count to 0
        }
    }
}
