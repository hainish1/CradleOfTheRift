using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Boss leap attack using  physics 
/// </summary>
public class LeapAttackState_Boss : EnemyState
{
    private EnemyBoss_SS boss;

    private enum Phase { Windup, Leap }
    private Phase phase;
    private float timer;
    private Vector3 velocity;
    private bool hasLanded;

    private float currentFlightTime;
    private float estimatedFlightDuration;
    private bool hasReachedPeak;

    public LeapAttackState_Boss(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        boss = enemy as EnemyBoss_SS;
    }

    public override void Enter()
    {
        base.Enter();

        // Flash VFX
        if (boss.firePoint != null)
        {
            Vector3 direction = (boss.target
                ? (boss.target.position + Vector3.up * .5f) - boss.firePoint.position
                : boss.transform.forward).normalized;
            Vector3 spawnPoint = boss.firePoint.position + direction * 0.1f;
            boss.CreateVFX(boss.flashVFX, spawnPoint, 5);
        }

        boss.PauseAgent();

        boss.hitAppliedThisAttack = false;
        boss.EnableHitBox(false);
        boss.isInAir = false;

        phase = Phase.Windup;
        timer = boss.windupTime;
        hasLanded = false;
        hasReachedPeak = false;
    }

    public override void Update()
    {
        base.Update();

        switch (phase)
        {
            case Phase.Windup:
                HandleWindup();
                break;
            case Phase.Leap:
                HandleLeap();
                break;
        }
    }

    private void HandleWindup()
    {
        // Pin to ground during windup
        SnapToGround();

        FaceTarget(enemy.turnSpeed * 2f);
        timer -= Time.deltaTime;
        if (timer <= 0f) StartLeap();
    }

    private void SnapToGround()
    {
        Vector3 origin = enemy.transform.position + Vector3.up * 5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10f,
                boss.groundMask, QueryTriggerInteraction.Ignore))
        {
            float halfHeight = enemy.agent != null ? enemy.agent.height * 0.7f : 0f;
            Vector3 pos = enemy.transform.position;
            pos.y = hit.point.y + halfHeight + boss.startHeightAboveGround;
            enemy.transform.position = pos;
        }
    }

    private void StartLeap()
    {
        phase = Phase.Leap;
        boss.EnableHitBox(true);
        boss.isInAir = true;

        Vector3 startPos = enemy.transform.position;
        Vector3 targetPos = startPos + enemy.transform.forward * 2f;

        if (enemy.target != null)
        {
            Vector3 rawTargetPos = enemy.target.position;
            Vector3 jumpDir = rawTargetPos - startPos;
            jumpDir.y = 0;

            float distToPlayer = jumpDir.magnitude;
            jumpDir = jumpDir.normalized;

            float overshoot = Mathf.Min(boss.leapOverShootDistance, distToPlayer * 0.5f);
            targetPos = startPos + jumpDir * (distToPlayer + overshoot);
        }

        velocity = boss.CalculateBallisticVelocity(
            startPos, targetPos, boss.leapHeight, out estimatedFlightDuration);

        // Face the jump direction
        Vector3 hVel = new Vector3(velocity.x, 0, velocity.z);
        if (hVel.sqrMagnitude > 0.001f)
            enemy.transform.rotation = Quaternion.LookRotation(hVel);

        boss.inAirVelocity = velocity;
        currentFlightTime = 0f;
        hasReachedPeak = false;
    }

    private void HandleLeap()
    {
        if (hasLanded) return;

        float dt = Time.deltaTime;
        currentFlightTime += dt;

        velocity = boss.inAirVelocity;

        // Gravity
        velocity.y += Physics.gravity.y * boss.gravityScale * dt;

        // check when the boss has gone up and started descending
        if (!hasReachedPeak && velocity.y <= 0f)
            hasReachedPeak = true;

        // Only check for landing after the boss has actually risen and started falling
        if (hasReachedPeak)
        {
            if (boss.GroundCheck(out Vector3 gp))
            {
                Land(gp);
                return;
            }
        }

        // Swept-sphere collision-safe movement
        velocity = boss.SweepMove(velocity, dt);
        boss.inAirVelocity = velocity;

        // Keep agent in sync
        if (enemy.agent != null && enemy.agent.isOnNavMesh)
            enemy.agent.nextPosition = enemy.transform.position;

        // Safety timeout
        if (currentFlightTime > estimatedFlightDuration * 2.5f)
        {
            Land(enemy.transform.position);
        }
    }

    private void Land(Vector3 groundPoint)
    {
        if (hasLanded) return;
        hasLanded = true;

        float halfHeight = enemy.agent != null ? enemy.agent.height * 0.7f : 0f;
        enemy.transform.position = groundPoint + Vector3.up * (halfHeight + boss.startHeightAboveGround);
        boss.isInAir = false;
        boss.inAirVelocity = Vector3.zero;
        boss.EnableHitBox(false);

        boss.nextAttackAllowed = Time.time + boss.attackCooldown;

        boss.ResumeAgent();
        stateMachine.ChangeState(boss.GetRecoveryState());
    }

    public override void Exit()
    {
        base.Exit();
        boss.EnableHitBox(false);
        boss.isInAir = false;
        boss.inAirVelocity = Vector3.zero;

        if (enemy.agent != null && !enemy.agent.updatePosition)
        {
            boss.ResumeAgent();
        }
    }
}