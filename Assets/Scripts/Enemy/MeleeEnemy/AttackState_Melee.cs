using UnityEngine;

/// <summary>
/// Class - Represents the Attack State for Melee Enemy.
/// Uses swept-sphere collision during the leap
/// </summary>
public class AttackState_Melee : EnemyState
{
    private EnemyMelee enemyMelee;
    private AgentKnockBack knockBack;

    private enum Phase { Windup, Leap }
    private Phase phase;
    private float timer;
    private Vector3 velocity;
    private bool hasLanded;

    private float currentFlightTime;
    private float estimatedFlightDuration;
    private bool hasReachedPeak;

    // Tracks phase transitions 
    private Phase lastPhase;

    public AttackState_Melee(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyMelee = enemy as EnemyMelee;
        knockBack = enemy.GetComponent<AgentKnockBack>();
    }

    /// <summary>
    /// Enter: pause agent, reset flags, begin windup freeze.
    /// </summary>
    public override void Enter()
    {
        enemyMelee.PauseAgent();

        enemyMelee.hitAppliedThisAttack = false;
        enemyMelee.EnableHitBox(false);
        enemyMelee.isInAir = false;

        phase = Phase.Windup;
        lastPhase = phase;
        timer = enemyMelee.windupTime;
        hasLanded = false;
        hasReachedPeak = false;
    }


    public override void Update()
    {
        switch (phase)
        {
            case Phase.Windup:
                HandleWindup();
                break;
            case Phase.Leap:
                HandleLeap();
                break;
        }

        if (lastPhase == Phase.Windup && phase == Phase.Leap)
        {
            enemyMelee.PlayJumpSFX();
            enemyMelee.PlayMeleePSVFX(enemyMelee.jumpPoofVFXPrefab, enemyMelee.jumpVFXAttackPoint);
        }

        lastPhase = phase;
    }

    private void HandleWindup()
    {
        // Wait for any active ground-knockback to finish before leaping
        if (knockBack != null && knockBack.IsKnockbackActive)
        {
            timer = enemyMelee.windupTime;
            return;
        }

        // stick to ground every frame during windup 
        SnapToGround();

        FaceTarget(enemy.turnSpeed * 2f);
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            StartLeap();
        }
    }

    /// <summary>
    /// Raycast from high above straight down to find the physics ground surface
    /// </summary>
    private void SnapToGround()
    {
        Vector3 origin = enemy.transform.position + Vector3.up * 5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10f,
                enemyMelee.groundMask, QueryTriggerInteraction.Ignore))
        {
            float halfHeight = enemy.agent != null ? enemy.agent.height * 0.7f : 0f;
            Vector3 pos = enemy.transform.position;
            pos.y = hit.point.y + (halfHeight + enemyMelee.startHeightAboveGround);
            enemy.transform.position = pos;
        }
    }

    private void StartLeap()
    {
        phase = Phase.Leap;
        enemyMelee.EnableHitBox(true);
        enemyMelee.isInAir = true;

        Vector3 startPos = enemy.transform.position;
        Vector3 targetPos = startPos + enemy.transform.forward * 2f;

        if (enemy.target != null)
        {
            Vector3 rawTargetPos = enemy.target.position;
            Vector3 jumpDir = rawTargetPos - startPos;
            jumpDir.y = 0;

            // Lock the leap distance to where the player is NOW.
            float distToPlayer = jumpDir.magnitude;
            jumpDir = jumpDir.normalized;

            float overshoot = Mathf.Min(enemyMelee.leapOverShootDistance, distToPlayer * 0.5f);
            targetPos = startPos + jumpDir * (distToPlayer + overshoot);
        }

        velocity = enemyMelee.CalculateBallisticVelocity(
            startPos, targetPos, enemyMelee.leapHeight, out estimatedFlightDuration);

        // Face the jump direction
        Vector3 hVel = new Vector3(velocity.x, 0, velocity.z);
        if (hVel.sqrMagnitude > 0.001f)
            enemy.transform.rotation = Quaternion.LookRotation(hVel);

        enemyMelee.inAirVelocity = velocity;
        currentFlightTime = 0f;
        hasReachedPeak = false;
    }

    /// <summary>
    /// Core leap- gravity -> swept-sphere move -> landing detection
    /// Mid air knockback impulses are picked up from inAirVelocity
    /// </summary>
    private void HandleLeap()
    {
        if (hasLanded) return;

        float dt = Time.deltaTime;
        currentFlightTime += dt;

        // Pick up any mid air knockback impulse that was added externally
        velocity = enemyMelee.inAirVelocity;

        // Gravity
        velocity.y += Physics.gravity.y * enemyMelee.gravityScale * dt;

        // track when the slime has gone up and start descending
        if (!hasReachedPeak && velocity.y <= 0f)
            hasReachedPeak = true;

        // only check for landing after the slime has actually risen
        if (hasReachedPeak)
        {
            if (enemyMelee.GroundCheck(out Vector3 gp))
            {
                Land(gp);
                return;
            }
        }

        velocity = enemyMelee.SweepMove(velocity, dt);
        enemyMelee.inAirVelocity = velocity;

        // Keep agent in sync 
        if (enemy.agent != null && enemy.agent.isOnNavMesh)
            enemy.agent.nextPosition = enemy.transform.position;

        //prevent infinite flight
        if (currentFlightTime > estimatedFlightDuration * 2.5f)
        {
            Land(enemy.transform.position);
        }
    }

    /// <summary>
    /// Snap to ground, clear in air state, resume agent, go to recovery
    /// </summary>
    private void Land(Vector3 groundPoint)
    {
        if (hasLanded) return;
        hasLanded = true;

        float halfHeight = enemy.agent != null ? enemy.agent.height * 0.7f : 0f;
        enemy.transform.position = groundPoint + Vector3.up * (halfHeight + enemyMelee.startHeightAboveGround);
        enemyMelee.isInAir = false;
        enemyMelee.inAirVelocity = Vector3.zero;
        enemyMelee.EnableHitBox(false);

        // Always enforce cooldown on landing so the slime
        // can never immediately leap again after missing once
        enemy.nextAttackAllowed = Time.time + enemy.attackCooldown;

        // Snap back onto NavMesh  
        enemyMelee.ResumeAgent();

        stateMachine.ChangeState(enemyMelee.GetRecovery());
    }

    public override void Exit()
    {
        enemyMelee.EnableHitBox(false);
        enemyMelee.isInAir = false;
        enemyMelee.inAirVelocity = Vector3.zero;

        // Safety- resume agent if Land() was never called
        if (enemy.agent != null && !enemy.agent.updatePosition)
        {
            enemyMelee.ResumeAgent();
        }
    }
}
