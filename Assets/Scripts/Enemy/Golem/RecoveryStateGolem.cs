using UnityEngine;

/// <summary>
/// Class - Represents the Recovery State for Melee Enemy
/// </summary>
public class RecoveryStateGolem : EnemyState
{
    // give up trying to wander after this many consecutive failed picks and just chase
    private const int MaxConsecutiveWanderFailures = 4;

    private EnemyGolem enemyGolem;
    private float endTime;
    private float startWanderTime;
    private float wanderTimer;
    private bool isWandering;
    private int wanderFailures;

    public RecoveryStateGolem(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyGolem = enemy as EnemyGolem;
    }


    /// <summary>
    /// What to do when enemy enters the Recovery state
    /// </summary>
    public override void Enter()
    {
        // ensure recovery lasts as long as post attack pause
        float wanderDuration = Random.Range(enemyGolem.minWanderTime, enemyGolem.maxWanderTime);
        startWanderTime = Time.time + enemyGolem.postAttackCooldown;
        endTime = startWanderTime + wanderDuration;

        wanderTimer = 0f;
        isWandering = false;
        wanderFailures = 0;
        if (enemy.agent != null && enemy.agent.isActiveAndEnabled && enemy.agent.isOnNavMesh) // also check navmesh
        {
            enemy.agent.isStopped = true;
            enemy.agent.velocity = Vector3.zero;
            enemy.agent.ResetPath();
        }
    }

    /// <summary>
    /// Wait for recovery time to finish, then if player is in aggro range, chase it, else switch to idle
    /// </summary>
    public override void Update()
    {
        if (enemy.target == null)
        {
            stateMachine.ChangeState(enemyGolem.GetIdle());
            return;
        }

        float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.target.position);
        // Interrupt wandering if player gets too close -> trigger melee attack immediately
        if (distanceToPlayer <= enemyGolem.minAttackDistance && enemyGolem.CanStartAttack() && Time.time >= enemy.nextAttackAllowed)
        {
            stateMachine.ChangeState(enemyGolem.GetMeleeAttack());
            return;
        }

        // Interrupt wandering if player ran out of range -> chase them until in range
        if (distanceToPlayer > enemyGolem.shootingRange)
        {
            stateMachine.ChangeState(enemyGolem.GetChase());
            return;
        }

        // recovery is over go back to chase
        if (Time.time >= endTime)
        {
            stateMachine.ChangeState(enemyGolem.GetChase());
            return;
        }

        // stand
        if (Time.time < startWanderTime) return;

        bool needsNewPoint = !isWandering || wanderTimer <= 0f || HasArrivedAtDestination();
        if (needsNewPoint)
        {
            PickWanderPoint();
        }
        else
        {
            wanderTimer -= Time.deltaTime;
        }

        enemyGolem.FaceMovementDirectionSmooth(enemyGolem.turnSpeedWhileAiming);
    }

    private bool HasArrivedAtDestination()
    {
        var agent = enemy.agent;
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return false;
        if (agent.pathPending) return false;
        if (agent.remainingDistance > agent.stoppingDistance + 0.25f) return false;
        return !agent.hasPath || agent.velocity.sqrMagnitude < 0.05f;
    }

    private void PickWanderPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * enemyGolem.wanderRadius;
        Vector3 randomPos = enemyGolem.transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        float sampleRadius = Mathf.Max(2f, enemyGolem.wanderRadius * 0.25f);
        if (UnityEngine.AI.NavMesh.SamplePosition(randomPos, out UnityEngine.AI.NavMeshHit hit, sampleRadius, UnityEngine.AI.NavMesh.AllAreas))
        {
            if (enemy.agent != null && enemyGolem.TryResumePathing() && enemy.agent.SetDestination(hit.position))
            {
                isWandering = true;
                wanderTimer = enemyGolem.wanderInterval;
                wanderFailures = 0;
                return;
            }
        }

        isWandering = false;
        // If invalid spot or pathing isn't ready, wait half a second before checking again.
        wanderTimer = 0.5f;
        wanderFailures++;
        // If we cannot find anywhere valid to wander, go back to chase rather
        // than bein here for the whole recovery duration
        if (wanderFailures >= MaxConsecutiveWanderFailures)
        {
            stateMachine.ChangeState(enemyGolem.GetChase());
        }
    }

}
