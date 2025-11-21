using UnityEngine;


/// <summary>
/// Class - Represents the Attack State for Range Enemy
/// </summary>
public class AttackState_Range : EnemyState
{
    EnemyRange enemyRange;


    private float nextShootTime;
    // private float endTime;
    private int ammoLeft; // no of shots left

    public AttackState_Range(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyRange = enemy as EnemyRange;
    }

    /// <summary>
    /// When entering Attack State, take control from navmeshagent and control it manually
    /// </summary>
    public override void Enter()
    {
        if (enemy.agent != null)
        {
            enemy.agent.isStopped = true; // pause my brother a bit
            enemy.agent.velocity = Vector3.zero;
        }

        // endTime = Time.time + Mathf.Max(enemyRange.fireCooldown * 1.5f, 0.3f);
        ammoLeft = enemyRange.numberOfOrbs; // initialize it
        nextShootTime = Time.time; // first shoot immedietly
    }

    /// <summary>
    /// While inside attack state, look towards the player and try shooting if fireCooldown allows. Else switch to recovery state
    /// </summary>
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
        if (ammoLeft > 0 && Time.time >= nextShootTime)
        {
            enemyRange.FireOnce();
            ammoLeft--;
            nextShootTime = Time.time + enemyRange.fireCooldown;
            // enemyRange.nextAttackAllowed = Time.time + enemyRange.fireCooldown * 0.1f; // for re-entry

        }

        if(ammoLeft == 0)
        {
            stateMachine.ChangeState(enemyRange.GetRecovery());
            return;
        }


        float distance = Vector3.Distance(enemy.transform.position, enemy.target.position);
        if (distance >= enemyRange.attackRange * 1.2f)
        {
            stateMachine.ChangeState(enemyRange.GetRecovery());
        }
    }
}
