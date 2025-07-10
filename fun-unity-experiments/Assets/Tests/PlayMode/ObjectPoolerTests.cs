using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// AI Generated Code
// Not a big fan of using logs for observing behavior in tests, but good enough for now. RL.
public class ObjectPoolerTests
{
    private GameObject _objectPoolerGameObject;
    private ObjectPooler _objectPooler;
    private List<string> _debugLogs;
    private List<string> _debugWarnings;
    private GameObject _dummyPrefab; // Used as a generic prefab for tests

    // A mock IPoolableObject for testing callbacks
    private class MockPoolableObject : MonoBehaviour, IPoolableObject
    {
        public bool onSpawnCalled;
        public bool onDespawnCalled;
        
        public void OnObjectSpawn()
        {
            onSpawnCalled = true;
        }

        public void OnObjectDespawn()
        {
            onDespawnCalled = true;
        }
    }

    [SetUp]
    public void Setup()
    {
        // Reset singleton instance before each test to ensure a clean state.
        if (ObjectPooler.Instance != null)
        {
            Object.DestroyImmediate(ObjectPooler.Instance.gameObject);
        }

        // Create a dummy prefab. This is useful for tests that need a prefab
        // but don't care about its specific properties, or to prevent NREs
        // if a test configures a pool with a prefab that's not explicitly set.
        _dummyPrefab = new GameObject("DummyPrefab");

        // Create a new GameObject and add the ObjectPooler component.
        // The ObjectPooler's Awake() method will be called implicitly here,
        // which handles the singleton setup but no longer initializes pools automatically.
        _objectPoolerGameObject = new GameObject("ObjectPooler");
        _objectPooler = _objectPoolerGameObject.AddComponent<ObjectPooler>();

        // Initialize lists to capture debug messages for assertions.
        _debugLogs = new List<string>();
        _debugWarnings = new List<string>();

        // Attach a log handler to capture Unity's Debug.Log messages.
        // This allows us to assert on warnings/errors logged by the ObjectPooler.
        Application.logMessageReceived += HandleLog;
    }

    [TearDown]
    public void Teardown()
    {
        // Remove the log handler first to prevent it from capturing logs during cleanup.
        Application.logMessageReceived -= HandleLog;

        // Clean up the created GameObject and ObjectPooler instance after each test.
        if (_objectPoolerGameObject != null)
        {
            Object.DestroyImmediate(_objectPoolerGameObject);
        }
        _objectPooler = null;

        // Clean up the dummy prefab.
        if (_dummyPrefab != null)
        {
            Object.DestroyImmediate(_dummyPrefab);
        }
    }

    // Custom log handler to capture messages for assertions.
    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Log)
        {
            _debugLogs.Add(logString);
        }
        else if (type == LogType.Warning)
        {
            _debugWarnings.Add(logString);
        }
    }

    [Test]
    public void ObjectPooler_SingletonInstance_IsSetCorrectly()
    {
        // This test specifically checks the singleton behavior of ObjectPooler.
        // The Instance should be set by the Awake() call when AddComponent is used in Setup().
        Assert.IsNotNull(ObjectPooler.Instance);
        Assert.AreEqual(_objectPooler, ObjectPooler.Instance);
    }

    [Test]
    public void ObjectPooler_InitializesPools_Correctly()
    {
        // Arrange: Create a new, isolated ObjectPooler instance for this test
        // to fully control its initialization without interference from Setup().
        GameObject testPoolerGameObject = new GameObject("TestObjectPoolerForInit");
        ObjectPooler testObjectPooler = testPoolerGameObject.AddComponent<ObjectPooler>();

        // Create specific prefabs for this test
        var prefab1 = new GameObject("Prefab1");
        var prefab2 = new GameObject("Prefab2");

        var poolConfig1 = new ObjectPooler.Pool
        {
            tag = "TestPool1",
            prefab = prefab1,
            initialSize = 5
        };
        
        var poolConfig2 = new ObjectPooler.Pool
        {
            tag = "TestPool2",
            prefab = prefab2,
            initialSize = 3
        };

        testObjectPooler.poolConfigurations = new List<ObjectPooler.Pool> { poolConfig1, poolConfig2 };

        testObjectPooler.SetupPooler();

        // Act - No explicit action needed here as SetupPooler() initializes the pools.

        // Assert
        // Use reflection to access the private _pools dictionary for verification.
        var privatePoolsField = typeof(ObjectPooler).GetField("_pools", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var pools = privatePoolsField.GetValue(testObjectPooler) as Dictionary<string, ObjectPooler.Pool>;

        Assert.IsNotNull(pools);
        Assert.AreEqual(2, pools.Count);
        Assert.IsTrue(pools.ContainsKey("TestPool1"));
        Assert.IsTrue(pools.ContainsKey("TestPool2"));

        // Check initial sizes and ensure all pooled objects are inactive.
        Assert.AreEqual(5, pools["TestPool1"].objectQueue.Count);
        foreach (var obj in pools["TestPool1"].objectQueue)
        {
            Assert.IsFalse(obj.activeSelf);
        }

        Assert.AreEqual(3, pools["TestPool2"].objectQueue.Count);
        foreach (var obj in pools["TestPool2"].objectQueue)
        {
            Assert.IsFalse(obj.activeSelf);
        }

        // Clean up prefabs and the temporary GameObject created for this test.
        Object.DestroyImmediate(prefab1);
        Object.DestroyImmediate(prefab2);
        Object.DestroyImmediate(testPoolerGameObject);
    }

    [Test]
    public void ObjectPooler_GetFromPool_ReturnsActiveObject()
    {
        // Arrange
        var prefab = new GameObject("BulletPrefab");
        _objectPooler.poolConfigurations = new List<ObjectPooler.Pool>
        {
            new ObjectPooler.Pool { tag = "Bullet", prefab = prefab, initialSize = 1 }
        };

        _objectPooler.SetupPooler();

        // Act
        GameObject bullet = _objectPooler.GetFromPool("Bullet", Vector3.zero, Quaternion.identity);

        // Assert
        Assert.IsNotNull(bullet);
        Assert.IsTrue(bullet.activeSelf);
        // Ensure the object was dequeued from the pool.
        Assert.AreEqual(0, _objectPooler.poolConfigurations[0].objectQueue.Count);

        // Clean up
        Object.DestroyImmediate(prefab);
    }

    [Test]
    public void ObjectPooler_GetFromPool_CreatesNewObjectIfPoolEmpty()
    {
        // Arrange
        var prefab = new GameObject("EnemyPrefab");
        _objectPooler.poolConfigurations = new List<ObjectPooler.Pool>
        {
            new ObjectPooler.Pool { tag = "Enemy", prefab = prefab, initialSize = 0 } // Start with an empty pool
        };

        _objectPooler.SetupPooler();

        // Act
        GameObject enemy = _objectPooler.GetFromPool("Enemy", Vector3.zero, Quaternion.identity);

        // Assert
        Assert.IsNotNull(enemy);
        Assert.IsTrue(enemy.activeSelf);
        // Check if a warning was logged about creating a new object, indicating pool expansion.
        Assert.IsTrue(_debugWarnings.Any(log => log.Contains("Pool with tag 'Enemy' is empty. A new object was created.")));

        // Clean up
        Object.DestroyImmediate(prefab);
    }

    [Test]
    public void ObjectPooler_GetFromPool_ReturnsNullForNonExistentTag()
    {
        // Arrange
        _objectPooler.poolConfigurations = new List<ObjectPooler.Pool>(); // No pools configured

        _objectPooler.SetupPooler();

        // Act
        GameObject obj = _objectPooler.GetFromPool("NonExistent", Vector3.zero, Quaternion.identity);

        // Assert
        Assert.IsNull(obj);
        // Verify that a warning was logged for the non-existent pool.
        Assert.IsTrue(_debugWarnings.Any(log => log.Contains("Pool with tag 'NonExistent' doesn't exist or pools are not initialized.")));
    }

    [Test]
    public void ObjectPooler_ReturnToPool_DeactivatesAndEnqueuesObject()
    {
        // Arrange
        var prefab = new GameObject("CoinPrefab");
        _objectPooler.poolConfigurations = new List<ObjectPooler.Pool>
        {
            new ObjectPooler.Pool { tag = "Coin", prefab = prefab, initialSize = 1 }
        };
        // Explicitly call SetupPooler to initialize the pool for this test.
        _objectPooler.SetupPooler();

        GameObject coin = _objectPooler.GetFromPool("Coin", Vector3.zero, Quaternion.identity);
        Assert.IsTrue(coin.activeSelf); // Should be active after getting from pool
        Assert.AreEqual(0, _objectPooler.poolConfigurations[0].objectQueue.Count); // Queue should be empty after getting the object

        // Act
        _objectPooler.ReturnToPool("Coin", coin);

        // Assert
        Assert.IsFalse(coin.activeSelf); // Should be inactive after returning to the pool
        Assert.AreEqual(1, _objectPooler.poolConfigurations[0].objectQueue.Count); // Queue should have the object back
        Assert.AreEqual(coin, _objectPooler.poolConfigurations[0].objectQueue.Peek()); // The returned object should be the one at the front of the queue

        // Clean up
        Object.DestroyImmediate(prefab);
    }

    [Test]
    public void ObjectPooler_ReturnToPool_DestroysObjectForNonExistentTag()
    {
        // Arrange
        _objectPooler.poolConfigurations = new List<ObjectPooler.Pool>(); // No pools configured
        // Explicitly call SetupPooler to initialize the pool for this test (it will be empty).
        _objectPooler.SetupPooler();

        GameObject dummyObject = new GameObject("Dummy");

        // Act
        _objectPooler.ReturnToPool("NonExistent", dummyObject);

        // Assert
        // Verify that a warning was logged, indicating the object would be destroyed.
        Assert.IsTrue(_debugWarnings.Any(log => log.Contains("Pool with tag 'NonExistent' doesn't exist or pools are not initialized. Object will be destroyed.")));
        Object.DestroyImmediate(dummyObject); // Ensure it's cleaned up if not destroyed by the pooler's logic
    }

    [Test]
    public void ObjectPooler_GetFromPool_CallsOnObjectSpawn()
    {
        // Arrange
        var prefab = new GameObject("SpawnTestPrefab");
        prefab.AddComponent<MockPoolableObject>(); // Add the mock component to the prefab
        _objectPooler.poolConfigurations = new List<ObjectPooler.Pool>
        {
            new ObjectPooler.Pool { tag = "SpawnTest", prefab = prefab, initialSize = 1 }
        };
        // Explicitly call SetupPooler to initialize the pool for this test.
        _objectPooler.SetupPooler();

        // Act
        GameObject spawnedObject = _objectPooler.GetFromPool("SpawnTest", Vector3.zero, Quaternion.identity);
        MockPoolableObject poolable = spawnedObject.GetComponent<MockPoolableObject>();

        // Assert
        Assert.IsNotNull(poolable);
        Assert.IsTrue(poolable.onSpawnCalled); // Verify OnObjectSpawn was called

        // Clean up
        Object.DestroyImmediate(prefab);
    }

    [Test]
    public void ObjectPooler_ReturnToPool_CallsOnObjectDespawn()
    {
        // Arrange
        var prefab = new GameObject("DespawnTestPrefab");
        prefab.AddComponent<MockPoolableObject>(); // Add the mock component to the prefab
        _objectPooler.poolConfigurations = new List<ObjectPooler.Pool>
        {
            new ObjectPooler.Pool { tag = "DespawnTest", prefab = prefab, initialSize = 1 }
        };
        // Explicitly call SetupPooler to initialize the pool for this test.
        _objectPooler.SetupPooler();

        GameObject spawnedObject = _objectPooler.GetFromPool("DespawnTest", Vector3.zero, Quaternion.identity);
        MockPoolableObject poolable = spawnedObject.GetComponent<MockPoolableObject>();
        poolable.onSpawnCalled = false; // Reset the flag, as we are testing despawn now

        // Act
        _objectPooler.ReturnToPool("DespawnTest", spawnedObject);

        // Assert
        Assert.IsNotNull(poolable);
        Assert.IsTrue(poolable.onDespawnCalled); // Verify OnObjectDespawn was called

        // Clean up
        Object.DestroyImmediate(prefab);
    }

    [Test]
    public void ObjectPooler_SetupPooler_HandlesNullPrefabsInConfig()
    {
        // Arrange
        var poolConfig = new ObjectPooler.Pool
        {
            tag = "NullPrefabPool",
            prefab = null, // Null prefab
            initialSize = 5
        };
        _objectPooler.poolConfigurations = new List<ObjectPooler.Pool> { poolConfig };

        // Act
        _objectPooler.SetupPooler();

        // Assert
        // Check if a warning was logged for the null prefab
        Assert.IsTrue(_debugWarnings.Any(log => log.Contains("ObjectPooler: Pool with tag 'NullPrefabPool' has a null prefab. Skipping initialization for this pool.")));
        // Ensure the pool was not added to the internal dictionary
        var privatePoolsField = typeof(ObjectPooler).GetField("_pools", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var pools = privatePoolsField.GetValue(_objectPooler) as Dictionary<string, ObjectPooler.Pool>;
        Assert.IsNotNull(pools);
        Assert.IsFalse(pools.ContainsKey("NullPrefabPool"));
        Assert.AreEqual(0, pools.Count); // Should be 0 if only the null-prefab pool was configured
    }
}
