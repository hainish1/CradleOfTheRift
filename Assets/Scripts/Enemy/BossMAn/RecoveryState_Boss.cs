using UnityEngine;

public class RecoveryState_Boss : EnemyState
{
    private EnemyBoss_SS boss;
    private float recoveryTimer = 1f;

    public RecoveryState_Boss(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        this.boss = enemy as EnemyBoss_SS;
    }

    public override void Enter()
    {
        base.Enter();
        recoveryTimer = 1f;
    }

    public override void Update()
    {
        base.Update();
        recoveryTimer -= Time.deltaTime;

        if(recoveryTimer <= 0)
        {
            stateMachine.ChangeState(boss.GetIdle());
        }
    }
}