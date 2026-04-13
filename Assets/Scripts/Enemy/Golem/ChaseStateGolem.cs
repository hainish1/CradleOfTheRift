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

            stateMachine.ChangeState(enemyGolem.GetAttack());
        }

        // Blend golem animation between idle and moving.
        Vector3 worldVelocity = enemy.agent.velocity;
        Vector3 localVelocity = enemy.transform.InverseTransformDirection(worldVelocity);
        Vector3 localVelocityUnitVector = localVelocity.normalized;
        float moveBlendX = localVelocityUnitVector.x * (localVelocity.magnitude / enemyGolem.chaseSpeed);
        float moveBlendY = localVelocityUnitVector.z * (localVelocity.magnitude / enemyGolem.chaseSpeed);
        enemyGolem.golemAnim.SetFloat("MoveVector_X", moveBlendX, dampTime: 0.03f, Time.deltaTime);
        enemyGolem.golemAnim.SetFloat("MoveVector_Y", moveBlendY, dampTime: 0.03f, Time.deltaTime);
    }
}