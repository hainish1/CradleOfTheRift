using UnityEngine;


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
        if (enemy != null)
        {
            if (enemy.agent != null && enemy.agent.isOnNavMesh)
            {
                enemy.agent.isStopped = false;
                enemy.agent.speed = enemyRange.chaseSpeed; // set navmesh speed
            }

        }
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

        // handle combat stuff
        ManageCombat();

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

        repathTimer = 0.2f; // do about 5 times a second

        float distToTarget = Vector3.Distance(enemy.transform.position, enemy.target.position);

        Vector3 dest = enemy.target.position;

        // if too close, stop moving closer and move a bit back
        if(distToTarget < enemyRange.desiredDistance)
        {
            // back away a bit
            Vector3 dirFromTarget = (enemy.transform.position - enemy.target.position).normalized;
            dest = enemy.target.position + dirFromTarget * enemyRange.desiredDistance; 
        }

        if(enemy.agent != null && enemy.agent.isOnNavMesh)
        {
            enemy.agent.SetDestination(dest);
        }
    }

    void ManageCombat()
    {
        // Check Distance
        float distSqr = (enemy.target.position - enemy.transform.position).sqrMagnitude;
        float rangeSqr = enemyRange.attackRange * enemyRange.attackRange;

        if (distSqr <= rangeSqr)
        {
            // we are in range, check cooldown
            if (Time.time >= enemyRange.nextShootTime)
            {
                enemyRange.FireAtTarget();
                enemyRange.nextShootTime = Time.time + enemyRange.fireCooldown;
                
            }
        }
    }

    /// <summary>
    /// What to do when exiting the ChaseState
    /// </summary>
    public override void Exit()
    {
        if (enemy.agent) enemy.agent.isStopped = false; // free him again
    }
}
