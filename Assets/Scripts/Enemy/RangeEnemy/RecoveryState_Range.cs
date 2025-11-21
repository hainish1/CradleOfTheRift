using UnityEngine;


/// <summary>
/// Class - Represents the Recovery State for Range Enemy
/// </summary>
public class RecoveryState_Range : EnemyState
{
    EnemyRange enemyRange;
    private float endTime;

    public RecoveryState_Range(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyRange = enemy as EnemyRange;
    }

    /// <summary>
    /// When entering recovery state, set the recovery time 
    /// </summary>
    public override void Enter()
    {
        endTime = Time.time + enemyRange.reloadTime; // set
        if (enemy.agent != null) enemy.agent.isStopped = false;
    }

    /// <summary>
    /// Check if recovery time is finished, then if player is in aggro range, switch to chase state, else switch to idle state
    /// </summary>
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
