using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    private ExtractionZone extractionZone;
    [SerializeField]
    private List<EnemyType> enemies;
    [SerializeField]
    private Transform playerLocation;


    [SerializeField]
    private float timeBetweenWaves = 8f;

    [SerializeField]
    private float difficultyScale = 1.03f;
    [SerializeField]
    private float spawnRadius = 10f;


    [Header("Normal Waves Settings")]
    [SerializeField]
    private float baseWaveCredits = 10f;
    [SerializeField]
    private int startingEnemyCap = 10;
    [SerializeField]
    private float enemyCapGrowth = 10f;
    [SerializeField]
    private float baseTimeBetweenEnemySpawns = 1f;
    [SerializeField]
    private int globalMaxEnemies = 200;



    [Header("Extraction Wave Settings")]
    [SerializeField]
    private float extractionCreditMultiplier = 2f;
    [SerializeField]
    private float extractionEnemyCapMultiplier = 1.5f;


    private int currentEnemyCount = 0;
    private float currentCredits;
    private int currentMaxEnemyCap;
    private float currentTimeBetweenEnemySpawns;
    private float enemySpawnCountdown = 0f;
    private int currentWave = 0;


    [SerializeField]
    private Queue<EnemyType> enemiesToSpawn = new Queue<EnemyType>();

    private bool isExtractionActive = false;
    private float waveCountdown;
    private bool isWaveInProgress = false;

    //Jared UIDEV Getters

    public event Action<int> CurrentEnemyCountChanged;
    public event Action<float> CurrentCreditsChanged;
    public event Action<int> CurrentMaxEnemyCapChanged;
    public event Action<int> CurrentWaveChanged;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.extractionZone.ExtractionInteracted += OnExtractionZoneStarted;
        this.extractionZone.ExtractionFinished += OnExtractionZoneFinished;

        this.waveCountdown = this.timeBetweenWaves;

        // Sort the list of enemies from cheapest to most expensive
        enemies.Sort((a, b) => { return a.cost - b.cost; });

    }

    // Update is called once per frame
    void Update()
    {
        SpawnerUpdate();
    }

    private void SpawnerUpdate()
    {
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
        // Notify UI for change
        CurrentWaveChanged?.Invoke(this.currentWave);

        float waveCredits = this.baseWaveCredits * Mathf.Pow(this.difficultyScale, this.currentWave);
        int waveCap = Mathf.Min(Mathf.CeilToInt(this.startingEnemyCap + this.enemyCapGrowth * Mathf.Pow(difficultyScale, this.currentWave)), this.globalMaxEnemies);

        if (this.isExtractionActive)
        {
            waveCredits *= this.extractionCreditMultiplier;
            waveCap = Mathf.Min(Mathf.CeilToInt(waveCap * this.extractionEnemyCapMultiplier), this.globalMaxEnemies);
            this.currentTimeBetweenEnemySpawns = this.baseTimeBetweenEnemySpawns;
        }
        else
        {
            this.currentTimeBetweenEnemySpawns = this.baseTimeBetweenEnemySpawns;
        }

        this.currentCredits = waveCredits;
        this.currentMaxEnemyCap = waveCap;
        
        // Notify UI for change
        CurrentCreditsChanged?.Invoke(this.currentCredits);
        CurrentMaxEnemyCapChanged?.Invoke(this.currentMaxEnemyCap);

        GenerateWave();
    }

    private void EndWave()
    {
        this.isWaveInProgress = false;
        this.waveCountdown = this.timeBetweenWaves;
    }

    private void GenerateWave()
    {
        int lowestCostEnemy = this.enemies[0].cost;
        int enemiesToSpawnCount = 0;
        while (this.currentCredits >= lowestCostEnemy && this.currentEnemyCount + enemiesToSpawnCount < this.currentMaxEnemyCap)
        {
            EnemyType randomEnemy = enemies[UnityEngine.Random.Range(0, enemies.Count)];

            if (this.currentCredits >= randomEnemy.cost)
            {
                this.currentCredits -= randomEnemy.cost;
                this.enemiesToSpawn.Enqueue(randomEnemy);
                enemiesToSpawnCount++;
            }
        }
    }

    private void SpawnEnemy(EnemyType enemy)
    {
        Vector3 location = enemy.isFlying ? GetAirLocation() : GetGroundLocation();

        // safety check here
        if (!IsSpawnPositionOnNavSurface(location))
        {
            return;
        }

        GameObject enemyObj = Instantiate(enemy.prefab, location, Quaternion.identity);
        EnemyHealth enemyComponent = enemyObj.GetComponent<EnemyHealth>();
        enemyComponent.EnemyDied += OnEnemyDied;

        this.currentEnemyCount++;

        // Notify UI for change
        CurrentEnemyCountChanged.Invoke(this.currentEnemyCount);
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
    }

    private void OnExtractionZoneFinished()
    {
        this.isExtractionActive = false;
    }

    private bool IsSpawnPositionOnNavSurface(Vector3 pos)
    {
        NavMeshHit hit;
        return NavMesh.SamplePosition(pos, out hit, spawnRadius, NavMesh.AllAreas);
    }

    private void OnEnemyDied(EnemyHealth enemy)
    {
        this.currentEnemyCount = Math.Max(0, this.currentEnemyCount - 1);
        
        // Notify UI for change
        CurrentEnemyCountChanged.Invoke(this.currentEnemyCount);
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