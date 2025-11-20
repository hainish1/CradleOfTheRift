using System.Collections;
using UnityEngine;


/// <summary>
/// Class - Represents a Revenant enemy boss, inherits from Base Enemy class 
/// ,also defines functionality of its own.
/// Code copied straight from RangeEnemy.
/// </summary>
public class RevenantBossRange : Enemy
{
    public float projectileDamage = 1;

    [Header("Hover and movement")]
    public float hoverHeight = 2f; // height above ground
    public float hoverBobAmplitude = 0.25f; // up n down
    public float hoverBobSpeed = 2f;
    public float chaseSpeed = 3.5f;
    public float stopDistance = 7f; // how far away from player should it stop 
    public float attackRange = 12f;
    public float turnSpeedWhileAiming = 12f;
    public float agentAngularSpeed = 720f;
    public float agentAcceleration = 100f;
    
    [Space]

    [Header("Shooting")]
    public float projectileSpeed = 50f;
    public float fireCooldown = .6f;
    public Transform firePoint;     // first barrage point
    public Transform firePoint2;    // second barrage point
    public Transform AOEPoint;      // first AOE attack point
    public Transform AOEPoint2;     // second AOE attack point
    public EnemyProjectile projectilePrefab;
    public EnemyAOEProjectile AOEProjectilePrefab;
    public LayerMask projectileMask = ~0;
    //public float AOEProjectileDamageMultiplier = 4f; // AOE does more damage
    public float spawnOffset = 0.1f; // a little away fro fire point, safety
    public float projectileSpread = 0.1f; // random spread angle

    public int barrageProjectileCount = 10; // how many projectiles in one barrage
    public float barrageInterval = 0.05f; // time between projectiles in barrage
    public float barrageAttackDelay = 0.5f; // delay attack to warn player
    public float AOEAttackDelay = 0.8f; // delay attack to warn player

    [Space]

    [Header("Recovery")]
    [Tooltip("How much time to start again, basically reload time")]
    public float recoveryTime = 0.5f;
    public float longRecoveryTime = 5f;     // hacky shit i made bc i aint deviating from whats already written. used specifically after the aoe attack.

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
    //public RevOrbitVisuals orbitVisuals; // I promise i will actually implement this later but for now just get rid of it.


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

        stateMachine.Initialize(idle); // enter idle first
    }

    public override void Update()
    {
        base.Update();
        UpdateHover();
    }

    /// <summary>
    /// Apply Hovering Visuals to the Enemy
    /// </summary>
    void UpdateHover()
    {
        if (agent == null) return;

        bobPhase += Time.deltaTime * hoverBobSpeed;
        agent.baseOffset = hoverHeight + Mathf.Sin(bobPhase) * hoverBobAmplitude; // usign sin formula for bobbing
    }

    // HELPERS

    public EnemyState GetIdle() => idle;
    public EnemyState GetChase() => chase;
    public EnemyState GetAttack() => barrage_attack;
    public EnemyState GetAOEAttack() => AOE_attack;
    public EnemyState GetRecovery() => recovery;
    public EnemyState GetLongRecovery() => longRecovery;

    /// <summary>
    /// Used to fire one projectile in direction of te target
    /// </summary>
    public void FireOnce()
    {

        if (!firePoint || !firePoint2 || !projectilePrefab) return;
        
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
        //EnemyAOEProjectile projectile1 = Instantiate(AOEProjectilePrefab, spawnPoint1, rotation1); // For testing AOE
        projectile1.Init(direction1 * projectileSpeed, projectileMask, this.projectileDamage);

        EnemyProjectile projectile2 = Instantiate(projectilePrefab, spawnPoint2, rotation2);
        //EnemyAOEProjectile projectile2 = Instantiate(AOEProjectilePrefab, spawnPoint2, rotation2); // For testing AOE
        projectile2.Init(direction2 * projectileSpeed, projectileMask, this.projectileDamage);

        audioController?.PlayFireProjectileSound();
        // TODO: Rework orbit visuals
        // if (orbitVisuals != null)
        // {
        //     int orbIndex = orbitVisuals.GetNextVisibleOrbIndex();
        //     if (orbIndex >= 0)
        //     {
        //         orbitVisuals.HideOrb(orbIndex);
        //     }
        //     else
        //     {
        //         // no orbs left,maybe i can go to recovery
        //     }
        // }
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
        if (!AOEPoint || !AOEPoint2 || !projectilePrefab) return;
        StartCoroutine(FireAOECoroutine());
    }

    public IEnumerator FireAOECoroutine()
    {
        // Insert firing indicator vfx/sfx here
        playAttackIndicator();

        yield return new WaitForSeconds(AOEAttackDelay); // initial delay before firing AOE
        Vector3 direction1 = (target ? target.position + Vector3.up * .5f - AOEPoint.position : transform.forward).normalized;
        Vector3 spawnPoint1 = AOEPoint.position + direction1 * spawnOffset;
        Quaternion rotation1 = Quaternion.LookRotation(direction1, Vector3.up);

        Vector3 direction2 = (target ? target.position + Vector3.up * .5f - AOEPoint2.position : transform.forward).normalized;
        Vector3 spawnPoint2 = AOEPoint2.position + direction2 * spawnOffset;
        Quaternion rotation2 = Quaternion.LookRotation(direction2, Vector3.up);

        EnemyAOEProjectile projectile1 = Instantiate(AOEProjectilePrefab, spawnPoint1, rotation1);
        projectile1.Init(direction1 * projectileSpeed, projectileMask, this.projectileDamage);

        EnemyAOEProjectile projectile2 = Instantiate(AOEProjectilePrefab, spawnPoint2, rotation2);
        projectile2.Init(direction2 * projectileSpeed, projectileMask, this.projectileDamage);

        audioController?.PlayFireAOEProjectileSound();
    }   

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
    /// Used to initialize damage done by this Range enemy when it is initialized. New Damage value can be initialized using this.
    /// </summary>
    /// <param name="newDamage"></param>
    public void InitializeDamage(float newDamage)
    {
        // this.projectileDamage = Mathf.CeilToInt(newDamage);
        this.projectileDamage = newDamage;
        Debug.Log("Projectile Damage: " + this.projectileDamage);
    }

    /// <summary>
    /// Get Base Damage of this Range Enemy
    /// </summary>
    /// <returns></returns>
    public float GetBaseDamage() => projectileDamage;

    public override void Die()
    {
        // DOESNT WORRRK!!!!!! FFUUUUUUUCKKK
        Debug.Log("Revenant Boss Died");
        audioController?.PlayDeathSound();
        base.Die();
    }

}
