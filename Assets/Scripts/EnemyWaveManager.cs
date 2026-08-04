/*
 * File: EnemyWaveManager.cs
 * Purpose:
 *   Encapsulates wave timing, spawn scaling and a simple active-enemy registry
 *   used by AI systems to manage wave progression.
 */

using UnityEngine;

[System.Serializable]
// summary: Serializable container used to configure and drive enemy wave progression, spawn timing and simple active enemy tracking.
public class EnemyWaveManager
{
    [Header("Wave Settings")]
    public int currentWave = 0;
    public int maxWaves = 10;
    public float timeBetweenWaves = 10f;

    [Header("Spawn Scaling")]
    [Tooltip("How much faster enemies spawn each wave.")]
    public float spawnIntervalReductionRate = 0.95f;
    [Tooltip("How much the enemy count increases each wave.")]
    public float enemyCountIncreaseRate = 1.2f;
    [Tooltip("Minimum allowed spawn interval.")]
    public float minSpawnInterval = 0.5f;

    [Header("Spawn Settings")]
    public float spawnInterval = 2f;
    public float enemySpeedMultiplier = 1f;

    [Header("Enemy Pool")]
    [Tooltip("Assign enemy prefabs that can be spawned.")]
    public GameObject[] enemyPrefabs;

    // Track active spawned enemies so new waves wait for previous enemies to be cleared
    private readonly System.Collections.Generic.List<Enemy> activeEnemies = new System.Collections.Generic.List<Enemy>();

    public void RegisterEnemy(Enemy e)
    {
        if (e == null) return;
        if (!activeEnemies.Contains(e)) activeEnemies.Add(e);
    }

    // summary: Register an enemy in the active list so the wave manager knows it is present.

    public void UnregisterEnemy(Enemy e)
    {
        if (e == null) return;
        activeEnemies.Remove(e);
    }

    // summary: Unregisters an enemy when it is destroyed or removed from play.

    public bool HasActiveEnemies()
    {
        return activeEnemies.Count > 0;
    }

    // summary: Returns true when there are active spawned enemies tracked by the manager.

    public GameObject GetNextEnemy(Vector3 spawnPosition)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("EnemyWaveManager: No prefabs assigned!");
            return null;
        }

        int randomIndex = Random.Range(0, enemyPrefabs.Length);
        return enemyPrefabs[randomIndex];
    }

    // summary: Selects the next enemy prefab to spawn for a given spawn position.

    public bool IsWaveCapReached()
    {
        return currentWave >= maxWaves;
    }

    // summary: Returns true when the current wave index has reached the configured cap.

    public void AdvanceWave()
    {
        if (IsWaveCapReached())
        {
            return;
        }

        currentWave++;
        spawnInterval = Mathf.Max(minSpawnInterval, spawnInterval * spawnIntervalReductionRate);
        enemySpeedMultiplier += 0.1f;

        // Debug.Log($"Wave {currentWave} started! Spawn interval: {spawnInterval:F2}s, Speed x{enemySpeedMultiplier:F2}");
    }

    // summary: Advance to the next wave, adjusting spawn interval and speed scaling.

    public int GetEnemiesToSpawnForCurrentWave()
    {
        int baseCount = 5;
        int count = Mathf.RoundToInt(baseCount * Mathf.Pow(enemyCountIncreaseRate, currentWave - 1));
        return Mathf.Max(1, count);
    }

    // summary: Calculates how many enemies should spawn for the current wave based on scaling.
}
