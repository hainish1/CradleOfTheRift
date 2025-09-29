using UnityEngine;

public class RecoveryState_Range : EnemyState
{
    EnemyRange enemyRange;
    private float endTime;

    public RecoveryState_Range(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyRange = enemy as EnemyRange;
    }

    public override void Enter()
    {
        endTime = Time.time + enemyRange.recoveryTime; // set
        if (enemy.agent != null) enemy.agent.isStopped = false;
    }

    public override void Update()
    {
        if (Time.time >= endTime)
        {
            if (PlayerInAggressionRange()) // if the player is still in aggression range
            {
                stateMachine.ChangeState(enemyRange.GetChase());
            }
            else
            { 
                stateMachine.ChangeState(enemyRange.GetIdle());
            }
        }
    }



}
