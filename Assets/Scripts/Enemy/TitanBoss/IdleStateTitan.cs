using UnityEngine;

/// <summary>
/// Class - Represents the Idle State for Melee Enemy
/// </summary>
public class IdleStateTitan : EnemyState
{
    private EnemyTitan enemyTitan;


    public IdleStateTitan(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyTitan = enemy as EnemyTitan;
    }

    /// <summary>
    /// Check if player is in aggro range, if yes, switch to Chase state
    /// </summary>
    public override void Update()
    {
        if (PlayerInAggressionRange())
        {
            stateMachine.ChangeState(enemyTitan.GetChase());
        }
    }
}
