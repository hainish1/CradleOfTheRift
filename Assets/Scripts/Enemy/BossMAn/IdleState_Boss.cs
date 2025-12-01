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
            // stateMachine.ChangeState(boss.GetExploisionState());
            // stateMachine.ChangeState(boss.GetLeapAttackState());

            // alrgith now here I am keeping it pretty random, which attack is chosen, but we can like mess with it if we want
            float random = Random.value;
            if (random < 0.5f)
            {
                stateMachine.ChangeState(boss.GetBombState());
            }
            else if (random < 0.8f && !boss.IsPlayerTooFar() && !boss.IsPlayerTooClose()) // ensure player is not too far or too close to leap
            {
                stateMachine.ChangeState(boss.GetLeapAttackState());
            }
            else
            {
                stateMachine.ChangeState(boss.GetExploisionState());
            }
            return;
        }
    }
}