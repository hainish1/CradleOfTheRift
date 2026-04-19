using UnityEngine;

/// <summary>
/// Class - Placeholder for the Golem enemy melee logic.
/// </summary>
public class MeleeAttackStateGolem : EnemyState
{
    private EnemyGolem enemyGolem;
    private float stateTimer;
    private float totalAnimationTime;
    private float hitFrameTime;

    public MeleeAttackStateGolem(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyGolem = enemy as EnemyGolem;
    }

    public override void Enter()
    {
        enemyGolem.BeginAttackLock();
        enemyGolem.PauseAgent();
        enemyGolem.RefreshAttackAnimationSpeeds();
        stateTimer = 0f;

        float slamSpeed = enemyGolem.slamAnimSpeedMultiplier > 0f ? enemyGolem.slamAnimSpeedMultiplier : 1f;
        totalAnimationTime = enemyGolem.GetAnimationDuration(enemyGolem.slamAnim, slamSpeed);
        hitFrameTime = enemyGolem.GetAnimationEventTime(enemyGolem.slamAnim, slamSpeed, "GolemSlamDamage");

        enemyGolem.TryPlayAttackTrigger("AttackSlam", "AttackThrow", "AttackSlam");
    }

    public override void Update()
    {
        stateTimer += Time.deltaTime;

        // Keep turning to face the player until melee attack
        if (enemy.target != null && stateTimer < hitFrameTime)
        {
            enemyGolem.FaceTargetSmooth(enemyGolem.turnSpeedWhileAiming);
        }

        // Leave the state when the full animation time is over
        if (stateTimer >= totalAnimationTime)
        {
            // Set melee cooldown
            enemyGolem.nextAttackAllowed = Time.time + enemyGolem.attackCooldown;

            // If no player then idle, otherwise go to recovery to pause and wander
            if (enemy.target == null)
            {
                stateMachine.ChangeState(enemyGolem.GetIdle());
            }
            else
            {
                stateMachine.ChangeState(enemyGolem.GetRecovery());
            }
        }
    }

    public override void Exit()
    {
        enemyGolem.EndAttackLock();
        enemyGolem.ResumeAgent();
    }
}
