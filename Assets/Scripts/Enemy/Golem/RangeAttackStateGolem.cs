using System.Collections;
using UnityEngine;

/// <summary>
/// Class - Represents the Attack State for Melee Enemy.
/// Uses swept-sphere collision during the leap
/// </summary>
public class RangeAttackStateGolem : EnemyState
{
    private EnemyGolem enemyGolem;
    private float timer;
    private float totalAnimationTime; // How long the whole state lasts
    private float hitFrameTime; // The exact moment the throw happens

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
        timer = enemyGolem.throwAnim.length / enemyGolem.golemAnim.GetFloat("ThrowAnimSpeedMultiplier");
        //totalAnimationTime = enemyGolem.throwAnim.length / enemyGolem.golemAnim.GetFloat("ThrowAnimSpeedMultiplier");
        //hitFrameTime = enemyGolem.throwAnim.events[0].time / enemyGolem.golemAnim.GetFloat("ThrowAnimSpeedMultiplier");
        enemyGolem.golemAnim.SetTrigger("AttackThrow");
    }

    public override void Update()
    {
        if (enemy.target == null)
        {
            stateMachine.ChangeState(enemyGolem.GetIdle());
            return;
        }

        // Keep facing the player during windup
        enemyGolem.FaceTargetSmooth(enemyGolem.turnSpeedWhileAiming);

        timer -= Time.deltaTime;
        if (timer <= 0f)
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
}
