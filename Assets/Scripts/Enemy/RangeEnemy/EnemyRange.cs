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
    public float horizontalSmoothTime = 0.15f; // sync with agent speed
    public float maxHorizontalSpeed = 12f; // cap prevents runaway drift
    public float maxVerticalSpeed = 10f; // cap vertical speed

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
    public float wispSeparationRadius = 5f;
    public float wispSeparationStrength = 1.5f;
    public float navSampleDistance = 2f;
    public float angleAdaptSpeed = 1.5f;

    [Space]

    [Header("Shooting")]
    public float projectileSpeed = 50f;
    public float fireCooldown = .6f;
    public float recoveryDuration = 1f; // pause after shooting before chasing again
    [Space]
    public Transform firePoint; // where bullet come from
    public EnemyProjectile projectilePrefab;
    public float spawnOffset = 0.1f; // a little away fro fire point, safety

    [SerializeField]
    private AK.Wwise.Event shootSFX;

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
    private bool wasUsingDirectFlight;


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
        if (PauseManager.GameIsPaused) return;

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
        if (PauseManager.GameIsPaused) return;
        if (agent == null) return;

        // --- Vertical target ---
        float baseTargetY = transform.position.y;
        if (target != null)
        {
            baseTargetY = target.position.y + flyHeight;
        }

        bobPhase += Time.deltaTime * hoverBobSpeed;
        float bobOffset = Mathf.Sin(bobPhase) * hoverBobAmplitude;
        float finalTargetY = baseTargetY + bobOffset;

        // --- Horizontal target ---
        Vector3 desiredHorizontalPos = transform.position;
        bool usingDirectFlight = false;

        if (target != null)
        {
            Vector3 dirToTarget = target.position - transform.position;
            float distToTarget = dirToTarget.magnitude;
            bool hasLineOfSight = !Physics.Raycast(transform.position, dirToTarget.normalized,
                                                    distToTarget, obstacleMask);

            if (hasLineOfSight)
            {
                usingDirectFlight = true;
                desiredHorizontalPos = GetSpreadoutChasePoint();
            }
            else if (agent.isOnNavMesh)
            {
                desiredHorizontalPos = agent.nextPosition;
            }
        }
        else if (agent.isOnNavMesh)
        {
            desiredHorizontalPos = agent.nextPosition;
        }

        // reset SmoothDamp velocity when switching from navmesh and flight
        if (usingDirectFlight != wasUsingDirectFlight)
        {
            currentHorizontalVelocity = Vector3.zero;
            wasUsingDirectFlight = usingDirectFlight;
        }

        // Always keep the agent synced to our actual position so there is
        if (agent.isOnNavMesh)
        {
            agent.nextPosition = new Vector3(transform.position.x, agent.nextPosition.y, transform.position.z);
        }

        Vector3 nextPos = transform.position;

        nextPos.x = Mathf.SmoothDamp(transform.position.x, desiredHorizontalPos.x,
                                      ref currentHorizontalVelocity.x, horizontalSmoothTime);
        nextPos.z = Mathf.SmoothDamp(transform.position.z, desiredHorizontalPos.z,
                                      ref currentHorizontalVelocity.z, horizontalSmoothTime);

        // Cap horizontal speed to prevent runaway drift
        Vector3 hDelta = new Vector3(nextPos.x - transform.position.x, 0f, nextPos.z - transform.position.z);
        float maxStep = maxHorizontalSpeed * Time.deltaTime;
        if (hDelta.sqrMagnitude > maxStep * maxStep)
        {
            hDelta = hDelta.normalized * maxStep;
            nextPos.x = transform.position.x + hDelta.x;
            nextPos.z = transform.position.z + hDelta.z;
            currentHorizontalVelocity = Vector3.ClampMagnitude(currentHorizontalVelocity, maxHorizontalSpeed);
        }

        // Vertical move
        nextPos.y = Mathf.SmoothDamp(transform.position.y, finalTargetY, ref currentYVelocity, verticalSmoothTime);

        float vDelta = nextPos.y - transform.position.y;
        float maxVStep = maxVerticalSpeed * Time.deltaTime;
        if (Mathf.Abs(vDelta) > maxVStep)
        {
            nextPos.y = transform.position.y + Mathf.Sign(vDelta) * maxVStep;
            currentYVelocity = Mathf.Clamp(currentYVelocity, -maxVerticalSpeed, maxVerticalSpeed);
        }

        // apply all 
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


        shootSFX.Post(gameObject);

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
        Vector3 dir = (target.position - transform.position);
        dir.y = 0; // Keep rotation upright
        if (dir.sqrMagnitude < 0.0001f) return;

        dir.Normalize();
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

        shootSFX.Post(gameObject);

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
            Vector3 seperation = ComputeWispSeparation();
            here.x += seperation.x;
            here.z += seperation.z;
            here.y = target.position.y;
            return here;
        }

        if (Time.time < nextSpreadUpdateTime) return cachedSpreadPoint;
        nextSpreadUpdateTime = Time.time + Mathf.Max(0.05f, spreadInterval);

        AdaptSpreadAngle();

        Vector3 targetPos = target.position;
        float radius = Mathf.Max(0.25f, desiredDistance + spreadRadiusOffset);
        Vector3 ringOffset = new Vector3(Mathf.Cos(spreadAngleRad), 0f, Mathf.Sin(spreadAngleRad)) * radius;

        cachedSeparation = ComputeWispSeparation();
        cachedSpreadPoint = targetPos + ringOffset + cachedSeparation;
        cachedSpreadPoint.y = targetPos.y;

        return cachedSpreadPoint;
    }

    void AdaptSpreadAngle()
    {
        if (target == null || angleAdaptSpeed <= 0f) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, wispSeparationRadius);
        if (hits == null || hits.Length == 0) return;

        Vector3 targetPos = target.position;
        float myAngle = Mathf.Atan2(transform.position.z - targetPos.z, transform.position.x - targetPos.x);

        float angularPush = 0f;
        int count = 0;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i];
            if (c == null) continue;
            if (c.gameObject == gameObject) continue;
            if (c.attachedRigidbody != null && c.attachedRigidbody.gameObject == gameObject) continue;

            EnemyRange other = c.GetComponentInParent<EnemyRange>();
            if (other == null) continue;

            float otherAngle = Mathf.Atan2(other.transform.position.z - targetPos.z,
                                            other.transform.position.x - targetPos.x);

            float diff = Mathf.DeltaAngle(otherAngle * Mathf.Rad2Deg, myAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            float threshold = Mathf.PI / 3f;
            if (Mathf.Abs(diff) < threshold)
            {
                float pushDir = diff >= 0f ? 1f : -1f;
                float strength = 1f - Mathf.Clamp01(Mathf.Abs(diff) / threshold);
                angularPush += pushDir * strength;
                count++;
            }
        }

        if (count > 0)
        {
            angularPush /= count;
            spreadAngleRad += angularPush * angleAdaptSpeed * spreadInterval;
        }
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

        // Clamp separation so it can't cause explosive pushignb
        push = Vector3.ClampMagnitude(push, wispSeparationStrength * 2f);
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
