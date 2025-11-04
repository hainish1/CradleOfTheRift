using UnityEngine;

public class ChaseState_ExplodingEnemy : EnemyState
{
    private EnemyExploding enemyExploding;
    private float timer;

    public ChaseState_ExplodingEnemy(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        this.enemyExploding = enemy as EnemyExploding;
    }


    public override void Enter()
    {
        base.Enter();
        timer = enemyExploding.explosionTimer;
        if (enemyExploding.agent != null)
        {
            enemyExploding.agent.isStopped = false; // enab it
        }
    }

    public override void Update()
    {
        base.Update();

        timer -= Time.deltaTime;
        if (enemyExploding.target != null)
        {
            SetAgentDestination(enemyExploding.target.position); // set plsyer as position
        }

        if(timer <= 0f)
        {
            enemyExploding.BeginExplosion();
        }
    }
}