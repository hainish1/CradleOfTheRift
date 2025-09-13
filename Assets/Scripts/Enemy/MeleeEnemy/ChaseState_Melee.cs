using UnityEngine;

public class ChaseState_Melee : EnemyState
{
    private EnemyMelee enemyMelee;
    public ChaseState_Melee(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyMelee = enemy as EnemyMelee;

    }

    public override void Enter()
    {
        if (enemy.agent != null)
        {
            enemy.agent.isStopped = false;
        }
    }

    public override void Update()
    {
        if (enemy.target == null) return; // if there is not target then nothing to chase

        SetAgentDestination(enemy.target.position); // give the AI a position to chase
        FaceTarget(enemy.turnSpeed);

        if(PlayerInAttackRange(enemyMelee.attackRange) && Time.time >= enemy.nextAttackAllowed)
        {
            stateMachine.ChangeState(enemyMelee.GetAttack());
        }

    }
}
