using UnityEngine;

/// <summary>
/// Class - Represents the Chase State for Melee Enemy
/// </summary>
public class ChaseState_Melee : EnemyState
{
    private EnemyMelee enemyMelee;

    private bool dragging;
    private float phaseTimer;

    public ChaseState_Melee(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyMelee = enemy as EnemyMelee;

    }

    /// <summary>
    /// What to do when Enemy enters the Chase State
    /// </summary>
    public override void Enter()
    {
        if (enemy != null)
        {
            if (enemy.agent != null)
            {
                enemy.agent.isStopped = false;
                enemy.agent.speed = enemyMelee.dragSpeed;
                dragging = true;
                phaseTimer = enemyMelee.dragDuration;
            }
        }
    }

    /// <summary>
    /// Chase towards the player using navmeshagent
    /// stop at leaping range, then switch to Attack State
    /// </summary>
    public override void Update()
    {
        if (enemy.target == null) return; // if there is not target then nothing to chase
        float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.target.position);

        if (distanceToPlayer > enemyMelee.leapAttackRange)
        {
            phaseTimer -= Time.deltaTime; // continue chasing with drag n rest phase

            if (dragging)
            {
                enemy.agent.isStopped = false;
                enemy.agent.speed = enemyMelee.dragSpeed;
                SetAgentDestination(enemy.target.position); // give the AI a position to chase  
            }
            else
            {
                // rest phase
                enemy.agent.isStopped = true;
                enemy.agent.velocity = Vector3.zero;
                FaceTarget(enemy.turnSpeed);

            }

            if (phaseTimer <= 0f)
            {
                dragging = !dragging;
                phaseTimer = dragging ? enemyMelee.dragDuration : enemyMelee.restDuration;
            }
        }
        else if(distanceToPlayer <= enemyMelee.leapAttackRange && distanceToPlayer >= enemyMelee.minAttackDistance)
        {
            // within leap attack range but not too close, stop and face player
            enemy.agent.isStopped = true;
            enemy.agent.velocity = Vector3.zero;
            FaceTarget(enemy.turnSpeed);
        }
        else if(distanceToPlayer < enemyMelee.minAttackDistance)
        {
            // too close, back slightly
            Vector3 awayFromPlayer = enemy.transform.position - enemy.target.position;
            awayFromPlayer.y = 0f;
            awayFromPlayer.Normalize();

            Vector3 retreatPosition = enemy.target.position + awayFromPlayer * enemyMelee.leapAttackRange;
            enemy.agent.isStopped = false;
            SetAgentDestination(retreatPosition);
        }

        if(distanceToPlayer <= enemyMelee.leapAttackRange &&
            distanceToPlayer >= enemyMelee.minAttackDistance &&
            Time.time >= enemy.nextAttackAllowed)
        {
            stateMachine.ChangeState(enemyMelee.GetAttack());
        }

        

        // // trying to enter attack state
        // if (PlayerInAttackRange(enemyMelee.attackRange) && Time.time >= enemy.nextAttackAllowed)
        // {
        //     stateMachine.ChangeState(enemyMelee.GetAttack());
        // }

    }
}
