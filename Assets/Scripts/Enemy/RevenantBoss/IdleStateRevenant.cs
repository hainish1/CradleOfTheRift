using UnityEngine;

/// <summary>
/// Class - Represents the Idle State for Revenant enemy boss, enemy will enter this state when initialized
/// </summary>
public class IdleStateRevenant : EnemyState
{
    RevenantBossRange bossRange;
    public IdleStateRevenant(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        bossRange = enemy as RevenantBossRange;
    }

    /// <summary>
    /// If player is in aggro range, switch to ChaseState and start chasing it
    /// </summary>
    public override void Update()
    {
        if (PlayerInAggressionRange())
        {
            stateMachine.ChangeState(bossRange.GetChase());
        }
    }
}
