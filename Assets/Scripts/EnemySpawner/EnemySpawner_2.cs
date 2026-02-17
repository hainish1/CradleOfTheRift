using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class EnemySpawner_2 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private List<EnemyType> enemies;
    [SerializeField] private Transform playerLocation;
    [SerializeField] private DifficultyScaler difficultyScaler;
    [SerializeField] private GameObject spawnVFXPrefab;


    [Header("Node Settings")]
    [SerializeField] private LayerMask spawnNodeLayer;
    [SerializeField] private float spawnRadius = 40f;
    [SerializeField] private float minSpawnDist = 10f;
    [SerializeField] private float expandedSearchRadius = 100f; // For fallback spawning when no valid nodes are found within spawnRadius
    
    [Header("Optional Features")]
    [SerializeField] private bool isSpawning = true; 

    [Header("Wave Settings")]
    [SerializeField] private float timeBetweenWaves = 8f;
    [Tooltip("If the DifficultyScaler object is set, this field is ignored!")]
    [SerializeField] private float difficultyScale = 1.03f; 
    [SerializeField] private float baseWaveCredits = 10f;
    [SerializeField] private int startingEnemyCap = 10;
    [SerializeField] private float enemyCapGrowth = 10f;
    [SerializeField] private float baseTimeBetweenEnemySpawns = 1f;
    [SerializeField] private int globalMaxEnemies = 200;

    [Header("Extraction Wave Settings")]
    [SerializeField] private float extractionCreditMultiplier = 2f;
    [SerializeField] private float extractionEnemyCapMultiplier = 1.5f; 

    [Header("Enemy Stat Multipliers")]
    [SerializeField] private float healthGrowth = 0.07f;
    [SerializeField] private float damageGrowth = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool showSpawnDebug = true;
    [SerializeField] private bool isDevModeEnabled = false; 
    [SerializeField] private int maxDebugHistory = 50; // Keep only the last 50 spawns
    private List<SpawnDebugInfo> recentSpawns = new List<SpawnDebugInfo>();

    private struct SpawnDebugInfo
    {
        public Vector3 position;          // The final snapped position
        public Vector3 attemptedPosition; // The initial "floating" position
        public float searchRadius;        // The NavMesh.SamplePosition call
        public Vector3 originPosition;    // Player location at time of spawn
        public bool isSuccess; 
        public bool isFallback;
    }

    // Internal State
    private int currentEnemyCount = 0;
    private float currentCredits;
    private int currentMaxEnemyCap;
    private float currentTimeBetweenEnemySpawns;
    private float enemySpawnCountdown = 0f;
    private int currentWave = 0;
    private bool isExtractionActive = false;
    private float waveCountdown;
    private bool isWaveInProgress = false;
    private Queue<EnemyType> enemiesToSpawn = new Queue<EnemyType>();
    
    private Collider[] nodeResults = new Collider[50];

    // Events
    public event Action<bool> DevModeChanged;
    public event Action<int> CurrentEnemyCountChanged;
    public event Action<float> CurrentCreditsChanged;
    public event Action<int> CurrentMaxEnemyCapChanged;
    public event Action<int> CurrentWaveChanged;

    public bool IsDevModeEnabled
    {
        get => this.isDevModeEnabled;
        set
        {
            this.isDevModeEnabled = value;
            DevModeChanged?.Invoke(this.isDevModeEnabled);
        }
    }

    void Start()
    {
        EnsurePlayerLocation();
        if (ExtractionManager.Instance != null)
        {
            ExtractionManager.Instance.ExtractionStarted += OnExtractionZoneStarted;
            ExtractionManager.Instance.AllExtractionsFinished += OnExtractionZoneFinished;
        }
        
        this.waveCountdown = this.timeBetweenWaves;

        // Sort the list of enemies from cheapest to most expensive
        enemies.Sort((a, b) => a.cost.CompareTo(b.cost));

        var input = new InputAction("Toggle Spawning", binding: "<Keyboard>/l");
        input.performed += _ => ToggleSpawning();
        input.Enable();
    }

    private void OnDisable()
    {
        if (ExtractionManager.Instance != null)
        {
            ExtractionManager.Instance.ExtractionStarted -= OnExtractionZoneStarted;
            ExtractionManager.Instance.AllExtractionsFinished -= OnExtractionZoneFinished;
        }
    }

    private void ToggleSpawning()
    {
        isSpawning = !isSpawning;// toggling between true and false
        Debug.Log("Spawning is now " + (isSpawning ? "enabled" : "disabled"));
    }

    void Update()
    {
        if (this.isSpawning) {
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
                EnemyType enemy = this.enemiesToSpawn.Dequeue();
                SpawnEnemyProcess(enemy);
                
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
        this.enemySpawnCountdown = 0f; // First enemy spawns immediately
        this.currentWave++;
        CurrentWaveChanged?.Invoke(this.currentWave);

        float diff = GetDifficulty();
        float waveCredits = this.baseWaveCredits * Mathf.Pow(diff, this.currentWave);
        int waveCap = Mathf.Min(Mathf.CeilToInt(this.startingEnemyCap + this.enemyCapGrowth * Mathf.Pow(diff, this.currentWave)), this.globalMaxEnemies);

        if (isExtractionActive)
        {
            waveCredits *= this.extractionCreditMultiplier;
            waveCap = Mathf.Min(Mathf.CeilToInt(waveCap * this.extractionEnemyCapMultiplier), this.globalMaxEnemies);
        }

        this.currentCredits = waveCredits;
        this.currentMaxEnemyCap = waveCap;
        this.currentTimeBetweenEnemySpawns = this.baseTimeBetweenEnemySpawns;

        CurrentCreditsChanged?.Invoke(this.currentCredits);
        CurrentMaxEnemyCapChanged?.Invoke(this.currentMaxEnemyCap);
        GenerateWave();
    }

    private void SpawnEnemyProcess(EnemyType enemy)
    {
        // Try the standard radius
        if (TryExecuteSpawn(enemy, spawnRadius)) return;

        // Try the expanded radius
        if (TryExecuteSpawn(enemy, expandedSearchRadius))
        {
            if (showSpawnDebug) Debug.Log("Standard nodes blocked; used expanded radius.");
            return;
        }

        // Nothing found in either
        if (showSpawnDebug)
        {
            RecordDebugSpawn(playerLocation.position, playerLocation.position, -1, false, false);
        }
    }

    private bool TryExecuteSpawn(EnemyType enemy, float currentSearchRadius)
    {
        // Find all objects on the spawn node layer within the spawnRadius
        int nodesFound = Physics.OverlapSphereNonAlloc(playerLocation.position, currentSearchRadius, nodeResults, spawnNodeLayer);
        List<SpawnNode> candidates = new List<SpawnNode>();

        for (int i = 0; i < nodesFound; i++)
        {
            // Get distance to center, then subtract radius to find the nearest edge
            if (nodeResults[i].TryGetComponent(out SpawnNode node))
            {
                float dist = Vector3.Distance(playerLocation.position, node.transform.position);
                
                // Get node radius for edge calculation
                float nodeRadius = 0f;
                if (nodeResults[i] is SphereCollider sphere) {
                    nodeRadius = sphere.radius * Mathf.Abs(node.transform.lossyScale.x);
                }                
                float distanceToNearEdge = dist - nodeRadius;

                // Match enemy type (Flying/Ground) to node settings
                bool correctType = enemy.isFlying ? node.isForFlyingEnemies : node.isForGroundEnemies;

                // Must be outside min distance and correct type
                if (distanceToNearEdge >= minSpawnDist && correctType)
                {
                    candidates.Add(node);
                }
            }
        }

        if (candidates.Count > 0)
        {
            // Pick one valid node from the list of candidates
            SpawnNode selectedNode = candidates[UnityEngine.Random.Range(0, candidates.Count)];

            bool isFallback = currentSearchRadius > spawnRadius;
            ExecuteActualSpawn(selectedNode, enemy, isFallback);
            return true;
        }

        return false;
    }

    private void ExecuteActualSpawn(SpawnNode selectedNode, EnemyType enemy, bool isFallback)
    {
        // Calculate the actual spawn point within the node's radius
        float radius = 1f; 
        if (selectedNode.TryGetComponent(out SphereCollider sphere))
        {
            float maxScale = Mathf.Max(
                Mathf.Abs(selectedNode.transform.lossyScale.x), 
                Mathf.Abs(selectedNode.transform.lossyScale.z)
            );
            radius = sphere.radius * maxScale;
        }

        // Pick a random X/Z point within the circle
        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * radius;
        Vector3 randomOffset = new Vector3(randomCircle.x, 0, randomCircle.y);

        // Final position starts at the Node's altitude (Y)
        Vector3 initialSpawnPos = selectedNode.transform.position + randomOffset;
        Vector3 finalSpawnPos = initialSpawnPos;

        // Snap the position to the ground using the NavMesh
        float searchRadius = radius + 2f;
        if (NavMesh.SamplePosition(initialSpawnPos, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
        {
            finalSpawnPos = hit.position;

            // Play visual effects and instantiate the enemy
            PlaySpawnVFX(finalSpawnPos, Quaternion.identity);
            GameObject enemyObj = Instantiate(enemy.prefab, finalSpawnPos, Quaternion.identity);

            // Log the spawn for Gizmo debugging
            if (showSpawnDebug)
            {
                RecordDebugSpawn(finalSpawnPos, initialSpawnPos, searchRadius, true, isFallback);
            }

            HandleEnemySpawned(enemyObj);
        }
        else
        {
            // Log the failed spawn attempt for Gizmo debugging
            if (showSpawnDebug)
            {
                Debug.LogWarning($"Failed to find NavMesh position for spawn at {initialSpawnPos} with search radius {searchRadius}");
                RecordDebugSpawn(initialSpawnPos, initialSpawnPos, searchRadius, false, isFallback);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (playerLocation == null) return;

        // Draw the Minimum Spawn Distance 
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerLocation.position, minSpawnDist);

        // Draw the Maximum Spawn Radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(playerLocation.position, spawnRadius);

        if (!showSpawnDebug || !isDevModeEnabled) return;

        foreach (var spawn in recentSpawns)
        {
            // Draw the search sphere using a faint gray/yellow
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.7f);
            Gizmos.DrawWireSphere(spawn.attemptedPosition, spawn.searchRadius);
            if (spawn.isSuccess)
            {
                // Draw the "Start" point (where the system attempted to spawn before snapping to the NavMesh)
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(spawn.attemptedPosition, 0.2f);

                // Draw a line to the "Snapped" point
                Gizmos.color = Color.white;
                Gizmos.DrawLine(spawn.attemptedPosition, spawn.position);

                // Draw the final spawn point
                Color debugColor = spawn.isFallback ? Color.cyan : Color.red; 
                Gizmos.color = debugColor;
                Gizmos.DrawSphere(spawn.position, 0.5f);
            }
            else
            {
                // Purple for failure
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(spawn.attemptedPosition, 0.5f);
                Gizmos.DrawLine(spawn.attemptedPosition, spawn.attemptedPosition + Vector3.up * 2f);
            }
        }
    }

    private void HandleEnemySpawned(GameObject enemyObj)
    {
        ScaleEnemyHealth(enemyObj);
        ScaleEnemyDamage(enemyObj);
        this.currentEnemyCount++;
        CurrentEnemyCountChanged?.Invoke(this.currentEnemyCount);
    }

    private void ScaleEnemyHealth(GameObject enemyObj)
    {
        //     // 3 * (1 + (0.5) * (2 - 1)) = 4.5
        //     // 3 * (1 + (0.5) * (3 - 1)) = 6
        //     // 3 * (1 + (0,5) * (6 - 1)) = 10.5 rounded

        // 3 * (1 + (0.5) * (0))
        EnemyHealth enemyHealth = enemyObj.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            float newHealth = enemyHealth.GetMaxHealth() * (1 + (this.healthGrowth - 1) * (currentWave - 1));
            enemyHealth.InitializeHealth(newHealth);
            enemyHealth.EnemyDied += OnEnemyDied;
        }
    }

    private void ScaleEnemyDamage(GameObject enemyObj)
    {
        EnemyMelee melee = enemyObj.GetComponent<EnemyMelee>();
        if (melee != null)
        {
            float newDmg = melee.GetBaseDamage() * (1 + (this.damageGrowth - 1) * (currentWave - 1));
            melee.InitializeSlamDamage(newDmg);
            return;
        }

        EnemyRange range = enemyObj.GetComponent<EnemyRange>();
        if (range != null)
        {
            float newDmg = range.GetBaseDamage() * (1 + (this.damageGrowth - 1) * (currentWave - 1));
            range.InitializeDamage(newDmg);
        }
    }

    private void GenerateWave()
    {
        int lowestCost = this.enemies[0].cost;
        while (this.currentCredits >= lowestCost && this.currentEnemyCount + enemiesToSpawn.Count < this.currentMaxEnemyCap)
        {
            EnemyType randomEnemy = enemies[UnityEngine.Random.Range(0, enemies.Count)];
            if (this.currentCredits >= randomEnemy.cost)
            {
                this.currentCredits -= randomEnemy.cost;
                this.enemiesToSpawn.Enqueue(randomEnemy);
            }
        }
    }

    private void EndWave() 
    { 
        this.isWaveInProgress = false; 
        this.waveCountdown = this.timeBetweenWaves; 
    }

    private float GetDifficulty() => difficultyScaler ? difficultyScaler.GetDifficultyScale() : difficultyScale;
    private void OnExtractionZoneStarted(ExtractionZone zone) => this.isExtractionActive = true;
    private void OnExtractionZoneFinished() => this.isExtractionActive = false;
    private void OnEnemyDied(EnemyHealth enemy) 
    {
        this.currentEnemyCount = Math.Max(0, this.currentEnemyCount - 1); 
        CurrentEnemyCountChanged?.Invoke(this.currentEnemyCount); 
    }

    private void PlaySpawnVFX(UnityEngine.Vector3 position, UnityEngine.Quaternion rotation)
    {
        if (spawnVFXPrefab == null) return;
        GameObject vfx = Instantiate(spawnVFXPrefab, position, rotation);

        Destroy(vfx, 2.0f); // Should prob make the VFX auto destroy instead of doing it here.
    }

    private void EnsurePlayerLocation()
    {
        if (playerLocation != null) return;

        var playerGo = PlayerLocator.FindPlayerGameObject();
        if (playerGo != null)
            playerLocation = playerGo.transform;
    }

    private void RecordDebugSpawn(Vector3 finalPos, Vector3 attemptedPos, float searchRadius, bool success, bool fallback)
    {
        recentSpawns.Add(new SpawnDebugInfo 
        { 
            position = finalPos,
            attemptedPosition = attemptedPos, 
            searchRadius = searchRadius,
            originPosition = playerLocation.position,
            isSuccess = success, 
            isFallback = fallback 
        });

        if (recentSpawns.Count > maxDebugHistory)
        {
            recentSpawns.RemoveAt(0);
        }
    }
}