using UnityEngine;

public class AttackState_Range : EnemyState
{
    EnemyRange enemyRange;
    

    private float nextShootTime;
    private float endTime;

    public AttackState_Range(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyRange = enemy as EnemyRange;
    }


    public override void Enter()
    {
        if (enemy.agent != null)
        {
            enemy.agent.isStopped = true; // pause my brother a bit
            enemy.agent.velocity = Vector3.zero;
        }

        endTime = Time.time + Mathf.Max(enemyRange.fireCooldown * 1.5f, 0.3f);
        nextShootTime = Time.time; // first shoot immedietly
    }


    public override void Update()
    {
        if (enemy.target == null)
        {
            stateMachine.ChangeState(enemyRange.GetIdle());
            return;
        }

        Vector3 direction = enemy.target.position - enemy.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(direction);
            enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, look, enemyRange.turnSpeedWhileAiming * Time.deltaTime); // turn towards

        }

        // now fire
        if (Time.time >= nextShootTime)
        {
            enemyRange.FireOnce();
            nextShootTime = Time.time + enemyRange.fireCooldown;
            enemyRange.nextAttackAllowed = Time.time + enemyRange.fireCooldown * 0.75f; // for re-entry
        }


        float distance = Vector3.Distance(enemy.transform.position, enemy.target.position);
        if (distance >= enemyRange.attackRange * 1.2f || Time.time >= endTime)
        {
            stateMachine.ChangeState(enemyRange.GetRecovery());
        }
    }
}
