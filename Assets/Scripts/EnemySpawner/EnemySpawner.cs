using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private ExtractionZone extractionZone;
    [SerializeField]
    private List<EnemyType> enemies;
    [SerializeField]
    private Transform playerLocation;
    [SerializeField]
    private DifficultyScaler difficultyScaler;

    [Header("Settings")]
    [SerializeField]
    private float timeBetweenWaves = 8f;

    // This is now controlled by the Difficulty Scaler object.
    [Tooltip("If the DifficultyScaler object is set, this field is ignored!")]
    [SerializeField]
    private float difficultyScale = 1.03f;
    [SerializeField]
    private float spawnRadius = 10f;
    [SerializeField]
    private float minSpawnDist = 5f;
    [SerializeField]
    private bool isSpawning = true; // controlling with this

    private InputAction spawnToggleAction;


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

    [Header("Enemy Stat multiplier")]
    [SerializeField]
    private float healthGrowth = 0.07f;
    [SerializeField]
    private float damageGrowth = 0.05f;


    private int currentEnemyCount = 0;
    private float currentCredits;
    private int currentMaxEnemyCap;
    private float currentTimeBetweenEnemySpawns;
    private float enemySpawnCountdown = 0f;
    private int currentWave = 0;


    [SerializeField]
    private Queue<EnemyType> enemiesToSpawn = new Queue<EnemyType>();
    private List<(Vector3 position, bool isValid)> spawnDebugList = new List<(Vector3, bool)>();

    private bool isExtractionActive = false;
    private float waveCountdown;
    private bool isWaveInProgress = false;

    //Jared UIDEV Getters
    public event Action<bool> DevModeChanged;
    public event Action<int> CurrentEnemyCountChanged;
    public event Action<float> CurrentCreditsChanged;
    public event Action<int> CurrentMaxEnemyCapChanged;
    public event Action<int> CurrentWaveChanged;
    [SerializeField]
    private bool isDevModeEnabled = false;
public bool IsDevModeEnabled
{
    get => this.isDevModeEnabled;
    set
        {
            this.isDevModeEnabled = value;
            DevModeChanged?.Invoke(this.isDevModeEnabled);
    }
}

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.extractionZone.ExtractionInteracted += OnExtractionZoneStarted;
        this.extractionZone.ExtractionFinished += OnExtractionZoneFinished;

        this.waveCountdown = this.timeBetweenWaves;

        // Sort the list of enemies from cheapest to most expensive
        enemies.Sort((a, b) => { return a.cost - b.cost; });

        var input = new InputAction("Toggle Spawning", binding: "<Keyboard>/l");
        input.performed += _ => ToggleSpawning();
        input.Enable();

    }

    private void ToggleSpawning()
    {
        isSpawning = !isSpawning;// toggling between true and false
        Debug.Log("Spawning is now " + (isSpawning ? "enabled" : "disabled"));
    }

    // Update is called once per frame
    void Update()
    {
        if (this.isSpawning)
        {
            SpawnerUpdate();
        }
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
                Debug.Log("Spawned enemy");

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

        float waveCredits = this.baseWaveCredits * Mathf.Pow(GetDifficulty(), this.currentWave);
        int waveCap = Mathf.Min(Mathf.CeilToInt(this.startingEnemyCap + this.enemyCapGrowth * Mathf.Pow(GetDifficulty(), this.currentWave)), this.globalMaxEnemies);

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
        Vector3 location;
        bool validLocation;

        if (enemy.isFlying)
        {
            validLocation = TryGetAirLocation(enemy.prefab, out location);
        }
        else
        {
            validLocation = TryGetGroundLocation(enemy.prefab, out location);
        }

        if (!validLocation)
        {
            Debug.Log("Failed to find valid ground spawn location after multiple attempts.");
            spawnDebugList.Add((location, false));
            return;
        }

        spawnDebugList.Add((location, true));
        GameObject enemyObj = Instantiate(enemy.prefab, location, Quaternion.identity);

        ScaleEnemyHealth(enemyObj);
        ScaleEnemyDamage(enemyObj);

        this.currentEnemyCount++;

        // Notify UI for change
        CurrentEnemyCountChanged?.Invoke(this.currentEnemyCount);
    }

    private bool TryGetAirLocation(GameObject enemyPrefab, out Vector3 location)
    {
        float radius = GetEnemySpawnWidth(enemyPrefab);
        int maxAttempts = 2;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector3 potentialLocation = GetAirLocation();
            bool isFree = IsSpawnLocationFree(potentialLocation, radius);

            spawnDebugList.Add((potentialLocation, isFree));

            if (isFree)
            {
                location = potentialLocation;
                return true;
            }
        }

        location = Vector3.zero;
        return false;
    }

    private void ScaleEnemyHealth(GameObject enemyObj)
    {
        //     // 3 * (1 + (0.5) * (2 - 1)) = 4.5
        //     // 3 * (1 + (0.5) * (3 - 1)) = 6
        //     // 3 * (1 + (0,5) * (6 - 1)) = 10.5 rounded

        // 3 * (1 + (0.5) * (0))
        EnemyHealth enemyHealth = enemyObj.GetComponent<EnemyHealth>();
        float newHealthAfterMultiplier = enemyHealth.GetMaxHealth() * (1 + (this.healthGrowth - 1) * (currentWave - 1));
        // enemyHealth.InitializeHealth(Mathf.CeilToInt(newHealthAfterMultiplier));
        enemyHealth.InitializeHealth(newHealthAfterMultiplier);
        enemyHealth.EnemyDied += OnEnemyDied;
    }

    private void ScaleEnemyDamage(GameObject enemyObj)
    {   
        EnemyMelee enemyMelee = enemyObj.GetComponent<EnemyMelee>();

        if (enemyMelee != null)
        {
            float newDamage = enemyMelee.GetBaseDamage() * (1 + (this.damageGrowth - 1) * (currentWave - 1));
            enemyMelee?.InitializeSlamDamage(newDamage);
        }
        else
        {
            EnemyRange enemyRange = enemyObj.GetComponent<EnemyRange>();
            float newDamage = enemyRange.GetBaseDamage() * (1 + (this.damageGrowth - 1) * (currentWave - 1));
            enemyRange?.InitializeDamage(newDamage);
        }
    }

    private Vector3 GetGroundLocation()
    {
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

        // Random distance between minSpawnDist and spawnRadius
        float distance = UnityEngine.Random.Range(minSpawnDist, spawnRadius);

        Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance;
        Vector3 spawnLocation = playerLocation.position + offset;

        // Adjust Y using raycast
        float heightOffset = 5f;
        float raycastLength = 40f;
        if (Physics.Raycast(spawnLocation + Vector3.up * heightOffset, Vector3.down, out RaycastHit hitInfo, raycastLength))
        {
            spawnLocation.y = hitInfo.point.y;
        }

        return spawnLocation;
    }

    private Vector3 GetAirLocation()
    {
        // Random point on a unit sphere
        Vector3 locationOffset = UnityEngine.Random.onUnitSphere * spawnRadius;

        // Ensure it's above the player
        locationOffset.y = Mathf.Abs(locationOffset.y);

        // Maintain minimum horizontal distance
        Vector2 horizontalOffset = new Vector2(locationOffset.x, locationOffset.z);
        if (horizontalOffset.magnitude < minSpawnDist)
        {
            horizontalOffset = horizontalOffset.normalized * minSpawnDist;
            locationOffset.x = horizontalOffset.x;
            locationOffset.z = horizontalOffset.y;
        }

        return playerLocation.position + locationOffset;
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
        return NavMesh.SamplePosition(pos, out hit, 2f, NavMesh.AllAreas);
    }

    private void OnEnemyDied(EnemyHealth enemy)
    {
        this.currentEnemyCount = Math.Max(0, this.currentEnemyCount - 1);

        // Notify UI for change
        CurrentEnemyCountChanged?.Invoke(this.currentEnemyCount);
    }

    /// <summary>
    /// Returns the Difficulty Scale.
    /// Uses the DifficultyScaler object if it is set.
    /// Otherwise, use the built-in difficultyScale.
    /// </summary>
    /// <returns>The difficulty scale.</returns>
    private float GetDifficulty()
    {
        if (difficultyScaler)
        {
            return difficultyScaler.GetDifficultyScale();
        }

        return difficultyScale;
    }

    private void OnDrawGizmos()
    {
        if (playerLocation == null) return;

        // Draw max spawn distance (spawnRadius)
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // red, semi-transparent
        Gizmos.DrawWireSphere(playerLocation.position, spawnRadius);

        // Draw min spawn distance (minSpawnDist)
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // green, semi-transparent
        Gizmos.DrawWireSphere(playerLocation.position, minSpawnDist);

        foreach (var spawn in spawnDebugList)
        {
            Gizmos.color = spawn.isValid ? Color.green : Color.red;
            Gizmos.DrawSphere(spawn.position, 0.3f);
        }
    }

    private bool IsSpawnLocationFree(Vector3 position, float radius)
    {
        Collider[] colliders = Physics.OverlapSphere(position, radius);
        return colliders.Length == 0;
    }

    private bool TryGetGroundLocation(GameObject enemyPrefab, out Vector3 location)
    {
        float radius = GetEnemySpawnWidth(enemyPrefab);
        int maxAttempts = 2;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector3 potentialLocation = GetGroundLocation();

            bool isFree = IsSpawnPositionOnNavSurface(potentialLocation);

            spawnDebugList.Add((potentialLocation, isFree));
            if (isFree)
            {
                location = potentialLocation;
                return true;
            }
        }

        location = Vector3.zero;
        return false;
    }

    private float GetEnemySpawnWidth(GameObject enemyPrefab)
    {
        BoxCollider box = enemyPrefab.GetComponent<BoxCollider>();
        if (box == null)
            return 1f;

        Vector3 worldSize = Vector3.Scale(box.size, box.transform.lossyScale);

        float size = Mathf.Max(worldSize.x, worldSize.z) * 0.5f * 1.1f;
        Debug.Log("ENEMY SIZE: " + size);
        return size;
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