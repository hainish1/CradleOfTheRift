using UnityEngine;

/// <summary>
/// Class - Represents the Idle State for Range Enemy, enemy will enter this state when initialized
/// </summary>
public class IdleState_Range : EnemyState
{
    EnemyRange enemyRange;
    public IdleState_Range(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyRange = enemy as EnemyRange;
    }

    /// <summary>
    /// If player is in aggro range, switch to ChaseState and start chasing it
    /// </summary>
    public override void Update()
    {
        if (PlayerInAggressionRange())
        {
            stateMachine.ChangeState(enemyRange.GetChase());
        }
    }
}
