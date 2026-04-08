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
    [SerializeField] private DifficultyScaler difficultyScaler;


    [Header("Scaling Curves")]
    [SerializeField] private AnimationCurve healthScaleCurve;
    [SerializeField] private AnimationCurve damageScaleCurve;

    private EnemyHealth activeBoss;
    public event Action BossDied;

    private void Awake()
    {
        extractionZone = GetComponent<ExtractionZone>();
        if (extractionZone == null)
            Debug.LogError("BossSpawner: ExtractionZone component not found!");
        
        if (difficultyScaler == null) {
            Debug.LogError("BossSpawner: DifficultyScaler reference not set!");
            return;
        }

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
        
        EnemyHealth bossHealth = boss.GetComponent<EnemyHealth>();
        ScaleBossHealth(bossHealth);
        ScaleBossDamage(boss);

        this.activeBoss = boss.GetComponent<EnemyHealth>();
        this.activeBoss.EnemyDied += OnBossDied;
    }

    private void ScaleBossHealth(EnemyHealth bossHealth)
    {        
        if (bossHealth != null)
        {
            float difficulty = difficultyScaler.GetDifficultyScale();
            float multiplier = healthScaleCurve.Evaluate(difficulty);

            float oldHealth = bossHealth.GetMaxHealth();

            float newHealth = bossHealth.GetMaxHealth() * multiplier;
            bossHealth.InitializeHealth(newHealth);

            Debug.Log($"BossSpawner: Scaled Boss Health to {newHealth} from base {oldHealth} with difficulty {difficulty} and multiplier {multiplier}");
        }
    }

    private void ScaleBossDamage(GameObject enemyObj)
    {
        float difficulty = difficultyScaler.GetDifficultyScale();
        float multiplier = damageScaleCurve.Evaluate(difficulty);

        EnemyMelee melee = enemyObj.GetComponent<EnemyMelee>();
        if (melee != null)
        {
            float oldDmg = melee.GetBaseDamage();
            float newDmg = melee.GetBaseDamage() * multiplier;
            melee.InitializeSlamDamage(newDmg);
            return;
        }

        RevenantBossRange range = enemyObj.GetComponent<RevenantBossRange>();
        if (range != null)
        {
            range.InitializeAllDamage(multiplier);
            return;
        }

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

    private void Reset()
    {
        ResetScalingCurves();
    }

    [ContextMenu("Reset Scaling Curves")]
    private void ResetScalingCurves()
    {
        healthScaleCurve = AnimationCurve.Linear(0, 1, 30, 6);
        damageScaleCurve = AnimationCurve.Linear(0, 1, 30, 3);
    }
}

[Serializable]
public class BossType
{
    public GameObject prefab;
    public bool isFlying;    
}
