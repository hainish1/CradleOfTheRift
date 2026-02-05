using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    private ExtractionZone extractionZone;
    private float heightOffset = 5f;
    [SerializeField] private float spawnDelay = 1f;
    [SerializeField] private GameObject spawnVFXPrefab;

    private EnemyHealth activeBoss;
    public event Action BossDied;

    private void Awake()
    {
        extractionZone = GetComponent<ExtractionZone>();
        if (extractionZone == null)
            Debug.LogError("BossSpawner: ExtractionZone component not found!");
    }

    private void OnEnable()
    {
        extractionZone.BossSpawnRequested += OnBossSpawnRequested;
    }

    private void OnDisable()
    {
        extractionZone.BossSpawnRequested -= OnBossSpawnRequested;
    }

    private void OnBossSpawnRequested()
    {
        StartCoroutine(SpawnBossDelayed());
    }

    private IEnumerator SpawnBossDelayed()
    {
        yield return new WaitForSeconds(this.spawnDelay);

        Transform spawn = extractionZone.GetSpawnPoint;
        UnityEngine.Vector3 spawnPoint = spawn.position + UnityEngine.Vector3.up * heightOffset;

        BossType randomBoss = ExtractionManager.Instance.GetNextBoss();

        // Ground alignment
        if (!randomBoss.isFlying)
        {
            if (Physics.Raycast(spawnPoint, UnityEngine.Vector3.down, out RaycastHit hit, 10f))
            {
                spawnPoint.y = hit.point.y;
            }
        }

        PlaySpawnVFX(spawnPoint, UnityEngine.Quaternion.identity);
        GameObject boss = Instantiate(randomBoss.prefab, spawnPoint, UnityEngine.Quaternion.identity);
        this.activeBoss = boss.GetComponent<EnemyHealth>();
        this.activeBoss.EnemyDied += OnBossDied;
    }

    private void OnBossDied(EnemyHealth deadBoss)
    {
        if (this.activeBoss != null)
        {
            this.activeBoss.EnemyDied -= OnBossDied;
        }
        this.activeBoss = null;
        this.BossDied?.Invoke();
    }

    private void PlaySpawnVFX(UnityEngine.Vector3 position, UnityEngine.Quaternion rotation)
    {
        if(spawnVFXPrefab == null) return;
        GameObject vfx = Instantiate(spawnVFXPrefab, position, rotation);

        Destroy(vfx, 4.0f); // Should prob make the VFX auto destroy instead of doing it here.
    }
}

[Serializable]
public class BossType
{
    public GameObject prefab;
    public bool isFlying;    
}
