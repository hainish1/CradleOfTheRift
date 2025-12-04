using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    private ExtractionZone extractionZone;
    [SerializeField] private List<BossType> bosses;
    private float heightOffset = 5f;
    [SerializeField] private float spawnDelay = 1f;

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
        Vector3 spawnPoint = spawn.position + Vector3.up * heightOffset;

        BossType randomBoss = bosses[UnityEngine.Random.Range(0, bosses.Count)];

        // Ground alignment
        if (!randomBoss.isFlying)
        {
            if (Physics.Raycast(spawnPoint, Vector3.down, out RaycastHit hit, 10f))
            {
                spawnPoint.y = hit.point.y;
            }
        }

        GameObject boss = Instantiate(randomBoss.prefab, spawnPoint, Quaternion.identity);
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
}

[Serializable]
public class BossType
{
    public GameObject prefab;
    public bool isFlying;    
}
