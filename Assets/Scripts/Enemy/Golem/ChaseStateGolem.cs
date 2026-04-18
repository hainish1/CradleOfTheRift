using UnityEngine;

/// <summary>
/// Class - Represents the Chase/Positioning State for the Golem.
/// Routes to Melee, Ranged, or chases the player based on distance.
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

    public override void Enter()
    {
        enemyGolem.TryResumePathing();
    }

    public override void Update()
    {
        if (enemy.target == null) return; 
        if (knockBack != null && knockBack.IsKnockbackActive) return;

        float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.target.position);

        // Player is in melee range -> Go directly to melee attack
        if (distanceToPlayer <= enemyGolem.minAttackDistance)
        {
            if (enemy.agent != null)
            {
                enemy.agent.isStopped = true;
                enemy.agent.velocity = Vector3.zero;
            }
            
            stateMachine.ChangeState(enemyGolem.GetMeleeAttack());
            return;
        }
        
        // Player is out of range -> Chase them until in range
        if (distanceToPlayer > enemyGolem.shootingRange)
        {
            if (!enemyGolem.TrySetChaseDestination() && enemy.agent != null)
            {
                enemy.agent.isStopped = true;
                enemy.agent.velocity = Vector3.zero;
            }

            return;
        }
        
        // Player is in range -> throw rock at player forehead
        else
        {
            if (enemy.agent != null)
            {
                enemy.agent.isStopped = true;
                enemy.agent.velocity = Vector3.zero;
            }
            
            FaceTarget(enemy.turnSpeed);

            stateMachine.ChangeState(enemyGolem.GetAttack());
            return;
        }
    }
}
