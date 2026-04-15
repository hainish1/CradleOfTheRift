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
    private bool hasThrown;

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
        timer = Random.Range(enemyTitan.minWindupTime, enemyTitan.maxWindupTime);
        hasThrown = false;

        // Trigger throwing animation (not yet implemented)
        //enemy.animator.SetTrigger("ThrowRock");
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
        
        if (!hasThrown)
        {
            timer -= Time.deltaTime;

            if (timer <= 0f)    // Hardcoded windup that should get replaced with animation events later
            {
                enemyTitan.RockBarrage();
                hasThrown = true;
                //enemyTitan.golemAnim.SetTrigger("AttackRanged");

                // Set cooldown and go to recovery
                enemy.nextAttackAllowed = Time.time + enemyTitan.attackCooldown;
                stateMachine.ChangeState(enemyTitan.GetRecovery());
            }
        }
    }

    public override void Exit()
    {
        enemyTitan.ResumeAgent();
    }
}
