using UnityEngine;
using UnityEngine.AI;

public class EnemyBoss_SS : Enemy
{
    [Header("Bomb Attack Settings")]
    public GameObject expEnemyPrefab;  // explodies
    public Transform[] spawnPoints;
    public int bombsPerCycle = 3; // number of slimes per bomb state
    public float bombSpawnInterval = 4f;
    public float idleTime = 1f;
    public Transform firePoint;
    public float slimeArcDistance;
    public float slimeArcDuration;

    [Header("Ring Attack Settings")]
    public float maxRadius;
    public float duration;
    public float explosionDamage;
    public float explosionCameraShakeForce = 10f;
    public GameObject poofVFX;

    public GameObject explosionVFXPrefab;
    public GameObject shockwaveVFXPrefab;

    [Space]

    [Header("Leap Attack")]
    [SerializeField] private EnemyMeleeHitbox hitbox;
    [HideInInspector] public bool hitAppliedThisAttack;
    public float slamDamage = 3f;
    public float minRequiredPlayerDistance = 90f; // to ensure our boss does not leap like very far
    public float playerTooClose = 10f; // to ensure our boss does not leap when its too close

    public float knockbackPower = 3f;
    public float windupTime = 0.25f;
    public float leapDuration = 0.6f;
    public float leapHeight = 3f;
    public float leapOverShootDistance = 2f;
    public float gravityScale = 3f;
    public float startHeightAboveGround = 0.05f;
    [Tooltip("Max height to player allowed for leap attack")]
    public float maxAttackHeightDiff = 4f;
    public LayerMask groundMask = ~0;

    [Header("VFX")]
    public GameObject flashVFX;
    public GameObject jumpVFX;
    public Transform jumpVFXPoint;
    public Transform height; // the animator component is under here

    [Header("Sweep Collision")]
    public float collisionRadius = 0.5f;

    [HideInInspector] public bool isInAir;
    [HideInInspector] public Vector3 inAirVelocity;

    private Animator heightAnimator;

    private IdleState_Boss idle;
    private SpawnBombState_Boss bombState;
    private RecoveryState_Boss recovery;
    private RingAttackState_Boss ringAttack;
    private LeapAttackState_Boss leapAttack;


    public override void Start()
    {
        base.Start();

        if (height != null)
            heightAnimator = height.GetComponent<Animator>();

        idle = new IdleState_Boss(this, stateMachine);
        bombState = new SpawnBombState_Boss(this, stateMachine);
        recovery = new RecoveryState_Boss(this, stateMachine);
        ringAttack = new RingAttackState_Boss(this, stateMachine, maxRadius, duration, explosionDamage, playerMask);
        leapAttack = new LeapAttackState_Boss(this, stateMachine);
        stateMachine.Initialize(idle);
    }

    public override void Die()
    {
        // exit the current state so clean up happen, destroy the Ring VFX thing
        stateMachine.currentState?.Exit();

        base.Die();
    }

    public EnemyState GetIdle() => idle;
    public EnemyState GetBombState() => bombState;
    public EnemyState GetRecoveryState() => recovery;
    public EnemyState GetExploisionState() => ringAttack;
    public EnemyState GetLeapAttackState() => leapAttack;



    public void CreatePoofVFX(Vector3 spawnPosition)
    {
        PlayPSVFX(poofVFX, spawnPosition);
    }

    public void CreateVFX(GameObject vfxPrefab, Vector3 spawnPosition, float destroyAfter)
    {
        PlayPSVFX(vfxPrefab, spawnPosition);
    }


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
        if (toPlayer.sqrMagnitude < 0.0001f) return;

        toPlayer.Normalize();

        var pm = playerCol.GetComponentInParent<PlayerMovement>();
        if (pm != null)
        {
            pm.ApplyImpulse(toPlayer * knockbackPower);

            var damageable = pm.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                damageable.TakeDamage(slamDamage);
            }
        }
        hitAppliedThisAttack = true;
        nextAttackAllowed = Time.time + attackCooldown;
        EnableHitBox(false);
    }

    public void PlayPSVFX(GameObject vfxPrefab, Transform spawnPos)
    {
        if (vfxPrefab == null) return;

        spawnPos = spawnPos != null ? spawnPos : transform;
        PlayPSVFXInternal(vfxPrefab, spawnPos.position);
    }

    /// <summary>
    /// takes a world position directly.
    /// </summary>
    public void PlayPSVFX(GameObject vfxPrefab, Vector3 position)
    {
        if (vfxPrefab == null) return;
        PlayPSVFXInternal(vfxPrefab, position);
    }

    private void PlayPSVFXInternal(GameObject vfxPrefab, Vector3 position)
    {
        GameObject fx;
        if (ObjectPool.instance != null)
        {
            fx = ObjectPool.instance.GetObject(vfxPrefab, transform);
        }
        else
        {
            fx = Instantiate(vfxPrefab);
        }
        fx.transform.position = position;
        fx.transform.rotation = Quaternion.identity;

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



    public bool IsPlayerTooFar()
    {
        return Vector3.Distance(transform.position, target.position) > minRequiredPlayerDistance;
    }

    public bool IsPlayerTooClose()
    {
        return Vector3.Distance(transform.position, target.position) <= playerTooClose;
    }

    public bool IsPlayerTooHighOrLow()
    {
        if (target == null) return true;
        return Mathf.Abs(target.position.y - transform.position.y) > maxAttackHeightDiff;
    }

    // SweptCollision helpers

    public Vector3 CalculateBallisticVelocity(Vector3 startPoint, Vector3 endPoint, float height, out float duration)
    {
        float gravity = Physics.gravity.y * gravityScale;

        // Flatten target Y so leapHeight alone controls the arc
        endPoint.y = startPoint.y;
        float displacementY = 0f;

        Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0, endPoint.z - startPoint.z);

        float velocityY = Mathf.Sqrt(-2f * gravity * height);
        float timeToPeak = -velocityY / gravity;
        float timeToFall = Mathf.Sqrt(2f * (displacementY - height) / gravity);
        duration = timeToPeak + timeToFall;

        Vector3 velocityXZ = displacementXZ / duration;
        return velocityXZ + Vector3.up * velocityY;
    }

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
            float safeDist = Mathf.Max(0f, sweepHit.distance - 0.01f);
            transform.position += dir * safeDist;

            if (sweepHit.normal.y > 0.6f)
            {
                float halfH = agent != null ? agent.height * 0.7f : 0f;
                Vector3 pos = transform.position;
                pos.y = sweepHit.point.y + halfH + startHeightAboveGround;
                transform.position = pos;
            }

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

    public bool GroundCheck(out Vector3 groundPoint, float probeDepth = 0.3f)
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

    public void PauseAgent()
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.ResetPath();
        agent.updatePosition = false;
        agent.updateRotation = false;
    }

    public void ResumeAgent()
    {
        if (agent == null || !agent.isActiveAndEnabled) return;

        // Find ground first, then NavMesh
        Vector3 correctedPos = transform.position;
        Vector3 rayOrigin = correctedPos + Vector3.up * 5f;
        float halfH = agent != null ? agent.height * 0.7f : 0f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit gHit, 10f,
                groundMask, QueryTriggerInteraction.Ignore))
        {
            correctedPos.y = gHit.point.y + halfH + startHeightAboveGround;
        }

        if (NavMesh.SamplePosition(correctedPos, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
        {
            Vector3 finalPos = navHit.position;
            finalPos.y = correctedPos.y;
            transform.position = finalPos;
            agent.Warp(finalPos);
        }
        else
        {
            transform.position = correctedPos;
            agent.Warp(correctedPos);
        }

        agent.nextPosition = transform.position;
        agent.velocity = Vector3.zero;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.isStopped = true;
        agent.ResetPath();
    }



    // Animator thigns 

    public void TriggerSquish()
    {
        if (heightAnimator != null)
            heightAnimator.SetTrigger("squish");
    }

    public void TriggerStretch()
    {
        if (heightAnimator != null)
            heightAnimator.SetTrigger("stretch");
    }

    public void SetIsJumping(bool value)
    {
        if (heightAnimator != null)
            heightAnimator.SetBool("isJumping", value);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = aggressionColor;
        Gizmos.DrawWireSphere(transform.position, maxRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, playerTooClose);

    }
}