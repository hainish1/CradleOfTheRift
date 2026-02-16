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
        enemyRange.SafeResumeAgent();
        enemyRange.SetHorizontalPosition(false);
        if(enemy.agent != null) enemy.agent.speed = enemyRange.chaseSpeed;
    }

    /// <summary>
    /// While inside chase state, face the player and chase towards it. If player is in attack range, then switch to Attack state
    /// </summary>
    public override void Update()
    {
        if (PauseManager.GameIsPaused) return;
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

    }


    void ManageMovement()
    {
        repathTimer -= Time.deltaTime;
        if(repathTimer > 0) return;

        repathTimer = Mathf.Max(0.05f, enemyRange.spreadInterval); // default 5 times/sec

        Vector3 desired = enemyRange.GetSpreadoutChasePoint();

        // Keep the movement on the NavMesh 
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
