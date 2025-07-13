using System.Collections;
using NUnit.Framework;
using UnityEngine;

public class SpatialHashingManagerTests
{
    private SpatialHashingManager manager;
    private GameObject managerGO; // GameObject holding the manager script

    [SetUp]
    public void SetUp()
    {
        // Ensure only one instance of manager for each test run
        if (SpatialHashingManager.Instance != null)
        {
            Object.Destroy(SpatialHashingManager.Instance.gameObject);
        }

        managerGO = new GameObject("SpatialHashingManager");
        manager = managerGO.AddComponent<SpatialHashingManager>();
        manager.cellSize = 10f; // Set a default cell size for consistent testsa
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up all Enemy GameObjects created during the test
        // This is important to prevent leftover objects affecting subsequent tests.
        var enemies = Object.FindObjectsOfType<Enemy>();
        foreach(var enemy in enemies)
        {
            Object.Destroy(enemy.gameObject);
        }

        // Destroy the manager GameObject itself
        if (managerGO != null)
        {
            Object.Destroy(managerGO);
        }
    }

    /// <summary>
    /// Helper method to create a simple GameObject with an Enemy component.
    /// Used to simulate enemies for testing purposes.
    /// </summary>
    private GameObject CreateEnemyGO(string name = "TestEnemy")
    {
        GameObject go = new GameObject(name);
        Enemy enemy = go.AddComponent<Enemy>();
        enemy.gameObject.AddComponent<LineRenderer>();

        return go;
    }


    // --- TEST SUITE: GetCellCoordinates ---
    [Test]
    public void GetCellCoordinates_ZeroPosition_ReturnsZeroCell()
    {
        Vector3Int coords = manager.GetCellCoordinates(Vector3.zero);
        Assert.AreEqual(new Vector3Int(0, 0, 0), coords);
    }

    [Test]
    public void GetCellCoordinates_PositivePosition_ReturnsCorrectCell()
    {
        // Position within the first cell (0,0,0)
        Vector3Int coords = manager.GetCellCoordinates(new Vector3(5, 5, 5));
        Assert.AreEqual(new Vector3Int(0, 0, 0), coords);

        // Position exactly at the boundary, should be in the next cell
        coords = manager.GetCellCoordinates(new Vector3(10, 10, 10));
        Assert.AreEqual(new Vector3Int(1, 1, 1), coords);

        // Position just before a boundary
        coords = manager.GetCellCoordinates(new Vector3(19.9f, 0.1f, 29.9f));
        Assert.AreEqual(new Vector3Int(1, 0, 2), coords);
    }

    [Test]
    public void GetCellCoordinates_NegativePosition_ReturnsCorrectCell()
    {
        // Position just below zero, should be in cell (-1,-1,-1)
        Vector3Int coords = manager.GetCellCoordinates(new Vector3(-0.1f, -0.1f, -0.1f));
        Assert.AreEqual(new Vector3Int(-1, -1, -1), coords);

        // Position within cell (-1,-1,-1)
        coords = manager.GetCellCoordinates(new Vector3(-5f, -5f, -5f));
        Assert.AreEqual(new Vector3Int(-1, -1, -1), coords);

        // Position within cell (-2,-3,-4)
        coords = manager.GetCellCoordinates(new Vector3(-10.1f, -20.0f, -30.01f));
        Assert.AreEqual(new Vector3Int(-2, -2, -4), coords); // Mathf.FloorToInt(-1.01) = -2, FloorToInt(-2) = -2, FloorToInt(-3.001) = -4
    }

    // --- TEST SUITE: RegisterEnemy ---
    [Test]
    public void RegisterEnemy_AddsEnemyToAllEnemiesListAndHashMap()
    {
        GameObject enemyGO = CreateEnemyGO();
        Enemy enemy = enemyGO.GetComponent<Enemy>();
        enemy.transform.position = Vector3.one; // Place enemy for cell calculation

        manager.RegisterEnemy(enemy);

        Assert.AreEqual(1, manager.GetRegisteredEnemyCount(), "Should have 1 enemy registered.");
        Vector3Int expectedCell = manager.GetCellCoordinates(enemy.transform.position);
        Assert.IsTrue(manager.SpatialGrid.ContainsKey(expectedCell), "Hash map should contain the enemy's cell.");
        Assert.Contains(enemy, manager.SpatialGrid[expectedCell], "Enemy should be in its cell's list.");
        Assert.AreEqual(1, manager.SpatialGrid[expectedCell].Count, "Cell list should contain exactly one enemy.");

        // Test duplicate registration: should not add the same enemy again
        manager.RegisterEnemy(enemy);
        Assert.AreEqual(1, manager.GetRegisteredEnemyCount(), "Duplicate registration should not increase count.");
        Assert.AreEqual(1, manager.SpatialGrid[expectedCell].Count, "Duplicate registration should not add to cell list.");
    }

    [Test]
    public void RegisterEnemy_HandlesNullEnemy()
    {
        int initialCount = manager.GetRegisteredEnemyCount();

        manager.RegisterEnemy(null); // Attempt to register a null enemy

        Assert.AreEqual(initialCount, manager.GetRegisteredEnemyCount(), "Registering a null enemy should not change count.");
    }

    [Test]
    public void UnregisterEnemy_RemovesEnemyFromAllEnemiesListAndHashMap()
    {
        GameObject enemyGO = CreateEnemyGO();
        Enemy enemy = enemyGO.GetComponent<Enemy>();
        enemy.transform.position = Vector3.one;
        
        manager.RegisterEnemy(enemy);

        Assert.AreEqual(1, manager.GetRegisteredEnemyCount(), "Pre-condition: 1 enemy should be registered.");
        Vector3Int initialCell = manager.GetCellCoordinates(enemy.transform.position);
        Assert.IsTrue(manager.SpatialGrid.ContainsKey(initialCell), "Pre-condition: Cell should exist.");

        manager.UnregisterEnemy(enemy);

        Assert.AreEqual(0, manager.GetRegisteredEnemyCount(), "Enemy should be removed from allEnemies list.");
        Assert.IsFalse(manager.SpatialGrid.ContainsKey(initialCell), "Cell should be removed from hash map if it becomes empty.");
    }


    [Test]
    public void UnregisterEnemy_HandlesNullOrUnregisteredEnemy()
    {
        GameObject enemyGO1 = CreateEnemyGO("Enemy1");
        Enemy enemy1 = enemyGO1.GetComponent<Enemy>();
        enemy1.transform.position = Vector3.zero;
        manager.RegisterEnemy(enemy1); // Register first enemy

        GameObject enemyGO2 = CreateEnemyGO("Enemy2");
        Enemy enemy2 = enemyGO2.GetComponent<Enemy>();
        enemy2.transform.position = Vector3.one * 100; // Far away in a different cell
        manager.RegisterEnemy(enemy2); // Register second enemy

        Assert.AreEqual(2, manager.GetRegisteredEnemyCount(), "Pre-condition: 2 enemies should be registered.");

        manager.UnregisterEnemy(null); // Attempt to unregister null
        Assert.AreEqual(2, manager.GetRegisteredEnemyCount(), "Unregistering null should not change count.");

        // Create a GameObject with Enemy, but don't let it register or destroy it
        GameObject nonRegisteredGO = new GameObject("NonRegistered");
        Enemy nonRegisteredEnemy = nonRegisteredGO.AddComponent<Enemy>();
        Object.Destroy(nonRegisteredGO); // Destroy it immediately so OnEnable doesn't auto-register

        manager.UnregisterEnemy(nonRegisteredEnemy); // Attempt to unregister a non-registered/destroyed enemy
        Assert.AreEqual(2, manager.GetRegisteredEnemyCount(), "Unregistering a non-registered enemy should not change count.");

        // Clean up remaining enemies
        Object.Destroy(enemyGO1);
        Object.Destroy(enemyGO2);
    }

    // Rest of the Unit tests for FindNearestEnemy, GetCellCoordinates, etc. will be added later. RL.
}
