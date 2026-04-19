using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Class - Represents the Chase/Positioning State for the Titan.
/// Routes to Melee, Ranged, or chases the player based on distance.
/// </summary>
public class ChaseStateTitan : EnemyState
{
    // if pathing fails for this many seconds straight, force a navmesh warp
    private const float _stuckRecoverySeconds = 2f;
    private const float _stuckWarpRadius = 30f;

    private EnemyTitan enemyTitan;
    private AgentKnockBack knockBack;
    private float pathFailureTimer;

    public ChaseStateTitan(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyTitan = enemy as EnemyTitan;
        knockBack = enemy.GetComponent<AgentKnockBack>();
    }

    public override void Enter()
    {
        enemyTitan.TryResumePathing();
        pathFailureTimer = 0f;
    }

    public override void Update()
    {
        if (enemy.target == null) return;
        if (knockBack != null && knockBack.IsKnockbackActive) return;

        float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.target.position);
        bool cooldownReady = Time.time >= enemy.nextAttackAllowed;

        // Player is in melee range -> Go directly to melee attack
        if (distanceToPlayer <= enemyTitan.minAttackDistance)
        {
            if (!enemyTitan.CanStartAttack() || !cooldownReady)
            {
                StopAgentInPlace();
                FaceTarget(enemy.turnSpeed);
                return;
            }

            StopAgentInPlace();
            stateMachine.ChangeState(enemyTitan.GetMeleeAttack());
            return;
        }

        // Player is out of range -> Chase them until in range
        if (distanceToPlayer > enemyTitan.shootingRange)
        {
            if (!enemyTitan.TrySetChaseDestination())
            {
                StopAgentInPlace();
                pathFailureTimer += Time.deltaTime;
                if (pathFailureTimer >= _stuckRecoverySeconds)
                {
                    TryUnstickToNavMesh();
                    pathFailureTimer = 0f;
                }
            }
            else
            {
                pathFailureTimer = 0f;
            }

            return;
        }

        // Player is in range -> throw rock at player forehead
        else
        {
            pathFailureTimer = 0f;

            if (!enemyTitan.CanStartAttack() || !cooldownReady)
            {
                StopAgentInPlace();
                FaceTarget(enemy.turnSpeed);
                return;
            }

            StopAgentInPlace();
            FaceTarget(enemy.turnSpeed);

            stateMachine.ChangeState(enemyTitan.GetAttack());
            return;
        }
    }

    private void StopAgentInPlace()
    {
        if (enemy.agent == null || !enemy.agent.isActiveAndEnabled || !enemy.agent.isOnNavMesh) return;
        enemy.agent.isStopped = true;
        enemy.agent.velocity = Vector3.zero;
    }

    private void TryUnstickToNavMesh()
    {
        if (enemy.agent == null || !enemy.agent.isActiveAndEnabled) return;

        Vector3 origin = enemy.target != null ? enemy.target.position : enemy.transform.position;
        if (NavMesh.SamplePosition(origin, out NavMeshHit hit, _stuckWarpRadius, NavMesh.AllAreas))
        {
            enemy.agent.Warp(hit.position);
            enemy.transform.position = hit.position;
            enemy.agent.nextPosition = hit.position;
            enemy.agent.isStopped = false;
            enemy.agent.ResetPath();
        }
    }
}
