using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class EnemySpawner_2 : MonoBehaviour
{
    // =========================================================================
    //  STAT SCALER STRUCT
    //  A self-contained scaling definition for any numeric stat.
    //  Each instance lives in the Inspector as its own foldout group.
    //
    //  baseValue       → the starting value at wave 1 (no scaling applied yet)
    //  minIncreasePerWave → guaranteed flat increase added every wave BEFORE scaling.
    //                       Ensures no two waves ever feel identical.
    //  scaler          → the growth multiplier applied on top of the floor.
    //                       1.0  = no extra curve (pure flat floor only)
    //                       1.2  = 20% stronger per wave
    //                      <1.0  = stat shrinks over time (useful for testing)
    //  scalingMode     → how scaler is applied each wave:
    //                       Linear:      1 + (scaler - 1) * (wave - 1)   — steady, additive
    //                       Exponential: scaler ^ (wave - 1)             — compounds, accelerates
    //
    //  Formula:  floor = baseValue + (minIncreasePerWave * wave)
    //            result = floor * Calculate(wave)
    //
    //  NOTE: baseValue means different things per stat — read each stat's comment.
    // =========================================================================
    [Serializable]
    public struct StatScaler
    {
        public enum ScalingMode { Linear, SoftExponential, Exponential }

        [Tooltip("Starting value at wave 1, before any growth is applied.")]
        public float baseValue;

        [Tooltip("This amount is added to the base every wave, guaranteed, before the scaler curve is applied.")]
        public float minIncreasePerWave;

        [Tooltip("Growth multiplier per wave. 1.0 = no curve. 1.2 = 20% stronger per wave. <1 = weaker.")]
        public float scaler;

        [Tooltip("Linear grows additively each wave. Exponential compounds (accelerates) each wave.")]
        public ScalingMode scalingMode;

        [Tooltip("Only used by SoftExponential. Stretches the curve over more waves.\n1 = identical to Exponential.\n2 = takes twice as many waves to reach the same value.\nHigher = gentler ramp.")]
        public float softness;

        /// <summary>
        /// Returns the final computed value for the given wave number.
        ///
        /// Floor = baseValue + (minIncreasePerWave * (wave - 1))
        ///   Wave 1 always equals baseValue exactly — no increase yet.
        ///   Wave 2 onwards adds minIncreasePerWave each wave, guaranteed.
        ///
        /// Multiplier (Linear):         1 + (scaler - 1) * (wave - 1)
        ///   Grows additively. Easy to predict. Wave 1 = 1.0x always.
        ///   e.g. scaler=1.2 → Wave 1: 1.0x  Wave 3: 1.4x  Wave 5: 1.8x
        ///
        /// Multiplier (Exponential):    scaler ^ (wave - 1)
        ///   Compounds each wave. Creates a steep late-game spike.
        ///   e.g. scaler=1.2 → Wave 1: 1.0x  Wave 3: 1.44x  Wave 5: 2.07x
        ///
        /// Multiplier (SoftExponential): scaler ^ ((wave - 1) / softness)
        ///   Same as Exponential but the exponent is divided by softness,
        ///   stretching the curve over more waves. softness=1 = pure Exponential.
        ///   e.g. scaler=1.2, softness=2 → Wave 1: 1.0x  Wave 3: 1.2x  Wave 5: 1.44x
        ///
        /// Result = floor * multiplier * difficultyScale
        /// </summary>
        public float Calculate(int wave, float difficultyScale = 1f)
        {
            int w = Mathf.Max(wave - 1, 0); // Ensure wave 1 starts at baseValue with no multiplier
            float floor = baseValue + minIncreasePerWave * w;

            float multiplier;
            switch (scalingMode)
            {
                case ScalingMode.Linear:
                    multiplier = 1f + (scaler - 1f) * w;
                    break;
                case ScalingMode.Exponential:
                    multiplier = Mathf.Pow(scaler, w);
                    break;

                case ScalingMode.SoftExponential:
                    float s = Mathf.Max(softness, 0.01f); // Guard against divide-by-zero
                    multiplier = Mathf.Pow(scaler, w / s);
                    break;

                default:
                    multiplier = 1f;
                    break;
            }

            return floor * multiplier * difficultyScale;
        }
    }



    [Header("References")]
    [SerializeField] private List<EnemyType> enemies;
    [SerializeField] private Transform playerLocation;
    [SerializeField] private DifficultyScaler difficultyScaler;
    [SerializeField] private GameObject spawnVFXPrefab;
    [SerializeField] private AK.Wwise.Event spawnSFX;


    [Header("Node Settings")]
    [SerializeField] private LayerMask spawnNodeLayer;
    [SerializeField] private float spawnRadius = 40f;
    [SerializeField] private float minSpawnDist = 10f;
    [SerializeField] private float expandedSearchRadius = 100f; // For fallback spawning when no valid nodes are found within spawnRadius
    
    [Header("Optional Features")]
    [SerializeField] private bool isSpawning = true; 

    [Header("Wave Credits")]
    [SerializeField] private StatScaler credits = new StatScaler
    {
        baseValue = 10f, 
        minIncreasePerWave = 5f, 
        scaler = 1.1f,
        scalingMode = StatScaler.ScalingMode.Linear,
        softness = 2f
    };

    [Header("Enemy Cap")]
    [SerializeField] private int globalMaxEnemies = 200;
    [SerializeField] private StatScaler enemyCap = new StatScaler
    {
        baseValue = 10f, 
        minIncreasePerWave = 2f, 
        scaler = 1.05f,
        scalingMode = StatScaler.ScalingMode.Linear,
        softness = 2f
    };

    [Header("Health Scaling")]
    [Tooltip("baseValue should be 1.0. minIncreasePerWave typically 0. scaler > 1 = tankier enemies per wave.")]
    [SerializeField] private StatScaler health = new StatScaler
    {
        baseValue = 1f, 
        minIncreasePerWave = 0f, 
        scaler = 1.15f,
        scalingMode = StatScaler.ScalingMode.Exponential,
        softness = 2f
    };

    
    [Header("Damage Scaling")]
    [Tooltip("baseValue should be 1.0. minIncreasePerWave typically 0. scaler > 1 = more damage per wave.")]
    [SerializeField] private StatScaler damage = new StatScaler
    {
        baseValue = 1f,
        minIncreasePerWave = 0f, 
        scaler = 1.1f,
        scalingMode = StatScaler.ScalingMode.Exponential,
        softness = 2f
    };


    [Header("Wave Timing")]
    [SerializeField] private float timeBetweenWaves = 8f;
    [SerializeField] private float baseTimeBetweenEnemySpawns = 1f;



    [Header("Wave Pacing")]
    [Tooltip("If true, the between-wave countdown only begins once ALL enemies are dead. " +
             "If false, the countdown begins once living enemies drop to or below the straggler threshold.")]
    [SerializeField] private bool requireFullClear = false;

    [Tooltip("Only used when requireFullClear is false. " +
             "The countdown starts once currentEnemyCount reaches this value or below. " +
             "e.g. 3 = timer starts when 3 or fewer enemies remain.")]
    [SerializeField] private int stragglerThreshold = 3;

    [Tooltip("Safety net. If the wave has been waiting for a clear condition for longer than this " +
             "many seconds, it force-ends regardless. Prevents softlocks from stuck or unreachable enemies.")]
    [SerializeField] private float stragglerTimeout = 45f;


    [Tooltip("Fallback difficulty value if no DifficulterScaler is assigned.")]
    [SerializeField] private float fallbackDifficultyScale = 1.03f; 


    [Header("Extraction Modifiers")]
    [Tooltip("Credit multiplier when extraction is active. 2.0 = double enemy budget this wave.")]
    [SerializeField] private float extractionCreditMultiplier = 2f;

    [Tooltip("Enemy cap multiplier when extraction is active.")]
    [SerializeField] private float extractionEnemyCapMultiplier = 1.5f; 


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

    // =========================================================================
    //  INTERNAL STATE
    // =========================================================================

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

    // Hybrid pacing state.
    // True once the spawn queue empties. Waiting for the clear condition before
    // starting the between-wave countdown.
    private bool isWaitingForClear = false;

    // Tracks how long we've been in the waiting-for-clear state.
    // If it exceeds stragglerTimeout, the wave is force-ended to prevent softlocks.
    private float clearWaitTimer = 0f;

    private Collider[] nodeResults = new Collider[50];

    // =========================================================================
    //  EVENTS
    // =========================================================================

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
            return;
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
            return;
        }

        // Once the spawn queue is empty, wait until the clear condition is met (or timeout fires),
        // THEN start the between-wave countdown
        if (!this.isWaitingForClear)
        {
            this.isWaitingForClear = true;
            this.clearWaitTimer = 0f;
            if (showSpawnDebug)
                Debug.Log($"[Wave {currentWave}] Queue empty — waiting for clear condition.");
        }

        this.clearWaitTimer += Time.deltaTime;

        bool clearConditionMet = requireFullClear
            ? this.currentEnemyCount <= 0                        // Full clear: every enemy must be dead
            : this.currentEnemyCount <= this.stragglerThreshold; // Threshold: enough enemies are dead

        bool timedOut = this.clearWaitTimer >= this.stragglerTimeout;

        if (clearConditionMet || timedOut)
        {
            if (timedOut && !clearConditionMet && showSpawnDebug)
                Debug.LogWarning($"[Wave {currentWave}] Straggler timeout hit ({stragglerTimeout}s). " +
                                 $"{currentEnemyCount} enemies still alive. Force-ending wave.");
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
        float waveCredits = credits.Calculate(this.currentWave, diff);
        int waveCap = Mathf.Min(Mathf.CeilToInt(enemyCap.Calculate(this.currentWave, diff)), globalMaxEnemies);

        if (isExtractionActive)
        {
            waveCredits *= this.extractionCreditMultiplier;
            waveCap = Mathf.Min(Mathf.CeilToInt(waveCap * this.extractionEnemyCapMultiplier), globalMaxEnemies);
        }

        this.currentCredits = waveCredits;
        this.currentMaxEnemyCap = waveCap;
        this.currentTimeBetweenEnemySpawns = this.baseTimeBetweenEnemySpawns;

        CurrentCreditsChanged?.Invoke(this.currentCredits);
        CurrentMaxEnemyCapChanged?.Invoke(this.currentMaxEnemyCap);

        Debug.Log($"[Wave {currentWave}] Credits: {waveCredits:F1} | Cap: {waveCap} | Extraction: {isExtractionActive}");

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
            Debug.LogWarning($"Failed to find a NavMesh position");
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
            // Play the sound effect.
            PlaySpawnSFX(enemyObj);

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
        EnemyHealth enemyHealth = enemyObj.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            float newHealth = enemyHealth.GetMaxHealth() * health.Calculate(currentWave);
            enemyHealth.InitializeHealth(newHealth);
            enemyHealth.EnemyDied += OnEnemyDied;
        }
    }

    private void ScaleEnemyDamage(GameObject enemyObj)
    {
        float multiplier = damage.Calculate(currentWave);

        EnemyMelee melee = enemyObj.GetComponent<EnemyMelee>();
        if (melee != null)
        {
            float newDmg = melee.GetBaseDamage() * multiplier;
            melee.InitializeSlamDamage(newDmg);
            return;
        }

        EnemyRange range = enemyObj.GetComponent<EnemyRange>();
        if (range != null)
        {
            float newDmg = range.GetBaseDamage() * multiplier;
            range.InitializeDamage(newDmg);
        }

        EnemyGolem golem = enemyObj.GetComponent<EnemyGolem>();
        if (golem != null)
        {
            golem.InitializeAllDamage(multiplier);
            return;
        }
    }

    private void GenerateWave()
    {
        // Create a sub-list of what we can actually afford right now
        List<EnemyType> affordableEnemies = enemies.FindAll(
            e => e.cost <= this.currentCredits && e.minWave <= this.currentWave
        );

        while (affordableEnemies.Count > 0 && this.currentEnemyCount + enemiesToSpawn.Count < this.currentMaxEnemyCap)
        {
            EnemyType randomEnemy = PickWeightedRandom(affordableEnemies);
            
            this.currentCredits -= randomEnemy.cost;
            this.enemiesToSpawn.Enqueue(randomEnemy);

            // Refresh affordable list (in case we can no longer afford the expensive ones)
            affordableEnemies.RemoveAll(e => e.cost > this.currentCredits);
        }
    }

    /// <summary>
    /// Selects a random enemy from the pool using each enemy's spawnWeight as a probability.
    /// Higher weight = more likely to be chosen. Equal weights = uniform distribution.
    /// </summary>
    private EnemyType PickWeightedRandom(List<EnemyType> pool)
    {
        int totalWeight = 0;
        foreach (EnemyType e in pool) totalWeight += e.spawnWeight;

        int roll = UnityEngine.Random.Range(0, totalWeight);
        foreach (EnemyType e in pool)
        {
            roll -= e.spawnWeight;
            if (roll < 0) return e;
        }

        return pool[pool.Count - 1]; // Fallback — should never be reached
    }

    private void EndWave() 
    { 
        this.isWaveInProgress = false;
        this.isWaitingForClear = false;
        this.clearWaitTimer = 0f;
        this.waveCountdown = this.timeBetweenWaves;

        if (showSpawnDebug)
            Debug.Log($"[Wave {currentWave}] Ended. Next wave in {timeBetweenWaves}s.");
    }

    private float GetDifficulty() => difficultyScaler ? difficultyScaler.GetDifficultyScale() : fallbackDifficultyScale;
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

        GameObject vfx;
        if (ObjectPool.instance != null)
        {
            vfx = ObjectPool.instance.GetObject(spawnVFXPrefab, transform);
        }
        else
        {
            vfx = Instantiate(spawnVFXPrefab);
        }
        vfx.transform.position = position;
        vfx.transform.rotation = rotation;

        if (ObjectPool.instance != null)
            ObjectPool.instance.ReturnObject(vfx, 2.0f);
        else
            Destroy(vfx, 2.0f);
    }

    /// <summary>
    /// Tries to play the spawn sound effect if it's set.
    /// </summary>
    /// <param name="enemy">The enemy to play the sound at.</param>
    private void PlaySpawnSFX(GameObject enemy)
    {
        if (spawnSFX != null)
            if(spawnSFX.IsValid())
                spawnSFX.Post(enemy);
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