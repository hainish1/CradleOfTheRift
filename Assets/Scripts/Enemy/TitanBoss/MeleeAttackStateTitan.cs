using UnityEngine;

/// <summary>
/// Class - Placeholder for the Golem enemy melee logic.
/// </summary>
public class MeleeAttackStateTitan : EnemyState
{
    private EnemyTitan enemyTitan;
    private float stateTimer;
    private float totalAnimationTime; // How long the whole state lasts
    private float hitFrameTime; // The exact moment the punch lands


    public MeleeAttackStateTitan(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyTitan = enemy as EnemyTitan;
    }

    public override void Enter()
    {
        enemyTitan.BeginAttackLock();
        enemyTitan.PauseAgent();
        enemyTitan.RefreshAttackAnimationSpeeds();
        stateTimer = 0f;

        // 50% chance to perform slam attack or sweep attack.
        if (Random.value < 0.5f) // Slam attack.
        {
            float slamSpeed = enemyTitan.slamAnimSpeedMultiplier > 0f ? enemyTitan.slamAnimSpeedMultiplier : 1f;
            totalAnimationTime = enemyTitan.GetAnimationDuration(enemyTitan.slamAnim, slamSpeed);
            hitFrameTime = enemyTitan.GetAnimationEventTime(enemyTitan.slamAnim, slamSpeed, "TitanSlamDamage", "GolemSlamDamage");
            enemyTitan.TryPlayAttackTrigger("AttackSlam", "AttackThrow", "AttackBarrage", "AttackSlam", "AttackSweep");
        }
        else // Sweep attack.
        {
            float sweepSpeed = enemyTitan.sweepAnimSpeedMultiplier > 0f ? enemyTitan.sweepAnimSpeedMultiplier : 1f;
            totalAnimationTime = enemyTitan.GetAnimationDuration(enemyTitan.sweepAnim, sweepSpeed);
            hitFrameTime = enemyTitan.GetAnimationEventTime(enemyTitan.sweepAnim, sweepSpeed, "TitanSweepDamageLeft", "TitanSweepDamageRight");
            enemyTitan.TryPlayAttackTrigger("AttackSweep", "AttackThrow", "AttackBarrage", "AttackSlam", "AttackSweep");
        }
    }

    public override void Update()
    {
        stateTimer += Time.deltaTime;

        // Keep turning to face the player until melee attack
        if (stateTimer < hitFrameTime)
        {
            enemyTitan.FaceTargetSmooth(enemyTitan.turnSpeedWhileAiming);
        }

        // Leave the state when the full animation time is over
        if (stateTimer >= totalAnimationTime)
        {
            // Set melee cooldown
            enemyTitan.nextAttackAllowed = Time.time + enemyTitan.attackCooldown;
            
            // Go to recovery to pause and wander
            stateMachine.ChangeState(enemyTitan.GetRecovery());
        }
    }

    public override void Exit()
    {
        enemyTitan.EndAttackLock();
        enemyTitan.ResumeAgent();
    }
}
