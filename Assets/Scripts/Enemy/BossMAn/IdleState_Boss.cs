using UnityEngine;

public class IdleState_Boss : EnemyState
{
    private EnemyBoss_SS boss;
    private float idleDuration;
    private float idleTimer;
    public IdleState_Boss(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        boss = enemy as EnemyBoss_SS;
    }

    public override void Enter()
    {
        base.Enter();
        idleDuration = boss.idleTime;
        idleTimer = 0f;
        if (boss.agent != null) boss.agent.isStopped = true;
    }

    public override void Update()
    {
        base.Update();

        idleTimer += Time.deltaTime;

        FaceTarget(boss.turnSpeed);

        if(idleTimer >= idleDuration)
        {
            // stateMachine.ChangeState(boss.GetBombState());
            stateMachine.ChangeState(boss.GetExploisionState());
            return;
        }
    }
}