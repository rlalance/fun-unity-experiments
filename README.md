# Unity Spatial Hashing & Proximity Detection Demo

# In editor Screenshot - Scene View - 1000 enemies
<img width="2615" height="1237" alt="image" src="https://github.com/user-attachments/assets/47212f95-163e-4cbc-84db-ab03272bc719" />

# In build Screenshot - 4000 enemies
<img width="3840" height="2160" alt="image" src="https://github.com/user-attachments/assets/b4c83123-1eab-4886-863e-53a057dff9bf" />

## Project Overview

This Unity project serves as a technical demonstration of optimized nearest neighbor detection for dynamic entities within a 3D environment. It showcases a robust Spatial Hashing system combined with distributed processing and real-time visual feedback, designed to maintain high performance even with a significant number of moving objects.

## Purpose

The primary goal of this project is to illustrate an efficient solution to a common game development challenge: finding the nearest enemy (or any object) without resorting to computationally expensive brute-force methods. It highlights advanced optimization techniques applicable to large-scale simulations, AI, and collision detection in Unity.

## Core Components & Features

The project is structured around several interconnected C# scripts:

### 1. `SpatialHashingManager.cs`

* **Role:** The central component for spatial partitioning. It divides the 3D world into discrete cells and maps enemies to these cells using a hash-based dictionary.
* **Key Features:**
    * **Robust Singleton Pattern:** Ensures a single, globally accessible instance, safely initialized upon first access or scene load, preventing common Unity initialization freezes.
    * **Efficient Registration/Unregistration:** Enemies are added to and removed from the spatial hash map as they spawn/despawn.
    * **Dynamic Position Updates:** Optimized to move enemies between cells only when their position changes significantly, minimizing unnecessary updates to the hash map.
    * **Optimized `FindNearestEnemy` Query:** Given a query position and radius, it intelligently searches only the relevant grid cells (including dynamically calculated surrounding cells) to find the nearest enemy, significantly outperforming brute-force methods.
    * **Read-Only Data Exposure:** Provides `IReadOnlyDictionary` access to the internal spatial grid for debugging and inspection without breaking encapsulation.

### 2. `Enemy.cs`

* **Role:** Represents an individual dynamic entity (enemy) within the simulation.
* **Key Features:**
    * **Autonomous Random Movement:** Enemies move independently, changing direction randomly every few seconds, providing a dynamic test bed for the spatial hashing system.
    * **Spatial Hashing Integration:** Automatically registers and unregisters itself with the `SpatialHashingManager` on `OnEnable`/`OnDisable` and updates its position in the hash map when it moves beyond a threshold.
    * **Visual Detection Feedback:** Activates an attached `TrailRenderer` to draw a dynamic, fading trail behind the enemy when it successfully detects a nearest neighbor, offering clear in-game visual confirmation.

### 3. `EnemyProximityDetector.cs`

* **Role:** Orchestrates the nearest enemy detection process for all enemies.
* **Key Features:**
    * **Distributed Processing (Coroutines):** Utilizes a coroutine to spread the computational load of finding the nearest enemy for *each* active enemy over multiple frames. This prevents CPU spikes and ensures a smooth framerate, even with many enemies.
    * **Configurable Slice Size:** The `enemiesPerFrameSlice` parameter allows real-time adjustment in the Inspector, enabling fine-tuning of the balance between detection responsiveness and per-frame performance impact.
    * **Centralized Registration:** Handles the initial registration of all enemies with the `SpatialHashingManager` during its `Start` phase, ensuring proper initialization order.
    * **Visual Debugging:** Draws persistent debug lines in the Scene View (Gizmos) from each enemy to its detected nearest neighbor, and displays a wire sphere representing its search radius.

### 4. `Player.cs`

* **Role:** Provides basic player movement and a simple bullet firing mechanism.
* **Key Features:**
    * Standard WASD movement and mouse look.
    * Fires bullets from an `ObjectPooler`.

### 5. `Bullet.cs`

* **Role:** Handles bullet behavior and interaction.
* **Key Features:**
    * **Pooled Object:** Integrates with `ObjectPooler` for efficient reuse.
    * **Collision-Based Spawning:** Spawns a new enemy from the `EnemySpawner`'s pool upon colliding with the "Floor" tagged GameObject.

### 6. `EnemySpawner.cs`

* **Role:** Manages the spawning and pooling of enemy prefabs.
* **Key Features:**
    * Utilizes the `ObjectPooler` to efficiently create and reuse enemy GameObjects.
    * Provides a public method to spawn enemies at a specific location, used by the `Bullet` script.

### 7. `ObjectPooler.cs` (Assumed Existing)

* **Role:** A generic object pooling system for efficient GameObject reuse.
* **Key Features:** Reduces garbage collection and instantiation overhead by maintaining pools of pre-instantiated GameObjects.

### 8. `PerformanceStatsDisplay.cs` (TextMeshPro)

* **Role:** Displays real-time performance and game statistics on a 3D World Space Canvas.
* **Key Features:**
    * Shows FPS, MS, total enemy count, registered enemies in Spatial Hashing, and active spatial hash cells.
    * Utilizes TextMeshPro's rich text features for stylish, readable, and scalable output.

### 9. `IMGUIStatsDisplay.cs`

* **Role:** Provides an alternative, lightweight, overlaid debug display for real-time statistics.
* **Key Features:** Renders directly to the screen using Unity's IMGUI API, useful for quick debug monitoring without requiring a UI Canvas.

## Technical Achievements & Optimizations

* **Spatial Hashing (Core Optimization):** Moves nearest neighbor search from an O(N) brute-force approach to a highly efficient O(C * E_C) lookup (where C is constant cells checked, E_C is enemies per cell), drastically reducing comparisons for large N.
* **Distributed Processing:** Prevents single-frame performance spikes by spreading heavy computational tasks (like iterating all enemies for detection) across multiple frames using coroutines.
* **Robust Singleton Pattern:** Implemented a battle-tested singleton pattern for `SpatialHashingManager` that safely handles initialization, prevents duplicates, and avoids common Unity freezing issues related to script execution order.
* **Object Pooling:** Integrated for both bullets and enemies to minimize garbage collection and instantiation overhead, contributing to smoother gameplay.
* **Decoupled Initialization:** Enemy registration with the Spatial Hashing Manager is now centrally managed by the `EnemyProximityDetector` during its `Start` phase, ensuring the manager is fully initialized before enemies attempt to register.
* **Dynamic Visual Feedback:** Utilizes `TrailRenderer` for a visually engaging and performant "detection" effect directly on the enemies, enhancing debugging and user understanding.
* **Real-time Debugging Tools:** Provides multiple layers of real-time feedback (TextMeshPro 3D panel, IMGUI overlay, Scene View Gizmos) for comprehensive performance monitoring and system visualization.

## How to Run & Test

1.  **Unity Version:** This project is developed with Unity 2022.3 LTS (or a compatible recent version).
2.  **Package Requirements:** Ensure the **TextMeshPro** package is installed and its **Essential Resources** are imported (`Window > TextMeshPro > Import TMP Essential Resources`).
3.  **Scene Setup:**
    * Place an empty GameObject with `SpatialHashingManager.cs` (e.g., "GameManager").
    * Place an empty GameObject with `EnemyProximityDetector.cs` (e.g., "DetectionSystem").
    * Place an empty GameObject with `EnemySpawner.cs` (e.g., "Spawner"). Assign your `EnemyPrefab` to its slot.
    * Ensure an `ObjectPooler.cs` GameObject is in the scene and configured with pools for "Bullet" and "Enemy" tags, referencing their respective prefabs.
    * Place an empty GameObject with `PerformanceStatsDisplay.cs` (e.g., "StatsPanel"). Ensure it has a child `TextMeshPro - Text` component on a `World Space` Canvas, and the `Stats Text` field is assigned.
    * Place an empty GameObject with `IMGUIStatsDisplay.cs` (e.g., "DebugOverlay").
    * Ensure your "Floor" GameObject has a `Collider` and is tagged "Floor".
    * Ensure your `Bullet` prefab has a `Collider` (not trigger) and a `Rigidbody`.
    * Ensure your `Enemy` prefab has a `Collider` (not trigger) and a `Rigidbody` (if physics-driven movement is desired, though current movement is transform-based).
4.  **Interaction:**
    * Use **WASD** to move the player camera.
    * Use the **Mouse** to look around.
    * **Left Click** to fire bullets.
    * Observe enemies moving randomly and displaying detection trails.
    * Watch the stats panels for real-time performance and game data.
    * In the **Game View**, ensure the **"Gizmos" button** is enabled to see debug lines.

## Future Improvements

* **ECS/DOTS Migration:** For truly massive scale (tens of thousands+ entities), migrating core data and logic to Unity's Entity Component System (ECS) with Burst and Jobs would provide the ultimate performance ceiling.
* **Adaptive Spatial Structures:** Implementing Quadtrees/Octrees or dynamic spatial hashing for more varied object densities.
* **Pathfinding Integration:** Connecting enemy movement to a navigation system (e.g., Unity's NavMesh) for more intelligent behavior.
* **Event-Driven Updates:** For spatial hashing updates, instead of `UpdateEnemyPosition` being called by the enemy, a central system could listen for movement events or physics updates to trigger cell re-evaluation.
* **Optimized `FindObjectsOfType<Enemy>()`:** Replace this with a pre-cached and managed list of enemies in `EnemyProximityDetector` for even greater efficiency.

---
