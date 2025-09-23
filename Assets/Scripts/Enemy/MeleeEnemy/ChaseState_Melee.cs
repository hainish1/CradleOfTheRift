using UnityEngine;

public class ChaseState_Melee : EnemyState
{
    private EnemyMelee enemyMelee;

    private bool dragging;
    private float phaseTimer;

    public ChaseState_Melee(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyMelee = enemy as EnemyMelee;

    }

    public override void Enter()
    {
        if (enemy != null)
        {
            if (enemy.agent != null)
            {
                enemy.agent.isStopped = false;
                enemy.agent.speed = enemyMelee.dragSpeed;
                dragging = true;
                phaseTimer = enemyMelee.dragDuration;
            }
        }
    }

    public override void Update()
    {
        if (enemy.target == null) return; // if there is not target then nothing to chase

        phaseTimer -= Time.deltaTime;
        if (dragging)
        {
            enemy.agent.isStopped = false;
            enemy.agent.speed = enemyMelee.dragSpeed;
            SetAgentDestination(enemy.target.position); // give the AI a position to chase  
        }
        else
        {
            // rest phase
            enemy.agent.isStopped = true;
            enemy.agent.velocity = Vector3.zero;
            FaceTarget(enemy.turnSpeed);

        }

        if (phaseTimer <= 0f)
        {
            dragging = !dragging;
            phaseTimer = dragging ? enemyMelee.dragDuration : enemyMelee.restDuration;
        }

        // trying to enter attack state
        if (PlayerInAttackRange(enemyMelee.attackRange) && Time.time >= enemy.nextAttackAllowed)
        {
            stateMachine.ChangeState(enemyMelee.GetAttack());
        }

    }
}
