using UnityEngine;


/// <summary>
/// Class - Represents the Attack State for Range Enemy
/// </summary>
public class AttackState_Range : EnemyState
{
    EnemyRange enemyRange;

    float delayTimer;
    bool hasFired;

    private float nextShootTime;
    private float endTime;

    public AttackState_Range(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyRange = enemy as EnemyRange;
    }

    /// <summary>
    /// When entering Attack State, take control from navmeshagent and control it manually
    /// </summary>
    public override void Enter()
    {
        enemyRange.SafeStopAgent();
        enemyRange.SetHorizontalPosition(true);
        hasFired = false;
        delayTimer = 0.5f; // tiny delay before shooting
    }

    public override void Exit()
    {
        enemyRange.SetHorizontalPosition(false);
    }

    /// <summary>
    /// While inside attack state, look towards the player and try shooting if fireCooldown allows. Else switch to recovery state
    /// </summary>
    public override void Update()
    {
        if (enemy.target == null)
        {
            stateMachine.ChangeState(enemyRange.GetIdle());
            return;
        }

        enemyRange.FaceTargetSmooth(enemyRange.turnSpeedWhileAiming);
        delayTimer -= Time.deltaTime;

        if(delayTimer <= 0f && !hasFired)
        {
            enemyRange.FireAtTarget();
            hasFired = true;

            stateMachine.ChangeState(enemyRange.GetRecovery());
        }
    }
}
