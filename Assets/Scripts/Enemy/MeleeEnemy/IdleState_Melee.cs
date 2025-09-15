using UnityEngine;

public class IdleState_Melee : EnemyState
{
    private EnemyMelee enemyMelee;

    public IdleState_Melee(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyMelee = enemy as EnemyMelee;
    }

    public override void Update()
    {
        if (PlayerInAggressionRange())
        {
            stateMachine.ChangeState(enemyMelee.GetChase());
        }
    }
}
