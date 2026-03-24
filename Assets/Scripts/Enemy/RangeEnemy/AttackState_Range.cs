using UnityEngine;


/// <summary>
/// Class - Represents the Attack State for Range Enemy.
/// </summary>
public class AttackState_Range : EnemyState
{
    EnemyRange enemyRange;

    float timer;

    public AttackState_Range(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyRange = enemy as EnemyRange;
    }

    /// <summary>
    /// when enter, hold position, stop agent, reset ammo, start with attackDelay
    /// </summary>
    public override void Enter()
    {
        // holdHorizontalPosition keeps the enemy in place via flight logic;
        enemyRange.SetHorizontalPosition(true);
        enemyRange.ResetFlightVelocity();
        enemyRange.SafeStopAgent();
        enemyRange.currentShotsRemaining = enemyRange.shotsPerSet;
        timer = Random.Range(enemyRange.attackDelayMin, enemyRange.attackDelayMax);
    }

    public override void Exit()
    {
        enemyRange.SetHorizontalPosition(false);
        // brief cooldown so Chase doesn't re-enter Attack immediately
        enemyRange.nextAttackTime = Time.time + Random.Range(enemyRange.attackDelayMin, enemyRange.attackDelayMax);
    }

    /// <summary>
    /// after each shot, wait recoveryDuration before next shot
    /// </summary>
    public override void Update()
    {
        if (enemy.target == null)
        {
            stateMachine.ChangeState(enemyRange.GetIdle());
            return;
        }

        enemyRange.FaceTargetSmooth(enemyRange.turnSpeedWhileAiming);


        float distSqr = (enemy.target.position - enemy.transform.position).sqrMagnitude;
        float rangeSqr = enemyRange.attackRange * enemyRange.attackRange;
        if (distSqr > rangeSqr * 1.2f || !enemyRange.HasLineOfSightToTarget()) // if enemy has lost sight to target, change back to CHASE state
        {
            stateMachine.ChangeState(enemyRange.GetChase());
            return;
        }

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            enemyRange.FireAtTarget();
            enemyRange.currentShotsRemaining--;

            if (enemyRange.currentShotsRemaining > 0)
            {
                timer = Random.Range(enemyRange.recoveryDurationMin, enemyRange.recoveryDurationMax); 
            }
            else
            {
                stateMachine.ChangeState(enemyRange.GetRecovery()); // out of ammo so reload
            }
        }
    }
}
