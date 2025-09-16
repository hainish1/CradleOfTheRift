using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    private ExtractionZone extractionZone;
    [SerializeField]
    private List<EnemyType> enemies;
    [SerializeField]
    private Transform playerLocation;


    [SerializeField]
    private int EnemyWaveCapIncrease = 2;
    [SerializeField]
    private float timeBetweenWaves = 8f;
    private int currentEnemyCount = 0;
    [SerializeField]
    private float currentCredits = 5f;


    [Header("Normal Waves Settings")]
    [SerializeField]
    private float baseCreditGainRate = 1f;
    [SerializeField]
    private int baseMaxEnemiesPerWave = 5;
    [SerializeField]
    private int baseMaxEnemyCap = 10;
    [SerializeField]
    private float baseTimeBetweenEnemySpawns = 1f;
    [SerializeField]
    private float baseMaxCredits = 20f;


    [Header("Extraction Wave Settings")]
    [SerializeField]
    private float extractionCreditGainRateMultiplier = 2f;
    [SerializeField]
    private float extractionZoneMaxCreditMultiplier = 1.5f;
    [SerializeField]
    private float extractionMaxEnemyMultiplier = 1.5f;



    private float currentCreditGainRate;
    private float currentMaxCredits;
    private int currentMaxEnemiesPerWave;
    private int currentMaxEnemyCap;
    private float currentTimeBetweenEnemySpawns;
    private float enemySpawnCountdown = 0f;
    private int currentWave = 0;


    [SerializeField]
    private float difficultyScale = 1.01f;
    [SerializeField]
    private Queue<EnemyType> enemiesToSpawn = new Queue<EnemyType>();


    private float spawnRadius = 10f;
    private bool isExtractionActive = false;
    private float waveCountdown;
    private bool isWaveInProgress = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.extractionZone.ExtractionInteracted += OnExtractionZoneStarted;
        this.extractionZone.ExtractionFinished += OnExtractionZoneFinished;

        this.waveCountdown = this.timeBetweenWaves;

        // Sort the list of enemies from cheapest to most expensive
        enemies.Sort((a, b) => { return a.cost - b.cost; });

        SetSpawningParametersRegular();
    }

    // Update is called once per frame
    void Update()
    {
        SpawnerUpdate();
    }

    private void SpawnerUpdate()
    {
        this.currentCredits += this.currentCreditGainRate * Time.deltaTime;
        this.currentCredits = Mathf.Min(this.currentCredits, this.currentMaxCredits);

        // Start a new wave after the countdown has finished
        if (!this.isWaveInProgress)
        {
            this.waveCountdown -= Time.deltaTime;

            if (this.waveCountdown <= 0f)
            {
                StartWave();
            }
        }

        // Spawn enemies until queue is empty
        if (this.enemiesToSpawn.Count > 0)
        {
            this.enemySpawnCountdown -= Time.deltaTime;
            if (this.enemySpawnCountdown <= 0f)
            {
                SpawnEnemy(this.enemiesToSpawn.Dequeue());
                this.enemySpawnCountdown = this.currentTimeBetweenEnemySpawns;
            }
        }

        // Reset properties when finishing a wave
        else if (this.isWaveInProgress)
        {
            EndWave();
        }
    }

    private void StartWave()
    {
        this.isWaveInProgress = true;

        // Have the first enemy spawn immediately
        this.enemySpawnCountdown = 0f;
        this.currentWave++;
        GenerateWave();
    }

    private void EndWave()
    {
        this.isWaveInProgress = false;
        this.waveCountdown = this.timeBetweenWaves;

        this.baseCreditGainRate *= this.difficultyScale;
        this.baseMaxCredits *= this.difficultyScale;

        this.baseMaxEnemiesPerWave += this.EnemyWaveCapIncrease;
        this.baseMaxEnemiesPerWave = Mathf.CeilToInt(this.baseMaxEnemiesPerWave * difficultyScale);
        this.baseMaxEnemyCap = Mathf.Min((int)(this.baseMaxEnemyCap * this.difficultyScale + this.EnemyWaveCapIncrease), 200);

        if (this.isExtractionActive)
        {
            SetSpawningParametersExtraction();
        }
        else
        {
            SetSpawningParametersRegular();
        }

        Debug.Log("CAP: " +  this.currentMaxEnemyCap + " NUM ENEMEI: " + this.currentEnemyCount);
    }

    private void GenerateWave()
    {
        int lowestCostEnemy = this.enemies[0].cost;
        int spawnedEnemies = 0;
        while (this.currentCredits >= lowestCostEnemy && spawnedEnemies < this.currentMaxEnemiesPerWave && this.currentEnemyCount < this.currentMaxEnemyCap)
        {
            EnemyType randomEnemy = enemies[UnityEngine.Random.Range(0, enemies.Count)];

            if (this.currentCredits >= randomEnemy.cost)
            {
                this.currentCredits -= randomEnemy.cost;
                this.enemiesToSpawn.Enqueue(randomEnemy);
                spawnedEnemies++;
                this.currentEnemyCount++;
            }
        }
    }

    private void SpawnEnemy(EnemyType enemy)
    {
        Vector3 location = enemy.isFlying ? GetAirLocation() : GetGroundLocation();

        Instantiate(enemy.prefab, location, Quaternion.identity);
    }

    private Vector3 GetGroundLocation()
    {
        Vector2 locationOffset = UnityEngine.Random.insideUnitCircle * this.spawnRadius;
        Vector3 spawnLocation = this.playerLocation.position + new Vector3(locationOffset.x, 0, locationOffset.y);

        float heightOffset = 5f;
        float raycastLength = 40f;

        // Shoot a raycast down to determine the grounds Y position
        if (Physics.Raycast(spawnLocation + Vector3.up * heightOffset, Vector3.down, out RaycastHit hitInfo, raycastLength))
        {
            spawnLocation.y = hitInfo.point.y;
        }

        return spawnLocation;
    }

    private Vector3 GetAirLocation()
    {
        Vector3 locationOffset = UnityEngine.Random.onUnitSphere * this.spawnRadius;
        locationOffset.y = Math.Abs(locationOffset.y);

        return this.playerLocation.position + locationOffset;
    }

    private void OnExtractionZoneStarted()
    {
        this.isExtractionActive = true;
        SetSpawningParametersExtraction();
    }

    private void OnExtractionZoneFinished()
    {
        this.isExtractionActive = false;
        // this.isExtractionZoneDone = true;
        SetSpawningParametersRegular();
    }

    private void SetSpawningParametersRegular()
    {
        this.currentCreditGainRate = this.baseCreditGainRate;
        this.currentMaxCredits = this.baseMaxCredits;
        this.currentMaxEnemiesPerWave = this.baseMaxEnemiesPerWave;
        this.currentMaxEnemyCap = this.baseMaxEnemyCap;
        this.currentTimeBetweenEnemySpawns = this.baseTimeBetweenEnemySpawns;
    }

    private void SetSpawningParametersExtraction()
    {
        this.currentCreditGainRate = this.baseCreditGainRate * this.extractionCreditGainRateMultiplier;
        this.currentMaxCredits = this.baseMaxCredits * this.extractionZoneMaxCreditMultiplier;

        this.currentMaxEnemiesPerWave = Mathf.CeilToInt(this.baseMaxEnemiesPerWave* extractionMaxEnemyMultiplier);
        this.currentMaxEnemyCap = Mathf.CeilToInt(this.baseMaxEnemyCap * this.extractionMaxEnemyMultiplier);

        this.currentTimeBetweenEnemySpawns = this.baseTimeBetweenEnemySpawns;
    }
}

[Serializable]
public class EnemyType
{
    public string name;
    public GameObject prefab;
    public int cost;
    public bool isFlying;    
}