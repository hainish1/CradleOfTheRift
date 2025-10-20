using UnityEngine;

/// <summary>
/// Class - Represents the Recovery State for Melee Enemy
/// </summary>
public class RecoveryState_Melee : EnemyState
{
    private EnemyMelee enemyMelee;

    float endTime;

    public RecoveryState_Melee(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyMelee = enemy as EnemyMelee;
    }


    /// <summary>
    /// What to do when enemy enters the Recovery state
    /// </summary>
    public override void Enter()
    {
        endTime = Time.time + enemyMelee.recoveryTime; // post attack pause
        if (enemy.agent != null) enemy.agent.isStopped = false;
    }

    /// <summary>
    /// Wait for recovery time to finish, then if player is in aggro range, chase it, else switch to idle
    /// </summary>
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
