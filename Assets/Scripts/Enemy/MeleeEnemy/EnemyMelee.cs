// using UnityEditor.UI;
using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Class - Represents a Melee Enemy, inherits from Base Enemy class 
/// and defines functionality of its own
/// </summary>
public class EnemyMelee : Enemy
{
    [Header("Melee related stuff")]
    public float chaseSpeed = 4f;
    public float attackRange = 1.2f;
    // public int damage = 10; 
    public float knockbackPower = 10f; // how far can push the enemy
    public float recoveryTime { get; private set; } = 0.25f;

    [Header("Slime drag stuff")]
    public float dragSpeed = 6f;
    public float dragDuration = 0.35f;
    public float restDuration = 0.25f;


    [Header("Slam attack")]
    public float slamDamage = 1;
    public float windupTime = .15f;
    public float chargeSpeed = 12f;
    public float chargeTime = .18f;

    [Header("AttackHitbox")]
    [SerializeField] private EnemyMeleeHitbox hitbox;
    [HideInInspector] public bool hitAppliedThisAttack;

    [Header("Leap Attack Settings")]
    public float leapAttackRange = 5f; // distance to start leap
    public float minAttackDistance = 3f; // min safe dist
    public float leapHeight = 1f; // vertical arc height above start point
    public float leapDuration = .5f; // time for leap
    public float leapOverShootDistance = 4f; // How far past the player to jump
    public float gravityScale = 4f;
    public float startHeightAboveGround = .05f;
    [Tooltip("Max height to player allowed for attacking.")]
    public float maxAttackHeightDiff = 2f;
    public LayerMask groundMask = ~0; // to detect what is ground

    [Header("Sweep Collision")]
    public float collisionRadius = 0.25f;

    [HideInInspector] public bool isInAir;
    [HideInInspector] public Vector3 inAirVelocity;

    IdleState_Melee idle;
    ChaseState_Melee chase;
    AttackState_Melee attack;
    RecoveryState_Melee recovery;

    [Header("Jump Sound Effect")]
    // The sound effect of the slime jumping at the player
    [SerializeField]
    private AK.Wwise.Event jumpSFX;
    [Header("Jump VFX")]
    public GameObject jumpPoofVFXPrefab;
    public Transform jumpVFXAttackPoint;

    public override void Start()
    {
        base.Start(); // run stuff that we wrote in base enemy class first

        Debug.Log($"[Melee Spawn] posY={transform.position.y} isOnNavMesh={agent.isOnNavMesh}");
        SnapToNavMesh();
        Debug.Log($"[Melee PostSnap] posY={transform.position.y} isOnNavMesh={agent.isOnNavMesh}");

        // Debug.Log($"onMesh={agent.isOnNavMesh} onLink={agent.isOnOffMeshLink} posY={transform.position.y}");

        agent.speed = chaseSpeed;

        var kb = GetComponent<AgentKnockBack>();
        if (kb != null) kb.manageAgentPosition = true; // just in case yk

        idle = new IdleState_Melee(this, stateMachine);
        chase = new ChaseState_Melee(this, stateMachine);
        attack = new AttackState_Melee(this, stateMachine);
        recovery = new RecoveryState_Melee(this, stateMachine);

        stateMachine.Initialize(idle);
    }

    /// <summary>
    /// Enable the hitbox used to detect gameobjects that this enemy hit
    /// </summary>
    /// <param name="enable"></param>
    public void EnableHitBox(bool enable)
    {
        if (hitbox != null && hitbox.gameObject.activeSelf != enable)
        {
            hitbox.gameObject.SetActive(enable);
        }
    }

    /// <summary>
    /// Try to apply damage and impulse to the player GameObject caught in colliders 
    /// </summary>
    /// <param name="playerCol"></param>
    public void TryApplyHit(Collider playerCol)
    {
        if (hitAppliedThisAttack) return;
        if (Time.time < nextAttackAllowed) return;

        Vector3 toPlayer = playerCol.transform.position - transform.position;
        toPlayer.y = 0f;

        var pm = playerCol.GetComponentInParent<PlayerMovement>();
        if (pm != null)
        {
            pm.ApplyImpulse(toPlayer.normalized * knockbackPower);

            var damageable = pm.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                damageable.TakeDamage(slamDamage);
            }
        }
        hitAppliedThisAttack = true;
        nextAttackAllowed = Time.time + attackCooldown; // ehhh do I need this here
        EnableHitBox(false);

    }

    /// <summary>
    /// Initialize damage done by this enemy, can be updated by enemy spawner
    /// </summary>
    /// <param name="newDamage"></param>
    public void InitializeSlamDamage(float newDamage)
    {
        // this.slamDamage = Mathf.CeilToInt(newDamage);
        this.slamDamage = newDamage;
        Debug.Log("Slam Damage: " + this.slamDamage);
    }

    public Vector3 CalculateBallisticVelocity(Vector3 startPoint, Vector3 endPoint, float height, out float duration)
    {
        float gravity = Physics.gravity.y * gravityScale;

        // Flatten the target to same Y 
        endPoint.y = startPoint.y;
        float displacementY = 0f;

        Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0, endPoint.z - startPoint.z);

        // vertical velocity: v = sqrt(-2gh)
        float velocityY = Mathf.Sqrt(-2 * gravity * height);

        // time calcs
        float timeToPeak = -velocityY / gravity;
        float timeToFall = Mathf.Sqrt(2 * (displacementY - height) / gravity);

        duration = timeToPeak + timeToFall; // total flight time

        // horizontal velocity
        Vector3 velocityXZ = displacementXZ / duration;

        return velocityXZ + Vector3.up * velocityY;
    }

    private void SnapToNavMesh()
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
    /// Move with swept-sphere collision
    /// </summary>
    public Vector3 SweepMove(Vector3 vel, float dt)
    {
        Vector3 delta = vel * dt;
        float dist = delta.magnitude;
        if (dist < 1e-5f) return vel;

        Vector3 origin = transform.position + Vector3.up * (collisionRadius + 0.02f);
        Vector3 dir = delta.normalized;

        if (Physics.SphereCast(origin, collisionRadius, dir, out RaycastHit sweepHit,
                dist, groundMask, QueryTriggerInteraction.Ignore))
        {
            // Stop just before the hit surface
            float safeDist = Mathf.Max(0f, sweepHit.distance - 0.01f);
            transform.position += dir * safeDist;

            // Ground hit, snap center to correct height above surface
            if (sweepHit.normal.y > 0.6f)
            {
                float halfH = agent != null ? agent.height * 0.7f : 0f;
                Vector3 pos = transform.position;
                pos.y = sweepHit.point.y + (halfH + startHeightAboveGround);
                transform.position = pos;
            }

            // remove the component going into the surface
            float velIntoSurface = Vector3.Dot(vel, -sweepHit.normal);
            if (velIntoSurface > 0f)
                vel += sweepHit.normal * velIntoSurface;
        }
        else
        {
            transform.position += delta;
        }

        return vel;
    }

    /// <summary>
    /// Check if ground is within depth below the enemy's feet
    /// </summary>
    public bool GroundCheck(out Vector3 groundPoint, float probeDepth = 0.15f)
    {
        float startHeight = collisionRadius + 0.1f;

        Vector3 origin = transform.position + Vector3.up * startHeight;
        float castDist = probeDepth + startHeight;

        if (Physics.SphereCast(origin, collisionRadius * 0.4f, Vector3.down, out RaycastHit groundHit,
                castDist, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundPoint = groundHit.point;
            return true;
        }

        groundPoint = transform.position;
        return false;
    }

    /// <summary>
    /// Pause NavMeshAgent steering for manual position control (leaps, knockback)
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

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
        {
            transform.position = navHit.position;
            agent.Warp(navHit.position);
        }
        else
        {
            agent.Warp(transform.position);
        }

        agent.nextPosition = transform.position;
        agent.velocity = Vector3.zero;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.isStopped = true;
        agent.ResetPath();
    }

    //Getters for States that this Melee Enemy has
    public EnemyState GetIdle() => idle;
    public EnemyState GetChase() => chase;
    public EnemyState GetAttack() => attack;
    public EnemyState GetRecovery() => recovery;

    /// <summary>
    /// Get Base Damage for this enemy
    /// </summary>
    /// <returns></returns>
    public float GetBaseDamage() => slamDamage;



    void OnDrawGizmos()
    {
        Gizmos.color = aggressionColor;
        Gizmos.DrawWireSphere(transform.position, minAttackDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    public void PlayJumpSFX()
    {
        this.jumpSFX.Post(gameObject);
    }

    public void PlayMeleePSVFX(GameObject vfxPrefab, Transform spawnPos)
    {
        if (vfxPrefab == null) return;

        spawnPos = spawnPos != null ? spawnPos : transform;

        GameObject fx;
        if (ObjectPool.instance != null)
        {
            fx = ObjectPool.instance.GetObject(vfxPrefab, spawnPos);
        }
        else
        {
            fx = Instantiate(vfxPrefab, spawnPos.position, Quaternion.identity, spawnPos);
        }

        float lifetime = EstimateParticleLifetime(fx);

        if (ObjectPool.instance != null)
        {
            ObjectPool.instance.ReturnObject(fx, lifetime);
        }
        else
        {
            Destroy(fx, lifetime);
        }
    }

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
}
