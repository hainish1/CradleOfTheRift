using UnityEngine;


/// <summary>
/// Class - Represents the Recovery State for Range Enemy
/// </summary>
public class RecoveryState_Range : EnemyState
{
    EnemyRange enemyRange;
    float timer;
    private float endTime;

    public RecoveryState_Range(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyRange = enemy as EnemyRange;
    }

    /// <summary>
    /// When entering recovery state, set the recovery time 
    /// </summary>
    public override void Enter()
    {
        timer = enemyRange.recoveryDuration;


        // endTime = Time.time + enemyRange.recoveryTime; // set
        // if (enemy.agent != null) enemy.agent.isStopped = false;
        if(enemy.agent != null && enemy.agent.isOnNavMesh)
        {
            enemy.agent.speed = enemyRange.chaseSpeed * 0.5f; // slow drift
        }

        enemyRange.nextShootTime = Time.time + enemyRange.fireCooldown;
    }

    /// <summary>
    /// Check if recovery time is finished, then if player is in aggro range, switch to chase state, else switch to idle state
    /// </summary>
    public override void Update()
    {
        // if (Time.time >= endTime)
        // {
        //     if (PlayerInAggressionRange()) // if the player is still in aggression range
        //     {
        //         stateMachine.ChangeState(enemyRange.GetChase());
        //     }
        //     else
        //     {
        //         stateMachine.ChangeState(enemyRange.GetIdle());
        //     }
        // }

        timer -= Time.deltaTime;

        enemyRange.FaceTargetSmooth(enemy.turnSpeed * 0.5f);

        if(timer <= 0f)
        {
            stateMachine.ChangeState(enemyRange.GetChase()); // it may switch to attack from chase
        }
    }



}
