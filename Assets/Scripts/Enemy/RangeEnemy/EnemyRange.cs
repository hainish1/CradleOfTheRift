using UnityEngine;


/// <summary>
/// Class - Represents a Range Enemy, inherits from Base Enemy class 
/// ,also defines functionality of its own
/// </summary>
public class EnemyRange : Enemy
{
    public float projectileDamage = 1;

    [Header("Flight Settings")]
    public float flyHeight = 3f;
    public float verticalSmoothTime = 0.3f; // how fast I adjust height
    public float horizontalSmoothTime = 0.1f; // sync with agent speed

    [Header("Hover and movement")]
    public float chaseSpeed = 3.5f;
    public float attackRange = 12f; // start shooting from this far
    public float desiredDistance = 6f; // try to stay this much away from player
    public float stopDistance = 7f; // how far away from player should it stop 
    public float hoverHeight = 2f; // height above ground
    public float hoverBobAmplitude = 0.25f; // up n down
    public float hoverBobSpeed = 2f;
    public float turnSpeedWhileAiming = 12f;
    public float agentAngularSpeed = 720f;
    public float agentAcceleration = 100f;

    [Header("Spacing / Anti-clump")]
    public float spreadInterval = 0.2f;
    public float spreadRadiusJitter = 2f;
    public float wispSeparationRadius = 4f;
    public float wispSeparationStrength = 1.2f;
    public float navSampleDistance = 2f;

    [Space]

    [Header("Shooting")]
    public float projectileSpeed = 50f;
    public float fireCooldown = .6f;
    public float recoveryDuration = 1f; // pause after shooting before chasing again
    [Space]
    public Transform firePoint; // where bullet come from
    public EnemyProjectile projectilePrefab;
    public float spawnOffset = 0.1f; // a little away fro fire point, safety
    public LayerMask projectileMask = ~0;
    public LayerMask obstacleMask = 1; // to detect walls

    [Space]

    [Header("Reccovery")]
    [Tooltip("After all orbs are finished, how much time to start again, basically reload time")]
    public float recoveryTime = 0.4f;

    IdleState_Range idle;
    ChaseState_Range chase;
    AttackState_Range attack;
    RecoveryState_Range recovery;

    float bobPhase;
    public EnemyRangeOrbitVisuals orbitVisuals;

    // internal state
    private float currentYVelocity; // for smoothDamp
    private Vector3 currentHorizontalVelocity; // for smoothDamp
    public float nextShootTime { get; set; } // used by state

    private float spreadAngleRad;
    private float spreadRadiusOffset;
    private float nextSpreadUpdateTime;
    private Vector3 cachedSpreadPoint;
    private Vector3 cachedSeparation;
    private bool holdHorizontalPosition;


    public override void Start()
    {
        base.Start();

        InitializeSpread();

        orbitVisuals = GetComponent<EnemyRangeOrbitVisuals>();
        if (agent != null)
        {
            agent.speed = chaseSpeed;
            // agent.stoppingDistance = stopDistance * 0.8f;
            agent.stoppingDistance = 0f; // control stopping manually
            agent.updatePosition = false;
            agent.updateRotation = true; // let agent handle the rotation on Y axis for now
        }

        var kb = GetComponent<AgentKnockBack>();
        if (kb != null) kb.manageAgentPosition = false;

        idle = new IdleState_Range(this, stateMachine);
        chase = new ChaseState_Range(this, stateMachine);
        attack = new AttackState_Range(this, stateMachine);
        recovery = new RecoveryState_Range(this, stateMachine);

        stateMachine.Initialize(idle); // enter idle first

    }



    public override void Update()
    {
        base.Update();

        // UpdateHover();
        UpdateFlightMovement();
    }

    /// <summary>
    /// Manually move the Transform to match the AGENT X and Z, but override the Y for flight control
    /// Also handles Bobbing
    /// </summary>
    void UpdateFlightMovement()
    {
        if (agent == null) return;

        float baseTargetY = transform.position.y;
        if (target != null)
        {
            baseTargetY = target.position.y + flyHeight;
        }
        // if (target == null) return;

        // Add Bobbing
        bobPhase += Time.deltaTime * hoverBobSpeed;
        float bobOffset = Mathf.Sin(bobPhase) * hoverBobAmplitude;
        float finalTargetY = baseTargetY + bobOffset;

        // // find the target height
        // float targetY = target.position.y + flyHeight;

        // find horizontal target
        Vector3 desiredHorizontalPos = transform.position;

        if (agent.isOnNavMesh)
        {
            desiredHorizontalPos = agent.nextPosition;
        }

        if (target != null)
        {
            // check for line of sight to target
            Vector3 dirToTarget = target.position - transform.position;
            float distToTarget = dirToTarget.magnitude;

            // raycast check
            bool hasLineOfSight = !Physics.Raycast(transform.position, dirToTarget.normalized, distToTarget, obstacleMask); // check if no wall in between

            if (hasLineOfSight)
            {
                // DO TRUE FLIGHT, IGNORE SHITTY NAVMESH
                desiredHorizontalPos = GetSpreadoutChasePoint();
                if (agent.isOnNavMesh)
                {
                    agent.nextPosition = transform.position;
                }
            }
        }

        // Apply Smoothing
        Vector3 nextPos = transform.position;

        nextPos.x = Mathf.SmoothDamp(transform.position.x, desiredHorizontalPos.x, ref currentHorizontalVelocity.x, horizontalSmoothTime);
        nextPos.z = Mathf.SmoothDamp(transform.position.z, desiredHorizontalPos.z, ref currentHorizontalVelocity.z, horizontalSmoothTime);

        // vertical move, try to match the target's height
        nextPos.y = Mathf.SmoothDamp(transform.position.y, finalTargetY, ref currentYVelocity, verticalSmoothTime);

        // Apply
        transform.position = nextPos;
    }


    public void FireAtTarget()
    {
        if (target == null || firePoint == null || projectilePrefab == null) return;

        Vector3 aimPosition = target.position + Vector3.up * 0.5f; // Aim at chest of player


        Vector3 direction = (aimPosition - firePoint.position).normalized;

        Quaternion rotation = Quaternion.LookRotation(direction);
        Vector3 spawnPoint = firePoint.position + direction * spawnOffset;

        // I could probably use Object Pooling here
        EnemyProjectile projectile = Instantiate(projectilePrefab, spawnPoint, rotation);
        projectile.Init(direction * projectileSpeed, projectileMask, projectileDamage);

        // Handle Visuals
        if (orbitVisuals != null)
        {
            int orbIndex = orbitVisuals.GetNextVisibleOrbIndex();
            if (orbIndex >= 0) orbitVisuals.HideOrb(orbIndex);
        }
    }

    public void SafeStopAgent()
    {
        if (agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    public void SafeResumeAgent()
    {
        if (agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
        }
    }


    public void FaceTargetSmooth(float speed)
    {
        if (target == null) return;
        Vector3 dir = (target.position - transform.position).normalized;
        dir.y = 0; // Keep rotation upright
        if (dir == Vector3.zero) return;

        Quaternion lookRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * speed);
    }



    /// <summary>
    /// Used to fire one projectile in direction of te target
    /// </summary>
    public void FireOnce()
    {
        if (!firePoint || !projectilePrefab) return;

        Vector3 direction = (target ? (target.position + Vector3.up * .5f) - firePoint.position : transform.forward).normalized;

        Vector3 spawnPoint = firePoint.position + direction * spawnOffset;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        EnemyProjectile projectile = Instantiate(projectilePrefab, spawnPoint, rotation);
        projectile.Init(direction * projectileSpeed, projectileMask, this.projectileDamage);

        if (orbitVisuals != null)
        {
            int orbIndex = orbitVisuals.GetNextVisibleOrbIndex();
            if (orbIndex >= 0)
            {
                orbitVisuals.HideOrb(orbIndex);
            }
            else
            {
                // no orbs left,maybe i can go to recovery
            }
        }
    }


    public void SetHorizontalPosition(bool hold)
    {
        holdHorizontalPosition = hold;
    }

    public Vector3 GetSpreadoutChasePoint()
    {
        if (target == null) return transform.position;

        if (holdHorizontalPosition)
        {
            Vector3 here = transform.position;
            here.y = target.position.y;
            return here;
        }

        if (Time.time < nextSpreadUpdateTime) return cachedSpreadPoint;
        nextSpreadUpdateTime = Time.time + Mathf.Max(0.05f, spreadInterval);

        Vector3 targetPos = target.position;
        float radius = Mathf.Max(0.25f, desiredDistance + spreadRadiusOffset);
        Vector3 ringOffset = new Vector3(Mathf.Cos(spreadAngleRad), 0f, Mathf.Sin(spreadAngleRad)) * radius;

        cachedSeparation = ComputeWispSeparation();
        cachedSpreadPoint = targetPos + ringOffset + cachedSeparation;
        cachedSpreadPoint.y = targetPos.y;

        return cachedSpreadPoint;
    }

    Vector3 ComputeWispSeparation()
    {
        if (wispSeparationRadius <= 0f || wispSeparationStrength <= 0f) return Vector3.zero;

        Collider[] hits = Physics.OverlapSphere(transform.position, wispSeparationRadius);
        if (hits == null || hits.Length == 0) return Vector3.zero;

        Vector3 push = Vector3.zero;
        int count = 0;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i];
            if (c == null) continue;
            if (c.attachedRigidbody != null && c.attachedRigidbody.gameObject == gameObject) continue;
            if (c.gameObject == gameObject) continue;

            EnemyRange other = c.GetComponentInParent<EnemyRange>();
            if (other == null) continue;

            Vector3 delta = transform.position - other.transform.position;
            delta.y = 0f;
            float d = delta.magnitude;
            if (d < 0.0001f) continue;

            float t = 1f - Mathf.Clamp01(d / wispSeparationRadius);
            push += (delta / d) * t;
            count++;
        }

        if (count == 0) return Vector3.zero;

        push /= count;
        push *= wispSeparationStrength;
        push.y = 0f;
        return push;
    }

    void InitializeSpread()
    {
        int id = GetInstanceID();
        float h1 = Mathf.Abs(Mathf.Sin(id * 13f) * 10000f);
        float h2 = Mathf.Abs(Mathf.Sin(id * 47f) * 10000f);
        h1 = h1 - Mathf.Floor(h1);
        h2 = h2 - Mathf.Floor(h2);

        // give each enemy their own unique spot
        spreadAngleRad = h1 * Mathf.PI * 2f;
        spreadRadiusOffset = (h2 - 0.5f) * spreadRadiusJitter;
        nextSpreadUpdateTime = 0f;
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


    // HELPERS

    public EnemyState GetIdle() => idle;
    public EnemyState GetChase() => chase;
    public EnemyState GetAttack() => attack;
    public EnemyState GetRecovery() => recovery;

    void OnDrawGizmos()
    {
        Gizmos.color = aggressionColor;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }


}
