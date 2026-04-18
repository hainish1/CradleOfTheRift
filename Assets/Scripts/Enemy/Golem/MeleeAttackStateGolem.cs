using UnityEngine;

/// <summary>
/// Class - Placeholder for the Golem enemy melee logic.
/// </summary>
public class MeleeAttackStateGolem : EnemyState
{
    private EnemyGolem enemyGolem;
    private float stateTimer;
    private float totalAnimationTime; // How long the whole state lasts
    private float hitFrameTime; // The exact moment the punch lands

    public MeleeAttackStateGolem(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyGolem = enemy as EnemyGolem;
    }

    public override void Enter()
    {
        enemyGolem.PauseAgent();
        //Debug.Log("Golem entered Melee Attack State");
        stateTimer = 0f;
        totalAnimationTime = enemyGolem.slamAnim.length / enemyGolem.golemAnim.GetFloat("SlamAnimSpeedMultiplier");
        hitFrameTime = enemyGolem.slamAnim.events[0].time / enemyGolem.golemAnim.GetFloat("SlamAnimSpeedMultiplier");
        enemyGolem.golemAnim.SetTrigger("AttackMelee");
    }

    public override void Update()
    {
        stateTimer += Time.deltaTime;

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
