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
            enemyTitan.golemAnim.ResetTrigger("AttackBarrage");
            enemyTitan.golemAnim.SetTrigger("AttackThrow");
        }
        else // Barrage attack.
        {
            float safeBarrageSpeed = enemyTitan.barrageAnimSpeedMultiplier > 0f ? enemyTitan.barrageAnimSpeedMultiplier : 1f;
            totalAnimationTime = GetAnimationDuration(enemyTitan.barrageAnim, safeBarrageSpeed);
            aimDuration = GetAnimationEventTime(enemyTitan.barrageAnim, safeBarrageSpeed, "TitanRockBarrage", "RockBarrage");
            enemyTitan.golemAnim.ResetTrigger("AttackThrow");
            enemyTitan.golemAnim.SetTrigger("AttackBarrage");
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
        enemyTitan.ResumeAgent();
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
