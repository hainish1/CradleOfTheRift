// using UnityEditor.UI;
using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Class - Represents a Golem, inherits from Base Enemy class 
/// and defines functionality of its own
/// </summary>
public class EnemyGolem : Enemy
{
    [Header("Movement and Range")]
    public float chaseSpeed = 10f;
    public float shootingRange = 60f;
    public float minAttackDistance = 4f;
    // public int damage = 10; 
    //public float knockbackPower = 100f; // how far can push the enemy
    // public float minRecoveryTime = 1f;
    // public float maxRecoveryTime = 1f;
    public float wanderInterval = 3f;  // Time between wander direction changes
    public float wanderRadius = 20f;    // Radius for wandering distance from its current position
    public float minWanderTime = 1f;
    public float maxWanderTime = 1f;
    public float postAttackCooldown = 1f;

    [Header("Rock Throw Ranged Settings")]
    public GameObject rockProjectilePrefab;
    public float directDamage = 20f;
    public float AOEDamage = 20f;
    //public float AOERadius = 3f; // Moved to the projectile itself
    public float minWindupTime = 1.5f;
    public float maxWindupTime = 2.5f;
    public float projectileVelocity = 12f;
    public float projectileKnockback = 100f;
    public Transform projectileSpawnPoint; // Where the rock spawns
    public float turnSpeedWhileAiming = 8f;
    public LayerMask projectileMask = ~0;
    public float playerAimOffset = 1.5f;

    [Header("Ground Slam Melee Settings")]
    public float meleeDamage = 20f;
    public float meleeHorizontalKnockback = 100f;
    public float meleeVerticalKnockback = 100f;
    public float meleeRadius = 3f;
    //public LayerMask meleeMask = ~0;
    public float meleeCooldown = 0.5f;
    public Transform meleePosition;
    public GameObject groundSlamPrefab;

    [Header("VFX / SFX")]
    public GameObject throwRockVFXPrefab;
    [SerializeField] private AK.Wwise.Event throwSFX;
    IdleStateGolem idle;
    ChaseStateGolem chase;
    RangeAttackStateGolem rangeAttack;
    MeleeAttackStateGolem meleeAttack;
    RecoveryStateGolem recovery;

    public Animator golemAnim;

    protected void OnEnable()
    {
        EnemyRegistry.RegisterGolem(this);
    }

    protected void OnDisable()
    {
        EnemyRegistry.UnregisterGolem(this);
    }

    public override void Start()
    {
        base.Start(); // run stuff that we wrote in base enemy class first
        SnapToNavMesh();
        if (agent != null) agent.speed = chaseSpeed;

        var kb = GetComponent<AgentKnockBack>();
        if (kb != null) kb.manageAgentPosition = true;

        // Initialize states
        idle = new IdleStateGolem(this, stateMachine);
        chase = new ChaseStateGolem(this, stateMachine);
        rangeAttack = new RangeAttackStateGolem(this, stateMachine);
        meleeAttack = new MeleeAttackStateGolem(this, stateMachine);
        recovery = new RecoveryStateGolem(this, stateMachine);
        golemAnim = GetComponentInChildren<Animator>();

        stateMachine.Initialize(idle);
    }

    protected void SnapToNavMesh()
    {
        if (agent == null) return;
        const float maxDistance = 100f;
        if (NavMesh.SamplePosition(transform.position, out var hit, maxDistance, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            agent.nextPosition = hit.position;
            transform.position = hit.position;
        }
    }

    /// <summary>
    /// Pause NavMeshAgent steering for manual position control
    /// </summary>
    public void PauseAgent()
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.ResetPath();
        agent.updatePosition = false;
        agent.updateRotation = false;
    }

    /// <summary>
    /// Resume NavMeshAgent steering, snapping to the nearest valid NavMesh point.
    /// </summary>
    public void ResumeAgent()
    {
        if (agent == null || !agent.isActiveAndEnabled) return;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.isStopped = true;
        agent.ResetPath();
    }
    public void FaceTargetSmooth(float speed)
    {
        if (target == null) return;
        Vector3 dir = (target.position - transform.position);
        dir.y = 0; // Keep rotation upright
        if (dir.sqrMagnitude < 0.0001f) return;

        dir.Normalize();
        Quaternion lookRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * speed);
    }

    public void PlayThrowSFX()
    {
        throwSFX?.Post(gameObject);
    }

    public void ThrowRock()
    {
        // Use the defined spawn point or default to slightly above the golem
        Vector3 spawnPos = projectileSpawnPoint != null 
            ? projectileSpawnPoint.position 
            : transform.position + Vector3.up * 1.5f;

        // Target the player's center mass rather than their feet
        Vector3 targetPos = target.position + Vector3.up * playerAimOffset; 
        
        // Pull from the Object Pool if available, else instantiate normally
        GameObject rockObj;
        if (ObjectPool.instance != null)
        {
            Transform spawnTransform = projectileSpawnPoint != null ? projectileSpawnPoint : this.transform;
            rockObj = ObjectPool.instance.GetObject(rockProjectilePrefab, spawnTransform);
            rockObj.transform.position = spawnPos;
            rockObj.transform.rotation = Quaternion.identity;
        }
        else
        {
            rockObj = Instantiate(rockProjectilePrefab, spawnPos, Quaternion.identity);
        }

        // Initialize the projectile
        if (rockObj.TryGetComponent<EnemyRockProjectile>(out EnemyRockProjectile rock))
        {
            float distanceXZ = Vector2.Distance(new Vector2(spawnPos.x, spawnPos.z), new Vector2(targetPos.x, targetPos.z));
            float timeToTarget = distanceXZ / projectileVelocity;
            timeToTarget = Mathf.Max(0.1f, timeToTarget);

            Vector3 calculatedVelocity = CalculateLaunchVelocity(spawnPos, targetPos, timeToTarget);

            // Init: (Vector3 velocity, LayerMask mask, float damage, float knockback, float aoeRadius, float aoeDamage)
            rock.Init(calculatedVelocity, projectileMask, directDamage, projectileKnockback, AOEDamage);
        }
    }

    /// <summary>
    /// Calculates the precise 3D velocity required to hit a target point over a specific duration, factoring in Unity's gravity.
    /// </summary>
    protected Vector3 CalculateLaunchVelocity(Vector3 startPoint, Vector3 targetPoint, float timeToTarget)
    {
        // Calculate displacement
        Vector3 displacement = targetPoint - startPoint;
        Vector3 displacementXZ = new Vector3(displacement.x, 0, displacement.z);

        // Calculate XZ (horizontal) velocity needed to cover the distance in timeToTarget
        Vector3 velocityXZ = displacementXZ / timeToTarget;

        // Calculate Y (vertical) velocity using the kinematic equation: d = vi*t + 1/2*a*t^2
        // Rearranged to solve for vi (initial velocity): vi = (d - 1/2*a*t^2) / t
        float velocityY = (displacement.y - (Physics.gravity.y * Mathf.Pow(timeToTarget, 2)) / 2f) / timeToTarget;

        // Combine horizontal and vertical velocities
        return velocityXZ + (Vector3.up * velocityY);
    }

    /// <summary>
    /// Perform a simple melee attack by checking for the player within a radius and applying damage and knockback if hit
    /// </summary>
    public void MeleeSlamAttack()
    {
        // Sphere for now, maybe use box later
        Collider[] hitColliders = Physics.OverlapSphere(meleePosition.position, meleeRadius);

        foreach (Collider hit in hitColliders)
        {
            // Check if the thing we hit was the player
            if (hit.CompareTag("Player"))
            {
                // Apply damage
                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                {
                    damageable.TakeDamage(meleeDamage);
                }

                // Apply knockback
                var pm = hit.GetComponentInParent<PlayerMovement>();
                if (pm != null)
                {
                    Vector3 horizontalDirection = (hit.transform.position - transform.position).normalized;
                    horizontalDirection.y = 0;
                    Vector3 finalKnockback = (horizontalDirection * meleeHorizontalKnockback) + (Vector3.up * meleeVerticalKnockback);
                    
                    pm.ApplyImpulse(finalKnockback);
                }

                // Once the player has been hit, break out of the loop so we don't accidentally hit them twice
                break; 
            }
        }

        // Play ground slam VFX
        if (groundSlamPrefab != null)        
        {
            GameObject slamVFX = Instantiate(groundSlamPrefab, meleePosition.position, Quaternion.identity);
            slamVFX.transform.localScale = Vector3.one * meleeRadius * 0.5f;
            Destroy(slamVFX, EstimateParticleLifetime(slamVFX));
        }
    }

    //Getters for States that this Melee Enemy has
    public EnemyState GetIdle() => idle;
    public EnemyState GetChase() => chase;
    public EnemyState GetAttack() => rangeAttack;
    public EnemyState GetMeleeAttack() => meleeAttack;
    public EnemyState GetRecovery() => recovery;

    protected float EstimateParticleLifetime(GameObject fx)
    {
        float max = 0.25f;

        var systems = fx.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            var main = ps.main;
            float startDelay = main.startDelay.constantMax;
            float duration = main.duration;
            float startLifetime = main.startLifetime.constantMax;
            float total = startDelay + duration + startLifetime;
            if (total > max) max = total;
        }

        return max;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minAttackDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, shootingRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);

        // Draw the melee sphere
        if (meleePosition != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(meleePosition.position, meleeRadius);
        }
    }

    // Called by BossSpawner to initialize damage based on difficulty scaler
    public void InitializeAllDamage(float multiplier)
    {
        // Kevin was here 
        // Debug.Log($"EnemyGolem Boss Base Damage. Base Direct Damage: {directDamage}, Base AOE Damage: {AOEDamage}, Base Melee Damage: {meleeDamage}");
        directDamage *= multiplier;
        AOEDamage *= multiplier;
        meleeDamage *= multiplier;

        // Debug.Log($"EnemyGolem: Scaled damage with multiplier {multiplier}. Direct Damage: {directDamage}, AOE Damage: {AOEDamage}, Melee Damage: {meleeDamage}");
    }
}
