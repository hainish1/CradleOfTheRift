using UnityEngine;

/// <summary>
/// Class - Represents the Idle State for Melee Enemy
/// </summary>
public class IdleState_Melee : EnemyState
{
    private EnemyMelee enemyMelee;


    public IdleState_Melee(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyMelee = enemy as EnemyMelee;
    }

    /// <summary>
    /// Check if player is in aggro range, if yes, switch to Chase state
    /// </summary>
    public override void Update()
    {
        if (PlayerInAggressionRange())
        {
            stateMachine.ChangeState(enemyMelee.GetChase());
        }
    }
}
