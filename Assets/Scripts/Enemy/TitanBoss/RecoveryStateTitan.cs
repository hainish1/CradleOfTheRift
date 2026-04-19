using UnityEngine;

/// <summary>
/// Class - Represents the Recovery State for Melee Enemy
/// </summary>
public class RecoveryStateTitan : EnemyState
{
    // give up wandering after this many failed picks and just chase tbh
    private const int _maxConsecutiveWanderFailures = 4;

    private EnemyTitan enemyTitan;
    private float endTime;
    private float startWanderTime;
    private float wanderTimer;
    private bool isWandering;
    private int wanderFailures;

    public RecoveryStateTitan(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyTitan = enemy as EnemyTitan;
    }


    /// <summary>
    /// What to do when enemy enters the Recovery state
    /// </summary>
    public override void Enter()
    {
        float wanderDuration = Random.Range(enemyTitan.minWanderTime, enemyTitan.maxWanderTime);
        startWanderTime = Time.time + enemyTitan.postAttackCooldown;
        endTime = startWanderTime + wanderDuration;

        wanderTimer = 0f;
        isWandering = false;
        wanderFailures = 0;
        if (enemy.agent != null && enemy.agent.isActiveAndEnabled && enemy.agent.isOnNavMesh)
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
            stateMachine.ChangeState(enemyTitan.GetIdle());
            return;
        }

        float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.target.position);
        // Interrupt wandering if player gets too close -> trigger melee attack immediately
        if (distanceToPlayer <= enemyTitan.minAttackDistance && enemyTitan.CanStartAttack() && Time.time >= enemy.nextAttackAllowed)
        {
            stateMachine.ChangeState(enemyTitan.GetMeleeAttack());
            return;
        }

        // Interrupt wandering if player ran out of range -> chase them until in range
        if (distanceToPlayer > enemyTitan.shootingRange)
        {
            stateMachine.ChangeState(enemyTitan.GetChase());
            return;
        }

        if (Time.time >= endTime)
        {
            stateMachine.ChangeState(enemyTitan.GetChase());
            return;
        }

        // stand still
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

        enemyTitan.FaceMovementDirectionSmooth(enemyTitan.turnSpeedWhileAiming);
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
        Vector2 randomCircle = Random.insideUnitCircle * enemyTitan.wanderRadius;
        Vector3 randomPos = enemyTitan.transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        float sampleRadius = Mathf.Max(2f, enemyTitan.wanderRadius * 0.25f);
        if (UnityEngine.AI.NavMesh.SamplePosition(randomPos, out UnityEngine.AI.NavMeshHit hit, sampleRadius, UnityEngine.AI.NavMesh.AllAreas))
        {
            if (enemy.agent != null && enemyTitan.TryResumePathing() && enemy.agent.SetDestination(hit.position))
            {
                isWandering = true;
                wanderTimer = enemyTitan.wanderInterval;
                wanderFailures = 0;
                return;
            }
        }

        isWandering = false;
        wanderTimer = 0.5f;
        wanderFailures++;
        // same stuff
        if (wanderFailures >= _maxConsecutiveWanderFailures)
        {
            stateMachine.ChangeState(enemyTitan.GetChase());
        }
    }

}
