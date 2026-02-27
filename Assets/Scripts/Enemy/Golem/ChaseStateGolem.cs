using UnityEngine;

/// <summary>
/// Class - Represents the Chase State for Melee Enemy
/// </summary>
public class ChaseStateGolem : EnemyState
{
    private EnemyGolem enemyGolem;
    private AgentKnockBack knockBack;

    private bool dragging;
    private float phaseTimer;

    public ChaseStateGolem(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyGolem = enemy as enemyGolem;
        knockBack = enemy.GetComponent<AgentKnockBack>();
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
                enemy.agent.speed = enemyGolem.dragSpeed;
                dragging = true;
                phaseTimer = enemyGolem.dragDuration;
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

        if (distanceToPlayer > enemyGolem.leapAttackRange)
        {
            phaseTimer -= Time.deltaTime; // continue chasing with drag n rest phase

            if (dragging)
            {
                if(enemy.agent != null)
                {
                    enemy.agent.isStopped = false;
                    enemy.agent.speed = enemyGolem.dragSpeed;
                    SetAgentDestination(enemy.target.position); // give the AI a position to chase
                }  
            }
            else
            {
                // rest phase
                if(enemy.agent != null)
                {
                    enemy.agent.isStopped = true;
                    enemy.agent.velocity = Vector3.zero;
                    FaceTarget(enemy.turnSpeed);
                }

            }

            if (phaseTimer <= 0f)
            {
                dragging = !dragging;
                phaseTimer = dragging ? enemyGolem.dragDuration : enemyGolem.restDuration;
            }
        }
        else if(distanceToPlayer <= enemyGolem.leapAttackRange && distanceToPlayer >= enemyGolem.minAttackDistance)
        {
            // within leap attack range but not too close, stop and face player
            enemy.agent.isStopped = true;
            enemy.agent.velocity = Vector3.zero;
            FaceTarget(enemy.turnSpeed);
        }
        else if(distanceToPlayer < enemyGolem.minAttackDistance)
        {
            // too close, back slightly
            Vector3 awayFromPlayer = enemy.transform.position - enemy.target.position;
            awayFromPlayer.y = 0f;
            awayFromPlayer.Normalize();

            Vector3 retreatPosition = enemy.target.position + awayFromPlayer * enemyGolem.leapAttackRange;
            enemy.agent.isStopped = false;
            SetAgentDestination(retreatPosition);
        }

        // Don't initiate attack during active knockback (PLS)
        if (knockBack != null && knockBack.IsKnockbackActive) return;

        // Don't leap at a player who is too high above or below - looks unnatural
        float yDiff = Mathf.Abs(enemy.target.position.y - enemy.transform.position.y);
        if (yDiff > enemyGolem.maxAttackHeightDiff) return;

        if(distanceToPlayer <= enemyGolem.leapAttackRange &&
            distanceToPlayer >= enemyGolem.minAttackDistance &&
            Time.time >= enemy.nextAttackAllowed)
        {
            stateMachine.ChangeState(enemyGolem.GetAttack());
        }

        

        // // trying to enter attack state
        // if (PlayerInAttackRange(enemyGolem.attackRange) && Time.time >= enemy.nextAttackAllowed)
        // {
        //     stateMachine.ChangeState(enemyGolem.GetAttack());
        // }

    }
}
