using UnityEngine;

/// <summary>
/// Class - Represents the Chase State for Melee Enemy
/// </summary>
public class ChaseStateGolem : EnemyState
{
    private EnemyGolem enemyGolem;
    private AgentKnockBack knockBack;
    public ChaseStateGolem(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyGolem = enemy as EnemyGolem;
        knockBack = enemy.GetComponent<AgentKnockBack>();
    }

    /// <summary>
    /// What to do when Enemy enters the Chase State
    /// </summary>
    public override void Enter()
    {
        if (enemy?.agent != null)
        {
            enemy.agent.isStopped = false;
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

        if (distanceToPlayer > enemyGolem.shootingRange)  // Chase the player if out of range
        {
            if(enemy.agent != null)
            {
                enemy.agent.isStopped = false;
                SetAgentDestination(enemy.target.position); // give the AI a position to chase
            }
        }  
        else if (distanceToPlayer < enemyGolem.minAttackDistance)   // If the player is too close, retreat for now. (This behavior may change to be removed or replaced with a melee attack)
        {
            Vector3 awayFromPlayer = enemy.transform.position - enemy.target.position;
            awayFromPlayer.y = 0f;
            awayFromPlayer.Normalize();

            Vector3 retreatPosition = enemy.target.position + awayFromPlayer * enemyGolem.shootingRange;
            
            if(enemy.agent != null)
            {
                enemy.agent.isStopped = false;
                SetAgentDestination(retreatPosition);   // Replace this with a melee attack state later
            }

        }
        else
        {
            if (enemy.agent != null)
            {
                enemy.agent.isStopped = true;
                enemy.agent.velocity = Vector3.zero;
            }

            FaceTarget(enemy.turnSpeed);

            // Don't initiate attack during active knockback (PLS)
            if (knockBack != null && knockBack.IsKnockbackActive) return;

            if(Time.time >= enemy.nextAttackAllowed)
            {
                stateMachine.ChangeState(enemyGolem.GetAttack());
            }
        }
    }
}
