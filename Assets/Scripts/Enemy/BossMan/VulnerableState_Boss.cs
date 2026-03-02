using UnityEngine;

public class VulnerableState_Boss : EnemyState
{
    private EnemyBoss_SS boss;
    private float timer;

    public VulnerableState_Boss(Enemy enemy, EnemyStateMachine stateMachine)
        : base(enemy, stateMachine)
    {
        boss = enemy as EnemyBoss_SS;
    }

    public override void Enter()
    {
        base.Enter();
        timer = boss.postExplosionVulnerableTime;
        
        if (boss.agent != null && boss.agent.isOnNavMesh)
        {
            boss.agent.isStopped = true;
            boss.agent.ResetPath();
        }
    }

    public override void Update()
    {
        base.Update();
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            stateMachine.ChangeState(boss.GetIdle());
        }
    }

    public override void Exit()
    {
        base.Exit();
    }
}
