using System.Collections;
using UnityEngine;

/// <summary>
/// Class - Represents the Attack State for Melee Enemy.
/// Uses swept-sphere collision during the leap
/// </summary>
public class RangeAttackStateTitan : EnemyState
{
    private EnemyTitan enemyTitan;
    private float stateTimer;
    private float totalAnimationTime;
    private float aimDuration;

    public RangeAttackStateTitan(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyTitan = enemy as EnemyTitan;
    }

    /// <summary>
    /// Enter: pause agent, reset flags, begin attack with windup.
    /// </summary>
    public override void Enter()
    {
        enemyTitan.BeginAttackLock();
        enemyTitan.PauseAgent();
        enemyTitan.RefreshAttackAnimationSpeeds();
        stateTimer = 0f;

        // 50% chance to perform throw attack or barrage attack.
        float rand = Random.value;
        if (rand < 0.5f) // Throw attack.
        {
            float safeThrowSpeed = enemyTitan.throwAnimSpeedMultiplier > 0f ? enemyTitan.throwAnimSpeedMultiplier : 1f;
            totalAnimationTime = GetAnimationDuration(enemyTitan.throwAnim, safeThrowSpeed);
            aimDuration = GetAnimationEventTime(enemyTitan.throwAnim, safeThrowSpeed, "TitanRockThrow", "GolemRockThrow", "ThrowRock");
            enemyTitan.TryPlayAttackTrigger("AttackThrow", "AttackThrow", "AttackBarrage", "AttackSlam", "AttackSweep");
        }
        else // Barrage attack.
        {
            float safeBarrageSpeed = enemyTitan.barrageAnimSpeedMultiplier > 0f ? enemyTitan.barrageAnimSpeedMultiplier : 1f;
            totalAnimationTime = GetAnimationDuration(enemyTitan.barrageAnim, safeBarrageSpeed);
            aimDuration = GetAnimationEventTime(enemyTitan.barrageAnim, safeBarrageSpeed, "TitanRockBarrage", "RockBarrage");
            enemyTitan.TryPlayAttackTrigger("AttackBarrage", "AttackThrow", "AttackBarrage", "AttackSlam", "AttackSweep");
        }
    }

    public override void Update()
    {
        if (enemy.target == null)
        {
            stateMachine.ChangeState(enemyTitan.GetIdle());
            return;
        }

        stateTimer += Time.deltaTime;

        // Aim through the release frame, then hold the pose while the rest of the animation finishes.
        if (stateTimer < aimDuration)
        {
            enemyTitan.FaceTargetSmooth(enemyTitan.turnSpeedWhileAiming);
        }

        if (stateTimer >= totalAnimationTime)
        {
            // Set cooldown and go to recovery
            enemy.nextAttackAllowed = Time.time + enemyTitan.attackCooldown;
            stateMachine.ChangeState(enemyTitan.GetRecovery());
        }
    }

    public override void Exit()
    {
        enemyTitan.EndAttackLock();
        enemyTitan.ResumeAgent();
    }

    private float GetAnimationDuration(AnimationClip clip, float speedMultiplier)
    {
        return enemyTitan.GetAnimationDuration(clip, speedMultiplier);
    }

    private float GetAnimationEventTime(AnimationClip clip, float speedMultiplier, params string[] functionNames)
    {
        return enemyTitan.GetAnimationEventTime(clip, speedMultiplier, functionNames);
    }
}
