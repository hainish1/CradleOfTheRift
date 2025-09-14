using UnityEngine;

public class IdleState_Range : EnemyState
{
    EnemyRange enemyRange;
    public IdleState_Range(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyRange = enemy as EnemyRange;
    }

    public override void Update()
    {
        if (PlayerInAggressionRange())
        {
            stateMachine.ChangeState(enemyRange.GetChase());
        }
    }
}
