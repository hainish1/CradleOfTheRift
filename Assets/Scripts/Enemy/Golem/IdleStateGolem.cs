using UnityEngine;

/// <summary>
/// Class - Represents the Idle State for Melee Enemy
/// </summary>
public class IdleStateGolem : EnemyState
{
    private EnemyGolem enemyGolem;


    public IdleStateGolem(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyGolem = enemy as EnemyGolem;
    }

    /// <summary>
    /// Check if player is in aggro range, if yes, switch to Chase state
    /// </summary>
    public override void Update()
    {
        if (PlayerInAggressionRange())
        {
            stateMachine.ChangeState(enemyGolem.GetChase());
        }
    }
}
