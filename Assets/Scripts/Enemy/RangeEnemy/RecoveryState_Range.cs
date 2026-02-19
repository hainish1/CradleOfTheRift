using UnityEngine;


/// <summary>
/// Class - Represents the Reload State for Range Enemy.
/// waits reloadTime, then re enables orbs and transitions to chase
/// </summary>
public class RecoveryState_Range : EnemyState
{
    EnemyRange enemyRange;
    float timer;

    public RecoveryState_Range(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyRange = enemy as EnemyRange;
    }

    /// <summary>
    /// when entering reload state, start the reload timer and slow-drift
    /// </summary>
    public override void Enter()
    {
        timer = enemyRange.reloadTime;
        // flight logic handles all movement — no agent stop/resume needed
    }

    /// <summary>
    /// count down reload timer, face player, when done, reset orbs and chase.
    /// </summary>
    public override void Update()
    {
        timer -= Time.deltaTime;

        enemyRange.FaceTargetSmooth(enemy.turnSpeed * 0.5f);

        if (timer <= 0f)
        {
            enemyRange.ReloadOrbs(); // re enable visuals and reset ammo

            if (enemy.target != null)
                stateMachine.ChangeState(enemyRange.GetChase());
            else
                stateMachine.ChangeState(enemyRange.GetIdle());
        }
    }
}
