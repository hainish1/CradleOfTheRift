// using UnityEditor.UI;
using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

/// <summary>
/// Class - Represents a Golem, inherits from Base Enemy class 
/// and defines functionality of its own
/// </summary>
public class EnemyTitan : EnemyGolem
{

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
    public float barrageProjectileKnockback = 20f;
    public Transform barrageProjectileSpawnPoint; // Where the rock spawns
    public LayerMask barrageProjectileMask = ~0;
    public int barrageProjectileCount = 8;
    public float barrageSpreadAngle = 45f; // Total angle of the spread
    public float barrageAimOffset = 1.5f;

    [Header("Double Sweep Melee Settings")]
    public float sweepMeleeDamage = 20f;
    public float sweepSideKnockback = 100f;
    public float sweepUpwardKnockback = 20f;
    public Vector3 sweepBoxSize = new Vector3(6f, 4f, 6f);
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
    /// Perform a sweep attack by checking for the player within a box and applying directional knockback if hit.
    /// 0 for left, 1 for right. Any other value defaults to an upward knockback.
    /// </summary>
    public void MeleeSweepAttack(int knockbackDirection)
    {
        Transform sweepTransform = sweepPosition != null ? sweepPosition : transform;
        Vector3 halfExtents = sweepBoxSize * 0.5f;
        Collider[] hitColliders = Physics.OverlapBox(
            sweepTransform.position,
            halfExtents,
            sweepTransform.rotation,
            ~0,
            QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hitColliders)
        {
            PlayerMovement pm = hit.GetComponentInParent<PlayerMovement>();
            if (pm == null)
            {
                continue;
            }

            var damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                damageable.TakeDamage(sweepMeleeDamage);
            }

            pm.ApplyImpulse(GetSweepImpulse(knockbackDirection, sweepTransform));

            // Once the player has been hit, break out of the loop so we don't accidentally hit them twice.
            break;
        }
    }

    private Vector3 GetSweepImpulse(int knockbackDirection, Transform sweepTransform)
    {
        Vector3 sweepRight = sweepTransform != null ? sweepTransform.right : transform.right;
        sweepRight.y = 0f;

        if (sweepRight.sqrMagnitude <= 0.0001f)
        {
            sweepRight = transform.right;
            sweepRight.y = 0f;
        }

        sweepRight.Normalize();

        if (knockbackDirection == 0) // Left)
        {
            return (-sweepRight * sweepSideKnockback) + (Vector3.up * sweepUpwardKnockback);
        }

        if (knockbackDirection == 1) // Right)
        {
            return (sweepRight * sweepSideKnockback) + (Vector3.up * sweepUpwardKnockback);
        }

        return Vector3.up * sweepUpwardKnockback;
    }

    /// <summary>
    /// Shoots out a spread of projectiles in a cone pattern towards one direction 
    /// </summary>
    public void RockBarrage()
    {
        if (target == null || rockBarrageProjectilePrefab == null) return;

        Vector3 spawnPos = barrageProjectileSpawnPoint != null 
            ? barrageProjectileSpawnPoint.position 
            : transform.position + Vector3.up * 1.5f;

        Vector3 targetPos = target.position + Vector3.up * barrageAimOffset; // Aim for center mass

        Vector3 fallbackDirection = targetPos - spawnPos;
        if (fallbackDirection.sqrMagnitude <= 0.0001f)
        {
            fallbackDirection = transform.forward;
        }

        Vector3 baseVelocity = TryCalculateBarrageLaunchVelocity(spawnPos, targetPos, barrageProjectileVelocity, out Vector3 solvedVelocity)
            ? solvedVelocity
            : fallbackDirection.normalized * Mathf.Max(0.1f, barrageProjectileVelocity);

        // Fire barrage
        for (int i = 0; i < barrageProjectileCount; i++)
        {
            // Get the direction of the calculated gravity arc
            Quaternion trajectoryRotation = Quaternion.LookRotation(baseVelocity);

            // Calculate the spread 
            float randomYaw = UnityEngine.Random.Range(-barrageSpreadAngle, barrageSpreadAngle);
            float randomPitch = UnityEngine.Random.Range(-barrageSpreadAngle / 2f, barrageSpreadAngle / 2f); // Divide by 2 to prevent shooting into the ground
            
            Quaternion spreadRotation = Quaternion.Euler(randomPitch, randomYaw, 0);

            Vector3 finalVelocity = (trajectoryRotation * spreadRotation) * Vector3.forward * baseVelocity.magnitude;

            // Spawn the projectile using pooling if available
            GameObject rockObj;
            if (ObjectPool.instance != null)
            {
                Transform spawnTransform = barrageProjectileSpawnPoint != null ? barrageProjectileSpawnPoint : transform;
                rockObj = ObjectPool.instance.GetObject(rockBarrageProjectilePrefab, spawnTransform);
                rockObj.transform.position = spawnPos;
                rockObj.transform.rotation = Quaternion.identity;
            }
            else
            {
                rockObj = Instantiate(rockBarrageProjectilePrefab, spawnPos, Quaternion.identity);
            }

            // Initialize the projectile 
            if (rockObj.TryGetComponent<EnemyRockBarrageProjectile>(out EnemyRockBarrageProjectile barrageRock))
            {
                barrageRock.Init(finalVelocity, barrageProjectileMask, barrageDirectDamage, barrageProjectileKnockback);
            }
        }
    }

    private bool TryCalculateBarrageLaunchVelocity(Vector3 startPoint, Vector3 targetPoint, float launchSpeed, out Vector3 launchVelocity)
    {
        launchSpeed = Mathf.Max(0.1f, launchSpeed);

        Vector3 displacement = targetPoint - startPoint;
        Vector3 displacementXZ = new Vector3(displacement.x, 0f, displacement.z);
        float distanceXZ = displacementXZ.magnitude;

        if (distanceXZ <= 0.01f)
        {
            launchVelocity = Vector3.up * Mathf.Sign(Mathf.Approximately(displacement.y, 0f) ? 1f : displacement.y) * launchSpeed;
            return true;
        }

        float gravity = Mathf.Abs(Physics.gravity.y);
        float speedSquared = launchSpeed * launchSpeed;
        float discriminant = (speedSquared * speedSquared)
            - gravity * ((gravity * distanceXZ * distanceXZ) + (2f * displacement.y * speedSquared));

        if (discriminant < 0f)
        {
            launchVelocity = Vector3.zero;
            return false;
        }

        float angle = Mathf.Atan((speedSquared - Mathf.Sqrt(discriminant)) / (gravity * distanceXZ));
        Vector3 horizontalDirection = displacementXZ / distanceXZ;
        float horizontalSpeed = launchSpeed * Mathf.Cos(angle);
        float verticalSpeed = launchSpeed * Mathf.Sin(angle);

        launchVelocity = (horizontalDirection * horizontalSpeed) + (Vector3.up * verticalSpeed);
        return true;
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

        // Draw the melee sphere
        if (meleePosition != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(meleePosition.position, meleeRadius);
        }

        // Draw the melee sweep box
        Transform sweepTransform = sweepPosition != null ? sweepPosition : transform;
        if (sweepTransform != null)
        {
            Gizmos.color = Color.red;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(sweepTransform.position, sweepTransform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, sweepBoxSize);
            Gizmos.matrix = previousMatrix;
        }
    }
}
