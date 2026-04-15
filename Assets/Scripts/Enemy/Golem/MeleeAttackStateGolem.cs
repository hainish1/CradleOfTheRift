using UnityEngine;

/// <summary>
/// Class - Placeholder for the Golem enemy melee logic.
/// </summary>
public class MeleeAttackStateGolem : EnemyState
{
    private EnemyGolem enemyGolem;
    private float stateTimer;
    private bool hasAttacked;

    // Temporary attack timing (replace with animation events later)
    private float totalAnimationTime = 1.0f; // How long the whole state lasts
    private float hitFrameTime = 0.5f;       // The exact moment the punch lands

    public MeleeAttackStateGolem(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyGolem = enemy as EnemyGolem;
    }

    public override void Enter()
    {
        enemyGolem.PauseAgent();
        //Debug.Log("Golem entered Melee Attack State");
        stateTimer = 0f;
        hasAttacked = false;
    }

    public override void Update()
    {
       stateTimer += Time.deltaTime;

        // Keep turning to face the player until melee attack
        if (stateTimer < hitFrameTime)
        {
            enemyGolem.FaceTargetSmooth(enemyGolem.turnSpeedWhileAiming);
        }

        // Play melee attack once when the timer hits the sweet spot
        if (stateTimer >= hitFrameTime && !hasAttacked)
        {
            enemyGolem.MeleeSlamAttack();
            hasAttacked = true;
            enemyGolem.golemAnim.SetTrigger("AttackMelee");
        }

        // Leave the state when the full animation time is over
        if (stateTimer >= totalAnimationTime)
        {
            // Set melee cooldown
            enemyGolem.nextAttackAllowed = Time.time + enemyGolem.attackCooldown;
            
            // Go to recovery to pause and wander
            stateMachine.ChangeState(enemyGolem.GetRecovery());
        }
    }

    public override void Exit()
    {
        enemyGolem.ResumeAgent();
    }
}