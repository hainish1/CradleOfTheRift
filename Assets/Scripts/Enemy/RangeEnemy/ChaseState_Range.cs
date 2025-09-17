using UnityEngine;

public class ChaseState_Range : EnemyState
{
    EnemyRange enemyRange;
    public ChaseState_Range(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyRange = enemy as EnemyRange;
    }

    public override void Enter()
    {
        if (enemy?.agent != null)
        {
            enemy.agent.isStopped = false;
            enemy.agent.speed = enemyRange.chaseSpeed; // set navmesh speed
        }
    }


    public override void Update()
    {
        if (enemy.target == null) return;

        float distance = Vector3.Distance(enemy.transform.position, enemy.target.position); // go but keep distance
        if (distance > enemyRange.stopDistance * .9f)
        {
            SetAgentDestination(enemy.target.position);
        }
        else
        {
            if (enemy.agent) enemy.agent.isStopped = true; // too close, stop there
        }

        FaceTarget(enemy.turnSpeed);

        if (distance <= enemyRange.attackRange && Time.time >= enemy.nextAttackAllowed)
        {
            stateMachine.ChangeState(enemyRange.GetAttack());
        }

    }

    public override void Exit()
    {
        if (enemy.agent) enemy.agent.isStopped = false; // free him again
    }
}
