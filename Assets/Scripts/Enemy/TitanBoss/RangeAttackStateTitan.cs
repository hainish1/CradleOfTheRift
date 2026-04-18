using System.Collections;
using UnityEngine;

/// <summary>
/// Class - Represents the Attack State for Melee Enemy.
/// Uses swept-sphere collision during the leap
/// </summary>
public class RangeAttackStateTitan : EnemyState
{
    private EnemyTitan enemyTitan;
    private float timer;
    private float totalAnimationTime; // How long the whole state lasts
    private float hitFrameTime; // The exact moment the punch lands

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

        // 50% chance to perform throw attack or barrage attack.
        float rand = Random.value;
        if (rand < 0.5f) // Throw attack.
        {
            timer = enemyTitan.throwAnim.length / enemyTitan.golemAnim.GetFloat("ThrowAnimSpeedMultiplier");
            //hitFrameTime = enemyTitan.throwAnim.events[0].time;
            enemyTitan.golemAnim.SetTrigger("AttackThrow");
        }
        else // Barrage attack.
        {
            timer = enemyTitan.barrageAnim.length / enemyTitan.golemAnim.GetFloat("BarrageAnimSpeedMultiplier");
            //hitFrameTime = enemyTitan.barrageAnim.events[0].time;
            enemyTitan.golemAnim.SetTrigger("AttackBarrage");
        }

        Debug.Log($"timer: {timer}");
        Debug.Log($"enemyTitan.throwAnim.length: {enemyTitan.throwAnim.length} | enemyTitan.golemAnim.GetFloat(\"ThrowAnimSpeedMultiplier\"): {enemyTitan.golemAnim.GetFloat("ThrowAnimSpeedMultiplier")}");
        Debug.Log($"enemyTitan.barrageAnim.length: {enemyTitan.barrageAnim.length} | enemyTitan.golemAnim.GetFloat(\"BarrageAnimSpeedMultiplier\"): {enemyTitan.golemAnim.GetFloat("BarrageAnimSpeedMultiplier")}");
    }

    public override void Update()
    {
        if (enemy.target == null)
        {
            stateMachine.ChangeState(enemyTitan.GetIdle());
            return;
        }

        // Keep facing the player during windup
        enemyTitan.FaceTargetSmooth(enemyTitan.turnSpeedWhileAiming);

        timer -= Time.deltaTime;
        if (timer <= 0f)
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
}
