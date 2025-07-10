// ObjectPooler.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An interface for objects that can be pooled. Implement this to receive callbacks
/// when the object is spawned from and returned to the pool.
/// </summary>
public interface IPoolableObject
{
    void OnObjectSpawn();
    void OnObjectDespawn();
}

public class ObjectPooler : MonoBehaviour
{
    #region Singleton

    public static ObjectPooler Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePools();
    }

    #endregion

    // A class to define the properties of a pool in the Inspector.
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int initialSize;
        
        // This queue will be populated at runtime
        public Queue<GameObject> objectQueue;
    }

    // A list of all pools to be created, configured in the Inspector.
    public List<Pool> poolConfigurations;

    // The master dictionary that holds all the pools, keyed by their tag.
    private Dictionary<string, Pool> _pools;

    /// <summary>
    /// Initializes all pools based on the configurations set in the Inspector.
    /// </summary>
    private void InitializePools()
    {
        _pools = new Dictionary<string, Pool>();

        foreach (var config in poolConfigurations)
        {
            config.objectQueue = new Queue<GameObject>();

            for (int i = 0; i < config.initialSize; i++)
            {
                GameObject obj = Instantiate(config.prefab, transform); // Parent to the pooler
                obj.SetActive(false);
                config.objectQueue.Enqueue(obj);
            }
            
            _pools.Add(config.tag, config);
            Debug.Log($"Pool with tag '{config.tag}' initialized with {config.initialSize} objects.");
        }
    }

    /// <summary>
    /// Spawns an object from the pool with the specified tag.
    /// </summary>
    /// <param name="tag">The tag of the pool to get an object from.</param>
    /// <param name="position">The world position to spawn the object at.</param>
    /// <param name="rotation">The world rotation to spawn the object with.</param>
    /// <returns>The spawned GameObject, or null if the tag is not found.</returns>
    public GameObject GetFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!_pools.TryGetValue(tag, out var pool))
        {
            Debug.LogWarning($"Pool with tag '{tag}' doesn't exist.");
            return null;
        }

        GameObject objectToSpawn;

        // --- Pool Expansion ---
        // If the queue is empty, instantiate a new object.
        if (pool.objectQueue.Count == 0)
        {
            objectToSpawn = Instantiate(pool.prefab, transform);
            
            // We can add a component to track its original pool tag, useful for returning without knowing the tag.
            // For now, we'll just log a warning for performance tracking.
            Debug.LogWarning($"Pool with tag '{tag}' is empty. A new object was created. Consider increasing the initial size.");
        }
        else
        {
            objectToSpawn = pool.objectQueue.Dequeue();
        }

        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        objectToSpawn.SetActive(true);

        // Call the OnObjectSpawn method if the object implements the interface.
        IPoolableObject poolable = objectToSpawn.GetComponent<IPoolableObject>();
        if (poolable != null)
        {
            poolable.OnObjectSpawn();
        }

        return objectToSpawn;
    }
    
    /// <summary>
    /// Returns an object to its corresponding pool.
    /// </summary>
    /// <param name="tag">The tag of the pool the object belongs to.</param>
    /// <param name="objectToReturn">The GameObject to return.</param>
    public void ReturnToPool(string tag, GameObject objectToReturn)
    {
        if (!_pools.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag '{tag}' doesn't exist. Object will be destroyed.");
            Destroy(objectToReturn);
            return;
        }
        
        objectToReturn.SetActive(false);
        
        // Call the OnObjectDespawn method if the object implements the interface.
        IPoolableObject poolable = objectToReturn.GetComponent<IPoolableObject>();
        if (poolable != null)
        {
            poolable.OnObjectDespawn();
        }

        _pools[tag].objectQueue.Enqueue(objectToReturn);
    }
}