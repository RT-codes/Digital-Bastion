/*
 * File: AI_Manager.cs
 * Purpose:
 *   High-level AI coordination for spawning enemy waves and
 *   advancing wave progression based on the EnemyWaveManager.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// summary: Coordinates enemy spawn waves and interacts with the EnemyWaveManager to spawn enemies at runtime.
public class AI_Manager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GameObject enemySpawner;
    [SerializeField] private GameObject playerBase;
    [SerializeField] private GridManager gridManager;

    [Header("Wave Management")]
    [SerializeField] private EnemyWaveManager waveManager;

    void Start()
    {
        if (enemySpawner != null && playerBase != null)
        {
            StartCoroutine(WaveRoutine());
        }
        else
        {
            Debug.LogError("AI_Manager: Please assign Spawner and Base in the Inspector!");
        }
    }

    private IEnumerator WaveRoutine()
    {
        while (!waveManager.IsWaveCapReached())
        {
            // Wait until any remaining enemies from the previous wave are gone
            while (waveManager.HasActiveEnemies())
            {
                yield return null;
            }

            waveManager.AdvanceWave();

            if (waveManager.IsWaveCapReached())
            {
                yield break;
            }

            int enemiesToSpawn = waveManager.GetEnemiesToSpawnForCurrentWave();
            for (int i = 0; i < enemiesToSpawn; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(waveManager.spawnInterval);
            }

            yield return new WaitForSeconds(waveManager.timeBetweenWaves);
        }
    }

    private void SpawnEnemy()
    {
        // Determine a randomized spawn position within the enemySpawner bounds (x and z)
        Vector3 spawnPos = enemySpawner.transform.position;
        Bounds spawnBounds = new Bounds(enemySpawner.transform.position, Vector3.zero);

        // Prefer an enabled collider on the spawner (including children). If none, use a Renderer in children.
        Collider col = enemySpawner.GetComponentInChildren<Collider>();
        if (col != null && col.enabled)
        {
            spawnBounds = col.bounds;
        }
        else
        {
            Renderer rend = enemySpawner.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                spawnBounds = rend.bounds;
            }
            else
            {
                // Fallback: use transform lossyScale as approximate bounds
                spawnBounds = new Bounds(enemySpawner.transform.position, enemySpawner.transform.lossyScale);
            }
        }

        float randX = Random.Range(spawnBounds.min.x, spawnBounds.max.x);
        float randZ = Random.Range(spawnBounds.min.z, spawnBounds.max.z);
        // Spawn slightly above the top of the bounds to avoid spawning inside the spawner
        float y = spawnBounds.max.y + 0.1f;
        spawnPos = new Vector3(randX, y, randZ);

        GameObject prefab = waveManager.GetNextEnemy(spawnPos);
        if (prefab != null)
        {
            GameObject enemyObj = Instantiate(prefab, spawnPos, Quaternion.identity);
            
            // Setup the Enemy component
            Enemy enemyScript = enemyObj.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                enemyScript.Initialize(playerBase.transform, waveManager.enemySpeedMultiplier, gridManager, waveManager);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
