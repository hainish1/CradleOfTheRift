// <summary>
//   <authors>
//     Jeidi Mo, Samuel Rigby
//   </authors>
//   <para>
//     Written by Jeidi Mo for GAMES 4510, University of Utah.
//     Contributed to by Samuel Rigby.
//          -Added compatability with golem animations.
//   </para>
// </summary>

using System;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.EventSystems.EventTrigger;

/// <summary>
/// Class - Represents a Golem, inherits from Base Enemy class 
/// and defines functionality of its own
/// </summary>
public class EnemyGolem : Enemy
{
    private static readonly float[] chaseProbeFractions = { 1f, 0.85f, 0.65f, 0.45f, 0.25f };

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
    [SerializeField] private GameObject rockHand;
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
    [SerializeField] private AK.Wwise.Event groundSlamSFX;
    IdleStateGolem idle;
    ChaseStateGolem chase;
    RangeAttackStateGolem rangeAttack;
    MeleeAttackStateGolem meleeAttack;
    RecoveryStateGolem recovery;

    [Header("Animation Settings")]
    public AnimationClip throwAnim;
    public AnimationClip meleeAnim;
    public float throwAnimSpeedMultiplier = 1f;
    public float meleeAnimSpeedMultiplier = 1f;
    [HideInInspector] public Animator golemAnim;

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
        RecalculateAttackAnimationSpeeds();

        stateMachine.Initialize(idle);
    }

    public override void Update()
    {
        stateMachine.Tick();

        // Blend golem animation between idle and moving.
        float moveBlend = agent.velocity.magnitude / agent.speed;
        golemAnim.SetFloat("MoveVector", moveBlend, dampTime: 0.03f, Time.deltaTime);
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
        EnsureAgentOnNavMesh();
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.ResetPath();
    }

    public bool EnsureAgentOnNavMesh(float maxDistance = 8f)
    {
        if (agent == null || !agent.isActiveAndEnabled) return false;

        if (agent.isOnNavMesh)
        {
            agent.nextPosition = transform.position;
            return true;
        }

        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
        {
            return false;
        }

        transform.position = hit.position;
        agent.Warp(hit.position);
        agent.nextPosition = hit.position;
        return true;
    }

    public bool TryResumePathing()
    {
        if (agent == null || !agent.isActiveAndEnabled) return false;

        agent.updatePosition = true;
        agent.updateRotation = true;

        if (!EnsureAgentOnNavMesh())
        {
            return false;
        }

        agent.isStopped = false;
        return true;
    }

    public bool TrySetChaseDestination()
    {
        if (target == null || !TryResumePathing())
        {
            return false;
        }

        Vector3 flatTargetOffset = target.position - transform.position;
        flatTargetOffset.y = 0f;

        if (flatTargetOffset.sqrMagnitude < 0.0001f)
        {
            agent.ResetPath();
            return true;
        }

        for (int i = 0; i < chaseProbeFractions.Length; i++)
        {
            Vector3 probePoint = transform.position + (flatTargetOffset * chaseProbeFractions[i]);
            float sampleRadius = i == 0 ? 8f : 4f;

            if (TrySetPathToPoint(probePoint, sampleRadius))
            {
                return true;
            }
        }

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.ResetPath();
        return false;
    }

    private bool TrySetPathToPoint(Vector3 desiredPoint, float sampleRadius)
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
        {
            return false;
        }

        if (!NavMesh.SamplePosition(desiredPoint, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
        {
            return false;
        }

        NavMeshPath path = new NavMeshPath();
        if (!agent.CalculatePath(hit.position, path))
        {
            return false;
        }

        if (path.status == NavMeshPathStatus.PathInvalid || path.corners.Length == 0)
        {
            return false;
        }

        agent.isStopped = false;
        return agent.SetPath(path);
    }

    public void FaceMovementDirectionSmooth(float speed)
    {
        Vector3 dir = Vector3.zero;

        if (agent != null && agent.enabled)
        {
            dir = agent.desiredVelocity;

            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = agent.velocity;
            }

            if (dir.sqrMagnitude < 0.0001f && agent.hasPath)
            {
                dir = agent.steeringTarget - transform.position;
            }
        }

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        dir.Normalize();
        Quaternion lookRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * speed);
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
        print("Playing Throw SFX");
        if (throwSFX.IsValid())
        {
            print("Posting SFX");
            throwSFX.Post(gameObject);
        }
    }

    public void ThrowRock()
    {
        // Play the SFX.
        PlayThrowSFX();
        
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
    ///   <para>
    ///     Hides the golem's right hand.
    ///   </para>
    /// </summary>
    public void HideRockHand() => rockHand.SetActive(false);

    /// <summary>
    ///   <para>
    ///     Shows the golem's right hand.
    ///   </para>
    /// </summary>
    public void ShowRockHand() => rockHand.SetActive(true);

    /// <summary>
    ///   <para>
    ///     Recalulates the melee and throw attack animation speeds.
    ///   </para>
    /// </summary>
    protected void RecalculateAttackAnimationSpeeds()
    {
        golemAnim.SetFloat("MeleeAnimSpeedMultiplier", meleeAnimSpeedMultiplier);
        golemAnim.SetFloat("ThrowAnimSpeedMultiplier", throwAnimSpeedMultiplier);
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
        
        // Play the ground slam SFX
        if (groundSlamSFX.IsValid())
        {
            groundSlamSFX.Post(gameObject);
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
