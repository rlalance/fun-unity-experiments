using UnityEngine;

public class Enemy : MonoBehaviour, IPoolableObject
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
            currentMoveDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;

            directionChangeTimer = Random.Range(minDirectionChangeTime, maxDirectionChangeTime);
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

    public void DrawLine(Vector3 nearestEnemyPosition)
    {
        if (lineRenderer)
        {
            lineRenderer.enabled = true; // Ensure the line is visible
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, nearestEnemyPosition);
        }
    }

    public void RemoveLine()
    {
        if (lineRenderer)
        {
            lineRenderer.enabled = false; // Disable the line renderer
        }
    }
    
    public void OnObjectSpawn()
    {
        SpatialHashingManager.Instance.RegisterEnemy(this);
        lastKnownPosition = transform.position;

        lineRenderer = gameObject.GetComponent<LineRenderer>();

        if (lineRenderer)
        {
            lineRenderer.widthMultiplier = 0.1f; // Set line width
            lineRenderer.positionCount = 2; // We need 2 points for a line segment
            lineRenderer.enabled = false; // Initially disable the line renderer
            lineRenderer.startColor = Color.blue;
            lineRenderer.endColor = Color.cyan;
        }
    }

    public void OnObjectDespawn()
    {
        SpatialHashingManager.Instance.UnregisterEnemy(this);
    }
}
