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


    public override void Start()
    {
        base.Start();

        orbitVisuals = GetComponent<EnemyRangeOrbitVisuals>();
        if (agent != null)
        {
            agent.speed = chaseSpeed;
            // agent.stoppingDistance = stopDistance * 0.8f;
            agent.stoppingDistance = 0f; // control stopping manually

            // agent.angularSpeed = agentAngularSpeed;
            // agent.acceleration = agentAcceleration;
            // agent.autoBraking = true;
            agent.updatePosition = false;
            agent.updateRotation = true; // let agent handle the rotation on Y axis for now
        }

        idle = new IdleState_Range(this, stateMachine);
        chase = new ChaseState_Range(this, stateMachine);
        attack = new AttackState_Range(this, stateMachine);
        recovery = new RecoveryState_Range(this, stateMachine);

        stateMachine.Initialize(idle); // enter idle first

    }

    public override void Update()
    {
        base.Update();

        UpdateHover();
        UpdateFlightMovement();
    }

    /// <summary>
    /// Manually move the Transform to match the AGENT X and Z, but override the Y for flight control
    /// </summary>
    void UpdateFlightMovement()
    {
        if (agent == null) return;
        if (target == null) return;

        // find the target height
        float targetY = target.position.y + flyHeight;

        // find horizontal target
        Vector3 nextPos = transform.position;
        Vector3 desiredHorizontalPos;

        // check for line of sight to target
        Vector3 dirToTarget = target.position - transform.position;
        float distToTarget = dirToTarget.magnitude;

        // raycast check
        bool hasLineOfSight = !Physics.Raycast(transform.position, dirToTarget.normalized, distToTarget, obstacleMask); // check if no wall in between

        if (hasLineOfSight)
        {
            // DO TRUE FLIGHT, IGNORE SHITTY NAVMESH
            desiredHorizontalPos = target.position;
            if (agent.isOnNavMesh)
            {
                agent.nextPosition = transform.position;
            }
        }
        else
        {
            // go back to navmesh
            desiredHorizontalPos = agent.nextPosition;
        }

        nextPos.x = Mathf.SmoothDamp(transform.position.x, desiredHorizontalPos.x, ref currentHorizontalVelocity.x, horizontalSmoothTime);
        nextPos.z = Mathf.SmoothDamp(transform.position.z, desiredHorizontalPos.z, ref currentHorizontalVelocity.z, horizontalSmoothTime);

        // vertical move, try to match the target's height
        nextPos.y = Mathf.SmoothDamp(transform.position.y, targetY, ref currentYVelocity, verticalSmoothTime);

        // Apply
        transform.position = nextPos;
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
        if(agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    public void SafeResumeAgent()
    {
        if(agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled)
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
