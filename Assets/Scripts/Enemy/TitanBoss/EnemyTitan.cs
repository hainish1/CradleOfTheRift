// using UnityEditor.UI;
using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Class - Represents a Golem, inherits from Base Enemy class 
/// and defines functionality of its own
/// </summary>
public class EnemyTitan : EnemyGolem
{
    // [Header("Movement and Range")]
    // public float chaseSpeed = 10f;
    // public float shootingRange = 60f;
    // public float minAttackDistance = 4f;
    // // public int damage = 10; 
    // //public float knockbackPower = 100f; // how far can push the enemy
    // // public float minRecoveryTime = 1f;
    // // public float maxRecoveryTime = 1f;
    // public float wanderInterval = 3f;  // Time between wander direction changes
    // public float wanderRadius = 20f;    // Radius for wandering distance from its current position
    // public float minWanderTime = 1f;
    // public float maxWanderTime = 1f;
    // public float postAttackCooldown = 1f;

    // [Header("Rock Throw Attack Settings")]
    // public GameObject rockProjectilePrefab;
    // public float directDamage = 20f;    // Not used rn
    // public float AOEDamage = 5f;        // Not used rn
    // public float minWindupTime = 1.5f;
    // public float maxWindupTime = 2.5f;
    // public float projectileVelocity = 12f;
    // public float projectileKnockback = 100f;
    // public Transform projectileSpawnPoint; // Where the rock spawns
    // public float turnSpeedWhileAiming = 8f;
    // public LayerMask projectileMask = ~0;

    [Header("Rock Barrage Attack Settings")]
    public GameObject rockBarrageProjectilePrefab;
    public float barrageDirectDamage = 20f;    // Not used rn
    public float barrageProjectileVelocity = 12f;
    public float barrageProjectileKnockback = 100f;
    public Transform barrageProjectileSpawnPoint; // Where the rock spawns
    public LayerMask barrageProjectileMask = ~0;

    [Header("Double Sweep Melee Settings")]
    public float sweepMeleeDamage = 20f;
    public float sweepHorizontalKnockback = 100f;
    public float sweepVerticalKnockback = 100f;
    public float sweepRadius = 3f;              // Use box isntead of sphere
    //public LayerMask meleeMask = ~0;
    public float sweepCooldown = 0.5f;
    public Transform sweepPosition;

    IdleStateTitan idle;
    ChaseStateTitan chase;
    RangeAttackStateTitan rangeAttack;
    MeleeAttackStateTitan meleeAttack;
    RecoveryStateTitan recovery;

    // [Header("VFX / SFX")]
    // public GameObject throwRockVFXPrefab;
    // [SerializeField] private AK.Wwise.Event throwSFX;
    // IdleStateTitan idle;
    // ChaseStateTitan chase;
    // RangeAttackStateTitan rangeAttack;
    // MeleeAttackStateTitan meleeAttack;
    // RecoveryStateTitan recovery;

    //public Animator golemAnim;

    // private void OnEnable()
    // {
    //     EnemyRegistry.RegisterGolem(this);
    // }

    // private void OnDisable()
    // {
    //     EnemyRegistry.UnregisterGolem(this);
    // }

    public override void Start()
    {
        base.Start(); // run stuff that we wrote in base enemy class first
        SnapToNavMesh();
        if (agent != null) agent.speed = chaseSpeed;

        var kb = GetComponent<AgentKnockBack>();
        if (kb != null) kb.manageAgentPosition = true;

        // Initialize states
        idle = new IdleStateTitan(this, stateMachine);
        chase = new ChaseStateTitan(this, stateMachine);
        rangeAttack = new RangeAttackStateTitan(this, stateMachine);
        meleeAttack = new MeleeAttackStateTitan(this, stateMachine);
        recovery = new RecoveryStateTitan(this, stateMachine);
        golemAnim = GetComponentInChildren<Animator>();

        stateMachine.Initialize(idle);
    }


    /// <summary>
    /// Perform a simple melee attack by checking for the player within a radius and applying damage and knockback if hit
    /// </summary>
    public void MeleeSweepAttack()
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
    }

    /// <summary>
    /// Shoots out a big spread of small rock projectiles in one direction
    /// </summary>
    public void RockBarrageBlast()
    {
        
    }

    //Getters for States that this Melee Enemy has
    public EnemyState GetIdle() => idle;
    public EnemyState GetChase() => chase;
    public EnemyState GetAttack() => rangeAttack;
    public EnemyState GetMeleeAttack() => meleeAttack;
    public EnemyState GetRecovery() => recovery;

    private float EstimateParticleLifetime(GameObject fx)
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

        // Draw the melee sphere so you can balance the radius in the editor!
        if (meleePosition != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(meleePosition.position, meleeRadius);
        }
    }

    // /// <summary>
    // /// Try to apply damage and impulse to the player GameObject caught in colliders 
    // /// </summary>
    // /// <param name="playerCol"></param>
    // public void TryApplyHit(Collider playerCol)
    // {
    //     if (hitAppliedThisAttack) return;
    //     if (Time.time < nextAttackAllowed) return;

    //     Vector3 toPlayer = playerCol.transform.position - transform.position;
    //     toPlayer.y = 0f;

    //     var pm = playerCol.GetComponentInParent<PlayerMovement>();
    //     if (pm != null)
    //     {
    //         pm.ApplyImpulse(toPlayer.normalized * knockbackPower);

    //         var damageable = pm.GetComponentInParent<IDamageable>();
    //         if (damageable != null && !damageable.IsDead)
    //         {
    //             damageable.TakeDamage(slamDamage);
    //         }
    //     }
    //     hitAppliedThisAttack = true;
    //     nextAttackAllowed = Time.time + attackCooldown; // ehhh do I need this here
    //     EnableHitBox(false);

    // }

    // /// <summary>
    // /// Initialize damage done by this enemy, can be updated by enemy spawner
    // /// </summary>
    // /// <param name="newDamage"></param>
    // public void InitializeSlamDamage(float newDamage)
    // {
    //     // this.slamDamage = Mathf.CeilToInt(newDamage);
    //     this.slamDamage = newDamage;
    //     Debug.Log("Slam Damage: " + this.slamDamage);
    // }

    // public Vector3 CalculateBallisticVelocity(Vector3 startPoint, Vector3 endPoint, float height, out float duration)
    // {
    //     float gravity = Physics.gravity.y * gravityScale;

    //     // Flatten the target to same Y 
    //     endPoint.y = startPoint.y;
    //     float displacementY = 0f;

    //     Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0, endPoint.z - startPoint.z);

    //     // vertical velocity: v = sqrt(-2gh)
    //     float velocityY = Mathf.Sqrt(-2 * gravity * height);

    //     // time calcs
    //     float timeToPeak = -velocityY / gravity;
    //     float timeToFall = Mathf.Sqrt(2 * (displacementY - height) / gravity);

    //     duration = timeToPeak + timeToFall; // total flight time

    //     // horizontal velocity
    //     Vector3 velocityXZ = displacementXZ / duration;

    //     return velocityXZ + Vector3.up * velocityY;
    // }

    // /// <summary>
    // /// Move with swept-sphere collision
    // /// </summary>
    // public Vector3 SweepMove(Vector3 vel, float dt)
    // {
    //     Vector3 delta = vel * dt;
    //     float dist = delta.magnitude;
    //     if (dist < 1e-5f) return vel;

    //     Vector3 origin = transform.position + Vector3.up * (collisionRadius + 0.02f);
    //     Vector3 dir = delta.normalized;

    //     if (Physics.SphereCast(origin, collisionRadius, dir, out RaycastHit sweepHit,
    //             dist, groundMask, QueryTriggerInteraction.Ignore))
    //     {
    //         // Stop just before the hit surface
    //         float safeDist = Mathf.Max(0f, sweepHit.distance - 0.01f);
    //         transform.position += dir * safeDist;

    //         // Ground hit, snap center to correct height above surface
    //         if (sweepHit.normal.y > 0.6f)
    //         {
    //             float halfH = agent != null ? agent.height * 0.7f : 0f;
    //             Vector3 pos = transform.position;
    //             pos.y = sweepHit.point.y + (halfH + startHeightAboveGround);
    //             transform.position = pos;
    //         }

    //         // remove the component going into the surface
    //         float velIntoSurface = Vector3.Dot(vel, -sweepHit.normal);
    //         if (velIntoSurface > 0f)
    //             vel += sweepHit.normal * velIntoSurface;
    //     }
    //     else
    //     {
    //         transform.position += delta;
    //     }

    //     return vel;
    // }

    // /// <summary>
    // /// Get Base Damage for this enemy
    // /// </summary>
    // /// <returns></returns>
    // public float GetBaseDamage() => slamDamage;
    // public void PlayMeleePSVFX(GameObject vfxPrefab, Transform spawnPos)
    // {
    //     if (vfxPrefab == null) return;

    //     spawnPos = spawnPos != null ? spawnPos : transform;

    //     GameObject fx;
    //     if (ObjectPool.instance != null)
    //     {
    //         fx = ObjectPool.instance.GetObject(vfxPrefab, spawnPos);
    //     }
    //     else
    //     {
    //         fx = Instantiate(vfxPrefab, spawnPos.position, Quaternion.identity, spawnPos);
    //     }

    //     float lifetime = EstimateParticleLifetime(fx);

    //     if (ObjectPool.instance != null)
    //     {
    //         ObjectPool.instance.ReturnObject(fx, lifetime);
    //     }
    //     else
    //     {
    //         Destroy(fx, lifetime);
    //     }
    // }
}
