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
    private bool hasThrown;

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
        timer = Random.Range(enemyGolem.minWindupTime, enemyGolem.maxWindupTime);
        hasThrown = false;

        // Trigger throwing animation (not yet implemented)
        //enemy.animator.SetTrigger("ThrowRock");
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
        
        if (!hasThrown)
        {
            timer -= Time.deltaTime;

            if (timer <= 0f)    // Hardcoded windup that should get replaced with animation events later
            {
                enemyGolem.ThrowRock();
                hasThrown = true;
                enemyGolem.golemAnim.SetTrigger("AttackRanged");

                // Set cooldown and go to recovery
                enemy.nextAttackAllowed = Time.time + enemyGolem.attackCooldown;
                stateMachine.ChangeState(enemyGolem.GetRecovery());
            }
        }
    }

    public override void Exit()
    {
        enemyGolem.ResumeAgent();
    }
}
