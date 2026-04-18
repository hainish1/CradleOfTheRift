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
        enemyGolem.PauseAgent();
        enemyGolem.RefreshAttackAnimationSpeeds();

        float throwSpeed = enemyGolem.throwAnimSpeedMultiplier > 0f ? enemyGolem.throwAnimSpeedMultiplier : 1f;
        totalAnimationTime = GetAnimationDuration(enemyGolem.throwAnim, throwSpeed);
        aimDuration = GetAnimationEventTime(enemyGolem.throwAnim, throwSpeed, "GolemRockThrow", "ThrowRock");
        stateTimer = 0f;

        enemyGolem.golemAnim.SetTrigger("AttackThrow");
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
        enemyGolem.ResumeAgent();
    }

    private float GetAnimationDuration(AnimationClip clip, float speedMultiplier)
    {
        if (clip == null)
        {
            return 0.1f;
        }

        float safeSpeed = speedMultiplier > 0f ? speedMultiplier : 1f;
        return clip.length / safeSpeed;
    }

    private float GetAnimationEventTime(AnimationClip clip, float speedMultiplier, params string[] functionNames)
    {
        if (clip == null)
        {
            return 0f;
        }

        AnimationEvent[] events = clip.events;
        for (int i = 0; i < events.Length; i++)
        {
            for (int j = 0; j < functionNames.Length; j++)
            {
                if (events[i].functionName == functionNames[j])
                {
                    return events[i].time / Mathf.Max(0.01f, speedMultiplier);
                }
            }
        }

        return GetAnimationDuration(clip, speedMultiplier);
    }
}
