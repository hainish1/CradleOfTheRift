using UnityEngine;

/// <summary>
/// Class - Placeholder for the Golem enemy melee logic.
/// </summary>
public class MeleeAttackStateGolem : EnemyState
{
    private EnemyGolem enemyGolem;
    private float stateTimer;

    public MeleeAttackStateGolem(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyGolem = enemy as EnemyGolem;
    }

    public override void Enter()
    {
        enemyGolem.PauseAgent();
        //Debug.Log("Golem entered Melee Attack State");
        stateTimer = 0f;
        enemyGolem.golemAnim.SetTrigger("AttackMelee");
    }

    public override void Update()
    {
        stateTimer += Time.deltaTime;

        float totalAnimationTime = enemyGolem.meleeAnim.length; // How long the whole state lasts
        float hitFrameTime = enemyGolem.meleeAnim.events[0].time; // The exact moment the punch lands

        // Keep turning to face the player until melee attack
        if (stateTimer < hitFrameTime)
        {
            enemyGolem.FaceTargetSmooth(enemyGolem.turnSpeedWhileAiming);
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
