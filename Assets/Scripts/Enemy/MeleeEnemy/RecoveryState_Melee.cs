using UnityEngine;

public class RecoveryState_Melee : EnemyState
{
    private EnemyMelee enemyMelee;

    float endTime;

    public RecoveryState_Melee(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyMelee = enemy as EnemyMelee;
    }

    public override void Enter()
    {
        endTime = Time.time + enemyMelee.recoveryTime; // post attack pause
        if (enemy.agent != null) enemy.agent.isStopped = false;
    }

    public override void Update()
    {
        if (Time.time >= endTime)
        {
            if (PlayerInAggressionRange())
            {
                stateMachine.ChangeState(enemyMelee.GetChase());
            }
            else
            {
                stateMachine.ChangeState(enemyMelee.GetIdle()); // I prolly Dont even need this since its risk of rain 2 style and its always after us
            }
        }
    }


}
