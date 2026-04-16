using UnityEngine;

/// <summary>
/// Class - Represents the Chase/Positioning State for the Golem.
/// Routes to Melee, Ranged, or chases the player based on distance.
/// </summary>
public class ChaseStateTitan : EnemyState
{
    private EnemyTitan enemyTitan;
    private AgentKnockBack knockBack;

    public ChaseStateTitan(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyTitan = enemy as EnemyTitan;
        knockBack = enemy.GetComponent<AgentKnockBack>();
    }

    public override void Enter()
    {
        if (enemy?.agent != null)
        {
            enemy.agent.isStopped = false;
        }
    }

    public override void Update()
    {
        if (enemy.target == null) return; 

        float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.target.position);

        // Player is in melee range -> Go directly to melee attack
        if (distanceToPlayer <= enemyTitan.minAttackDistance)
        {
            if (enemy.agent != null)
            {
                enemy.agent.isStopped = true;
                enemy.agent.velocity = Vector3.zero;
            }
            
            stateMachine.ChangeState(enemyTitan.GetMeleeAttack());
            return;
        }
        
        // Player is out of range -> Chase them until in range
        if (distanceToPlayer > enemyTitan.shootingRange)
        {
            if (enemy.agent != null)
            {
                enemy.agent.isStopped = false;
                SetAgentDestination(enemy.target.position); 
            }
        }
        
        // Player is in range -> throw rock at player forehead
        else
        {
            if (knockBack != null && knockBack.IsKnockbackActive) return;

            if (enemy.agent != null)
            {
                enemy.agent.isStopped = true;
                enemy.agent.velocity = Vector3.zero;
            }
            
            FaceTarget(enemy.turnSpeed);

            stateMachine.ChangeState(enemyTitan.GetAttack());
        }

        // Blend golem animation between idle and moving.
        Vector3 worldVelocity = enemy.agent.velocity;
        Vector3 localVelocity = enemy.transform.InverseTransformDirection(worldVelocity);
        float moveBlend = localVelocity.magnitude / enemyTitan.chaseSpeed;
        enemyTitan.golemAnim.SetFloat("MoveVector", moveBlend, dampTime: 0.03f, Time.deltaTime);
    }
}
