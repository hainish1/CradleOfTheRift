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
        enemyTitan.PauseAgent();
        stateTimer = 0f;

        // 50% chance to perform slam attack or sweep attack.
        float rand = Random.value;
        if (rand < 0.5f) // Slam attack.
        {
            totalAnimationTime = enemyTitan.slamAnim.length;
            hitFrameTime = enemyTitan.slamAnim.events[0].time;
            enemyTitan.golemAnim.SetTrigger("AttackSlam");
        }
        else // Sweep attack.
        {
            totalAnimationTime = enemyTitan.sweepAnim.length;
            hitFrameTime = enemyTitan.sweepAnim.events[0].time;
            enemyTitan.golemAnim.SetTrigger("AttackSweep");
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
        enemyTitan.ResumeAgent();
    }
}
