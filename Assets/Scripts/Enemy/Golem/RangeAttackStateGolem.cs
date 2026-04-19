using System.Collections;
using UnityEngine;

/// <summary>
/// Class - Represents the Attack State for Melee Enemy.
/// Uses swept-sphere collision during the leap
/// </summary>
public class RangeAttackStateGolem : EnemyState
{
    private EnemyGolem enemyGolem;
    private float stateTimer;
    private float totalAnimationTime;
    private float aimDuration;

    public RangeAttackStateGolem(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyGolem = enemy as EnemyGolem;
    }

    /// <summary>
    /// Enter: pause agent, reset flags, begin attack with windup.
    /// </summary>
    public override void Enter()
    {
        enemyGolem.BeginAttackLock();
        enemyGolem.PauseAgent();
        enemyGolem.RefreshAttackAnimationSpeeds();

        float throwSpeed = enemyGolem.throwAnimSpeedMultiplier > 0f ? enemyGolem.throwAnimSpeedMultiplier : 1f;
        totalAnimationTime = GetAnimationDuration(enemyGolem.throwAnim, throwSpeed);
        aimDuration = GetAnimationEventTime(enemyGolem.throwAnim, throwSpeed, "GolemRockThrow", "ThrowRock");
        stateTimer = 0f;

        enemyGolem.TryPlayAttackTrigger("AttackThrow", "AttackThrow", "AttackSlam");
    }


    public override void Update()
    {
        if (enemy.target == null)
        {
            stateMachine.ChangeState(enemyGolem.GetIdle());
            return;
        }

        stateTimer += Time.deltaTime;

        // Aim during the windup and release, but stop tracking during the hand-regain phase.
        if (stateTimer < aimDuration)
        {
            enemyGolem.FaceTargetSmooth(enemyGolem.turnSpeedWhileAiming);
        }

        if (stateTimer >= totalAnimationTime)
        {
            // Set cooldown and go to recovery
            enemy.nextAttackAllowed = Time.time + enemyGolem.attackCooldown;
            stateMachine.ChangeState(enemyGolem.GetRecovery());
        }
    }

    public override void Exit()
    {
        enemyGolem.EndAttackLock();
        enemyGolem.ResumeAgent();
    }

    private float GetAnimationDuration(AnimationClip clip, float speedMultiplier)
    {
        return enemyGolem.GetAnimationDuration(clip, speedMultiplier);
    }

    private float GetAnimationEventTime(AnimationClip clip, float speedMultiplier, params string[] functionNames)
    {
        return enemyGolem.GetAnimationEventTime(clip, speedMultiplier, functionNames);
    }
}
