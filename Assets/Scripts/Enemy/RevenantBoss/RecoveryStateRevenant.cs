using UnityEngine;


/// <summary>
/// Class - Represents the Recovery State for Revenant enemy boss
/// </summary>
public class RecoveryStateRevenant : EnemyState
{
    RevenantBossRange bossRange;
    private float endTime;

    public RecoveryStateRevenant(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        bossRange = enemy as RevenantBossRange;
    }

    /// <summary>
    /// When entering recovery state, set the recovery time 
    /// </summary>
    public override void Enter()
    {
        endTime = Time.time + bossRange.recoveryTime; // set
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
                stateMachine.ChangeState(bossRange.GetChase());
            }
            else
            {
                stateMachine.ChangeState(bossRange.GetIdle());
            }
        }
    }



}
