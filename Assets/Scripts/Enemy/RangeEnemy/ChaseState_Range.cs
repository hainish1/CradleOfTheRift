using UnityEngine;
using UnityEngine.AI;


/// <summary>
/// Class - Represents the Chase State for Range Enemy
/// </summary>
public class ChaseState_Range : EnemyState
{
    EnemyRange enemyRange;
    float repathTimer; // limit how many times we recalculate path - performance purpose
    public ChaseState_Range(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyRange = enemy as EnemyRange;
    }

    /// <summary>
    /// When entering the chase state, give control to navmeshagent
    /// </summary>
    public override void Enter()
    {
        // if (enemy != null)
        // {
        //     if (enemy.agent != null && enemy.agent.isOnNavMesh)
        //     {
        //         enemy.agent.isStopped = false;
        //         enemy.agent.speed = enemyRange.chaseSpeed; // set navmesh speed
        //     }

        // }
        enemyRange.SafeResumeAgent();
        enemyRange.SetHorizontalPosition(false);
        if(enemy.agent != null) enemy.agent.speed = enemyRange.chaseSpeed;
    }

    /// <summary>
    /// While inside chase state, face the player and chase towards it. If player is in attack range, then switch to Attack state
    /// </summary>
    public override void Update()
    {
        if (enemy.target == null)
        {
            stateMachine.ChangeState(enemyRange.GetIdle());
            return;
        }

        //Handle Movement
        ManageMovement();

        // Handle Rotation
        enemyRange.FaceTargetSmooth(enemyRange.turnSpeed);

        float distSqr = (enemy.target.position - enemy.transform.position).sqrMagnitude;
        float attackRangeSqr = enemyRange.attackRange * enemyRange.attackRange;

        // if in range and cooldown ready -> ATTACK
        if(distSqr <= attackRangeSqr && Time.time >= enemyRange.nextShootTime)
        {
            stateMachine.ChangeState(enemyRange.GetAttack());
        }

        // float distance = Vector3.Distance(enemy.transform.position, enemy.target.position); // go but keep distance
        // if (distance > enemyRange.stopDistance * .8f)
        // {
        //     if (enemy != null) SetAgentDestination(enemy.target.position);
        // }
        // else
        // {
        //     if (enemy != null)
        //     {
        //         if (enemy.agent) enemy.agent.isStopped = true; // too close, stop there
        //     }
        // }

        // FaceTarget(enemy.turnSpeed);

        // if (distance <= enemyRange.attackRange && Time.time >= enemy.nextAttackAllowed)
        // {
        //     stateMachine.ChangeState(enemyRange.GetAttack());
        // }

    }


    void ManageMovement()
    {
        repathTimer -= Time.deltaTime;
        if(repathTimer > 0) return;

        repathTimer = Mathf.Max(0.05f, enemyRange.spreadInterval); // default ~5x/sec

        Vector3 desired = enemyRange.GetSpreadoutChasePoint();

        // Keep the movement on the NavMesh even though visuals may "fly".
        if (enemy.agent != null && enemy.agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(desired, out hit, enemyRange.navSampleDistance, NavMesh.AllAreas))
            {
                enemy.agent.SetDestination(hit.position);
            }
            else
            {
                enemy.agent.SetDestination(desired);
            }
        }
    }

    /// <summary>
    /// What to do when exiting the ChaseState
    /// </summary>
    public override void Exit()
    {
        if (enemy.agent != null && enemy.agent.isActiveAndEnabled) enemy.agent.isStopped = false; // free him again
    }
}
