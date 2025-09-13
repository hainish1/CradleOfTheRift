using UnityEngine;

public class AttackState_Melee : EnemyState
{
    private EnemyMelee enemyMelee;
    float endTime;

    public AttackState_Melee(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyMelee = enemy as EnemyMelee;

    }

    public override void Enter()
    {
        // quick time window to do a hit
        endTime = Time.time + 0.15f;
        if (enemy.agent != null)
        {
            enemy.agent.isStopped = true;
            enemy.agent.velocity = Vector3.zero;
        }

        // apply the hit if in range
        if (enemy.target != null && PlayerInAttackRange(enemyMelee.attackRange + .1f))
        {
            // try to do a simple hit
            // TODO I Still need to figure this part out since I dont really have a rigidbody on the player

            // APPLY DAMAGE (TODO - NEED IDamageable)

            // KNOCKBACK
            var pm = enemy.target.GetComponentInParent<PlayerMovement>();
            if (pm != null)
            {
                Vector3 direction = (enemy.target.position - enemy.transform.position);
                direction.y = 0f; // we aren't pushing the player up
                if (direction.sqrMagnitude > 0.0001f)
                {
                    direction.Normalize();
                    pm.ApplyImpulse(direction * enemyMelee.knockbackPower);
                }

            }

            enemy.nextAttackAllowed = Time.time + enemy.attackCooldown;
        }
    }

    public override void Update()
    {
        FaceTarget(enemy.turnSpeed);
        if (Time.time >= endTime)
        {
            stateMachine.ChangeState(enemyMelee.GetRecovery());
        }
    }

    public override void Exit()
    {
        enemy.agent.isStopped = false;
    }
}
