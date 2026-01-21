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

    [Header("Node Settings")]
    [SerializeField] private LayerMask spawnNodeLayer;
    [SerializeField] private float spawnRadius = 40f;
    [SerializeField] private float minSpawnDist = 10f;
    
    [Header("Optional Features")]
    [SerializeField] private bool isSpawning = true; 
    [Tooltip("If true, enemies will try to spawn where the player cannot see them.")]
    [SerializeField] private bool useLineOfSightCheck = true;
    [SerializeField] private LayerMask visionObstacleMask;

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
    private List<SpawnDebugInfo> recentSpawns = new List<SpawnDebugInfo>();

    private struct SpawnDebugInfo
    {
        public Vector3 position;
        public bool isSuccess; 
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
                SpawnEnemyFromNode(this.enemiesToSpawn.Dequeue());
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

    private void SpawnEnemyFromNode(EnemyType enemy)
    {
        // 1. SEARCH: Find all objects on the spawn node layer within the spawnRadius
        int nodesFound = Physics.OverlapSphereNonAlloc(playerLocation.position, spawnRadius, nodeResults, spawnNodeLayer);
        List<SpawnNode> candidates = new List<SpawnNode>();

        for (int i = 0; i < nodesFound; i++)
        {
            // 2. EDGE DETECTION: Get distance to center, then subtract radius to find the nearest edge
            if (nodeResults[i].TryGetComponent(out SpawnNode node))
            {
                float dist = Vector3.Distance(playerLocation.position, node.transform.position);

                float nodeRadius = 0f;
                if (nodeResults[i] is SphereCollider sphere) {
                    nodeRadius = sphere.radius * node.transform.lossyScale.x;
                }
                float distanceToNearEdge = dist - nodeRadius;

                // 3. FILTERING: Match enemy type (Flying/Ground) to node settings
                bool correctType = enemy.isFlying ? node.isForFlyingEnemies : node.isForGroundEnemies;

                // 4. VALIDATION: Ensure edge is far enough away and not visible (if LOS enabled)
                if (distanceToNearEdge >= minSpawnDist && correctType)
                {
                    if (useLineOfSightCheck && IsNodeVisible(node))
                        continue; 

                    candidates.Add(node);
                }
            }
        }

        if (candidates.Count > 0)
        {
            // 5. SELECTION: Pick one valid node from the list of candidates
            SpawnNode selectedNode = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            
            float radius = 1f; 
            if (selectedNode.TryGetComponent(out SphereCollider sphere))
            {
                radius = sphere.radius * selectedNode.transform.lossyScale.x;
            }

            // 6. POSITIONING: Pick a random X/Z point in a circle. Y stays at 0 (flat offset)
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * radius;
            Vector3 randomOffset = new Vector3(randomCircle.x, 0, randomCircle.y);

            // Final position starts at the Node's altitude (Y)
            Vector3 spawnPos = selectedNode.transform.position + randomOffset;

            // 7. NAVMESH SNAP: Search a sphere (radius + 2) for the closest walkable surface.
            // If found (like the ground beneath a sky node), Y is updated to the ground height.
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, radius + 2f, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
            }

            // 8. SPAWN: Instantiate at the final calculated position
            GameObject enemyObj = Instantiate(enemy.prefab, spawnPos, Quaternion.identity);

            if (showSpawnDebug)
            {
                recentSpawns.Add(new SpawnDebugInfo { position = spawnPos, isSuccess = true });
            }

            HandleEnemySpawned(enemyObj);
        }
        else
        {
            // 9. FALLBACK: If no valid nodes exist, spawn at a random node ignoring distance/LOS rules
            if (showSpawnDebug)
            {
                recentSpawns.Add(new SpawnDebugInfo { position = playerLocation.position, isSuccess = false });
            }

            if (useLineOfSightCheck)
            {
                SpawnFromAnyValidNode(enemy);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showSpawnDebug || !isDevModeEnabled) return;

        foreach (var spawn in recentSpawns)
        {
            if (spawn.isSuccess)
            {
                Gizmos.color = new Color(0.6f, 0.0f, 0.0f, 1.0f); 
                Gizmos.DrawSphere(spawn.position, 2.0f);
                Gizmos.color = Color.black;
                Gizmos.DrawLine(spawn.position, spawn.position + Vector3.up * 10f);
            }
            else
            {
                Gizmos.color = new Color(0.3f, 0.0f, 0.3f, 1.0f);
                Gizmos.DrawSphere(spawn.position, 1.5f);
            }
        }
    }

    private bool IsNodeVisible(SpawnNode node)
    {
        Vector3 direction = node.transform.position - playerLocation.position;
        return !Physics.Raycast(playerLocation.position + Vector3.up, direction, direction.magnitude, visionObstacleMask);
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

    private void SpawnFromAnyValidNode(EnemyType enemy)
    {
        int nodesFound = Physics.OverlapSphereNonAlloc(playerLocation.position, spawnRadius, nodeResults, spawnNodeLayer);
        if (nodesFound > 0)
        {
            SpawnNode node = nodeResults[UnityEngine.Random.Range(0, nodesFound)].GetComponent<SpawnNode>();
            GameObject obj = Instantiate(enemy.prefab, node.transform.position, Quaternion.identity);
            HandleEnemySpawned(obj);
        }
    }
}