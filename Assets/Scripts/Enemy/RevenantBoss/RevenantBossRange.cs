using System.Collections;
using UnityEngine;

/// <summary>
/// Class - Represents a ranged enemy boss, inherits from Base Enemy class and Ranged Enemy class.
/// Two main attack patterns - Barrage of projectiles and AOE projectiles
/// Two recovery states - normal and an extended bombing run
/// </summary>
public class RevenantBossRange : Enemy
{
    [Header("Damage")]
    public float projectileDamage = 8;
    public float AOEProjectileDamage = 16;
    public float AOEArcProjectileDamage = 16;

    [Header("Hover and movement")]
    public float hoverHeight = 2f; // height above ground
    public float hoverBobAmplitude = 0.25f; // up n down
    public float hoverBobSpeed = 2f;
    public float chaseSpeed = 3.5f;
    public float stopDistance = 7f; // how far away from player should it stop to attack
    public float attackRange = 12f;
    public float turnSpeedWhileAiming = 12f;
    public float agentAngularSpeed = 720f;
    public float agentAcceleration = 100f;
    
    [Space]

    [Header("Shooting")]
    public float projectileSpeed = 50f;
    public float fireCooldown = .6f;
    public Transform firePoint;         // first barrage point
    public Transform firePoint2;        // second barrage point
    public Transform AOEPoint;          // first AOE attack point
    public Transform AOEPoint2;         // second AOE attack point
    public Transform arcFiringPoint;    // Arcing delayed AOE point
    public EnemyProjectile projectilePrefab;
    public EnemyAOEProjectile AOEProjectilePrefab;
    public EnemyAOEArcingProjectile AOEArcingProjectilePrefab;
    public LayerMask projectileMask = ~0;
    public float spawnOffset = 0.1f; // distance from fire point for safety
    public float projectileSpread = 0.1f; // random spread angle
    public float arcLaunchAngle = 45f;
    public float randomDistanceVariance = 0.2f;

    public int barrageProjectileCount = 10; // how many projectiles in one barrage
    public float barrageInterval = 0.05f; // time between projectiles in barrage
    public float barrageAttackDelay = 0.5f; // delay attack to warn player
    public float AOEAttackDelay = 0.8f; // delay attack to warn player
    public int recoveryBarrageCount = 8;
    public float recoveryBarrageRandomYaw = 10f;
    public float recoveryBarrageProjectileSpeed = 20f;
    public float attackPeriodLength = 5f;
    private float recoveryBarrageFiringInterval = 0.5f;

    [Space]

    [Header("Recovery")]
    [Tooltip("How much time until the next attack")]
    public float recoveryTime = 4f;
    public float longRecoveryTime = 6f;     // actually a third attack phase bc why not

    [Header("Visuals")]
    public GameObject attackIndicator;

    IdleStateRevenant idle;
    ChaseStateRevenant chase;
    AttackStateRevenant_Barrage barrage_attack;
    AttackStateRevenant_ShootAOE AOE_attack;
    RecoveryStateRevenant recovery;
    LongRecoveryStateRevenant longRecovery;

    private RevenantAudioController audioController;

    float bobPhase;
    //public RevOrbitVisuals orbitVisuals; // replaced with simpler visuals

    public override void Start()
    {
        base.Start();
        audioController = GetComponent<RevenantAudioController>();
        //orbitVisuals = GetComponent<RevOrbitVisuals>();
        if (agent != null)
        {
            agent.speed = chaseSpeed;
            agent.angularSpeed = agentAngularSpeed;
            agent.acceleration = agentAcceleration;
            agent.autoBraking = true;
            agent.stoppingDistance = stopDistance * 0.8f;
        }

        idle = new IdleStateRevenant(this, stateMachine);
        chase = new ChaseStateRevenant(this, stateMachine);
        barrage_attack = new AttackStateRevenant_Barrage(this, stateMachine);
        AOE_attack = new AttackStateRevenant_ShootAOE(this, stateMachine);
        recovery = new RecoveryStateRevenant(this, stateMachine);
        longRecovery = new LongRecoveryStateRevenant(this, stateMachine);

        recoveryBarrageFiringInterval = Mathf.Abs(longRecoveryTime / recoveryBarrageCount);
        stateMachine.Initialize(idle); // enter idle first
    }

    public override void Update()
    {
        base.Update();
        UpdateHover();
    }

    /// <summary>
    /// Apply hovering visuals
    /// </summary>
    void UpdateHover()
    {
        if (agent == null) return;

        bobPhase += Time.deltaTime * hoverBobSpeed;
        agent.baseOffset = hoverHeight + Mathf.Sin(bobPhase) * hoverBobAmplitude; // using sin formula for bobbing
    }

    // State Getters
    public EnemyState GetIdle() => idle;
    public EnemyState GetChase() => chase;
    public EnemyState GetAttack() => barrage_attack;
    public EnemyState GetAOEAttack() => AOE_attack;
    public EnemyState GetRecovery() => recovery;
    public EnemyState GetLongRecovery() => longRecovery;

    /// <summary>
    /// Used to fire two projectile in direction of the target
    /// </summary>
    public void FireOnce()
    {

        if (!firePoint || !firePoint2 || !projectilePrefab) return;
        
        // Calculate directions and spawn projectiles
        Vector3 direction1 = (target ? target.position + Vector3.up * .5f - firePoint.position : transform.forward).normalized;
        Vector3 spawnPoint1 = firePoint.position + direction1 * spawnOffset;
        Quaternion rotation1 = Quaternion.LookRotation(direction1, Vector3.up);

        Vector3 direction2 = (target ? target.position + Vector3.up * .5f - firePoint2.position : transform.forward).normalized;
        Vector3 spawnPoint2 = firePoint2.position + direction2 * spawnOffset;
        Quaternion rotation2 = Quaternion.LookRotation(direction2, Vector3.up);

        // add some spread
        direction1 += Random.insideUnitSphere * projectileSpread;
        direction1.Normalize();

        direction2 += Random.insideUnitSphere * projectileSpread;
        direction2.Normalize();

        EnemyProjectile projectile1 = Instantiate(projectilePrefab, spawnPoint1, rotation1);
        projectile1.Init(direction1 * projectileSpeed, projectileMask, this.projectileDamage);

        EnemyProjectile projectile2 = Instantiate(projectilePrefab, spawnPoint2, rotation2);
        projectile2.Init(direction2 * projectileSpeed, projectileMask, this.projectileDamage);

        audioController?.PlayFireProjectileSound();
    }

    public void FireBarrage()
    {
        StartCoroutine(FireBarrageCoroutine());
    }

    public IEnumerator FireBarrageCoroutine()
    {
        // Insert firing indicator vfx/sfx here
        playAttackIndicator();

        yield return new WaitForSeconds(barrageAttackDelay); // initial delay before starting barrage
        for (int i = 0; i < barrageProjectileCount; i++)
        {
            FireOnce();
            yield return new WaitForSeconds(barrageInterval); // small delay between shots
        }
    }

    public void FireAOE()
    {
        if (!AOEPoint || !AOEPoint2 || !projectilePrefab) {
            Debug.LogWarning("AOE firing points and/or projectile prefab are not assigned.");
            return;
        }
        StartCoroutine(FireAOECoroutine());
    }

    /// <summary>
    /// Fire two AOE projectiles towards the target
    /// </summary>
    public IEnumerator FireAOECoroutine()
    {
        // Insert firing indicator vfx/sfx here
        playAttackIndicator();

        yield return new WaitForSeconds(AOEAttackDelay); // initial delay before firing AOE

        // Calculate directions and spawn projectiles
        Vector3 direction1 = (target ? target.position + Vector3.up * .5f - AOEPoint.position : transform.forward).normalized;
        Vector3 spawnPoint1 = AOEPoint.position + direction1 * spawnOffset;
        Quaternion rotation1 = Quaternion.LookRotation(direction1, Vector3.up);

        Vector3 direction2 = (target ? target.position + Vector3.up * .5f - AOEPoint2.position : transform.forward).normalized;
        Vector3 spawnPoint2 = AOEPoint2.position + direction2 * spawnOffset;
        Quaternion rotation2 = Quaternion.LookRotation(direction2, Vector3.up);

        EnemyAOEProjectile projectile1 = Instantiate(AOEProjectilePrefab, spawnPoint1, rotation1);
        projectile1.Init(direction1 * projectileSpeed, projectileMask, this.AOEProjectileDamage);

        EnemyAOEProjectile projectile2 = Instantiate(AOEProjectilePrefab, spawnPoint2, rotation2);
        projectile2.Init(direction2 * projectileSpeed, projectileMask, this.AOEProjectileDamage);

        audioController?.PlayFireAOEProjectileSound();
    }

    public void RecoveryBarrage()
    {
        StartCoroutine(RecoveryBarrageCoroutine());
    }

    /// <summary>
    /// Fire a circular barrage of arcing AOE projectiles around the boss.
    /// </summary>
    public IEnumerator RecoveryBarrageCoroutine()
    {
        // Fire projectiles in a circular pattern with some random yaw
        float angleStep = 360f / recoveryBarrageCount;
        for (int i = 0; i < recoveryBarrageCount; i++)
        {
            // Calculate firing direction with random yaw
            float currentAngle = i * angleStep;
            Quaternion facingRotation = Quaternion.Euler(0, currentAngle, 0);
            Vector3 baseDir = facingRotation * transform.forward;
            float randomYaw = Random.Range(-recoveryBarrageRandomYaw, recoveryBarrageRandomYaw);
            Quaternion randomRot = Quaternion.Euler(0, randomYaw, 0);
            Vector3 finalHorizontalDir = randomRot * baseDir;

            // Calculate firing vector with arc angle
            Vector3 upComponent = Vector3.up * Mathf.Tan(arcLaunchAngle * Mathf.Deg2Rad);
            Vector3 firingVector = (finalHorizontalDir + upComponent).normalized;

            // Apply random speed variance
            float randomSpeedMod = Random.Range(1f - randomDistanceVariance, 1f + randomDistanceVariance);
            Vector3 finalVelocity = firingVector * recoveryBarrageProjectileSpeed * randomSpeedMod;
            Vector3 spawnPoint = arcFiringPoint.position + finalHorizontalDir * spawnOffset;
            
            EnemyAOEArcingProjectile projectile = Instantiate(AOEArcingProjectilePrefab, spawnPoint, Quaternion.LookRotation(firingVector));
            projectile.Init(finalVelocity, projectileMask, AOEProjectileDamage);

            yield return new WaitForSeconds(recoveryBarrageFiringInterval); // small delay between shots
        }
    }   

    /// <summary>
    /// Play attack indicator VFX and SFX (we're using WWise later)
    /// </summary>
    void playAttackIndicator()
    {
        if (attackIndicator != null)
        {
            GameObject newFx = Instantiate(attackIndicator);
            newFx.transform.position = transform.position + Vector3.up * 8f;
            newFx.transform.rotation = Quaternion.identity;
            newFx.transform.localScale = Vector3.one * 6f;

            audioController?.PlayAttackIndicatorSound();

            Destroy(newFx, 0.25f); // destroy after a short time
        }
    }

    /// <summary>
    /// Used to initialize damage for this enemy.
    /// </summary>
    /// <param name="newDamage"> Intended damage value </param>
    public void InitializeDamage(float newDamage)
    {
        // this.projectileDamage = Mathf.CeilToInt(newDamage);
        this.projectileDamage = newDamage;
        Debug.Log("Projectile Damage: " + this.projectileDamage);
    }

    /// <summary>
    /// Get base damage of this Range Enemy
    /// </summary>
    public float GetBaseDamage() => projectileDamage;
}
