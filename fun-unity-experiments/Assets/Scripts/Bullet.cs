using UnityEngine;

/// <summary>
/// Handles bullet behavior, including collision detection and returning to pool.
/// </summary>
public class Bullet : MonoBehaviour
{
    // A reference to the ObjectPooler instance (assuming it's a singleton)
    // or you can get it at Awake if not directly assigned.
    // Make sure your BULLET_TAG matches the one used in ObjectPooler
    private const string BULLET_TAG = "Bullet";
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("Bullet Rigidbody missing. Consider adding one for physics-based collisions.", this);
        }
    }

    void OnEnable()
    {
        // Reset velocity when enabled from pool to prevent inheriting old momentum
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        // You might want to set a timer to return the bullet to the pool after a certain time
        // in case it never hits anything.
        // Invoke("ReturnToPool", 5f); // Example: Return after 5 seconds
    }

    void OnDisable()
    {
        // Cancel any pending invokes if the bullet is returned to pool prematurely
        // CancelInvoke("ReturnToPool");
    }

    // This method is called when this collider/rigidbody has begun touching another rigidbody/collider.
    void OnCollisionEnter(Collision collision)
    {
        // You could check for specific tags or layers here
        // For example, if your floor is tagged "Floor"
        if (collision.gameObject.CompareTag("Floor"))
        {
            // Spawn an enemy at the collision point
            var pointOfContact = collision.contacts[0].point;
            pointOfContact.y = 0;
            
            EnemySpawner.Instance?.SpawnEnemyAt(pointOfContact); // Assuming EnemySpawner is a singleton

            // Return the bullet to the pool
            ReturnToPool();
        }
    }

    /// <summary>
    /// Returns this bullet object to its corresponding pool.
    /// </summary>
    private void ReturnToPool()
    {
        // Assuming ObjectPooler.Instance.ReturnToPool exists and knows which pool it belongs to
        // If your pooler uses tags, ensure this matches the tag used to get it.
        ObjectPooler.Instance.ReturnToPool(BULLET_TAG, gameObject);
    }
}