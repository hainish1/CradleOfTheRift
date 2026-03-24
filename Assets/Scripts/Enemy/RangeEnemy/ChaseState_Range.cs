using UnityEngine;
using UnityEngine.AI;


/// <summary>
/// Class - Represents the Chase State for Range Enemy
/// </summary>
public class ChaseState_Range : EnemyState
{
    EnemyRange enemyRange;

    public ChaseState_Range(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyRange = enemy as EnemyRange;
    }

    /// <summary>
    /// When entering the chase state, let the flight move freely without navmesh shit
    /// </summary>
    public override void Enter()
    {
        enemyRange.SetHorizontalPosition(false);
        enemyRange.SafeResumeAgent();
    }

    /// <summary>
    /// While inside chase state, face the player and chase towards it. If player is in attack range, then switch to Attack state
    /// </summary>
    public override void Update()
    {
        if (PauseManager.GameIsPaused) return;

        if (enemy.target == null)
        {
            stateMachine.ChangeState(enemyRange.GetIdle());
            return;
        }

        // Handle Rotation
        enemyRange.FaceTargetSmooth(enemyRange.turnSpeed);

        float distSqr = (enemy.target.position - enemy.transform.position).sqrMagnitude;

        // if player far out of aggression range go back to idle
        float outOfRange = enemy.playerOutOfRange > 0f ? enemy.playerOutOfRange : enemy.aggressionRange * 1.5f;
        if (distSqr > outOfRange * outOfRange)
        {
            stateMachine.ChangeState(enemyRange.GetIdle());
            return;
        }

        float attackRangeSqr = enemyRange.attackRange * enemyRange.attackRange;

        // if in range, cooldown expired, AND clear line of sight, only then ATTACK
        if (distSqr <= attackRangeSqr && Time.time >= enemyRange.nextAttackTime && enemyRange.HasLineOfSightToTarget())
        {
            stateMachine.ChangeState(enemyRange.GetAttack());
        }
    }

    /// <summary>
    /// What to do when exiting the ChaseState
    /// </summary>
    public override void Exit()
    {
        // nothing special flight, just let flight do its thing
    }
}
