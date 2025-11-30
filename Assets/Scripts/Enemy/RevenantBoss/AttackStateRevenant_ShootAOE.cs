using UnityEngine;

public class AttackStateRevenant_ShootAOE : EnemyState
{
    RevenantBossRange bossRange;


    private float nextShootTime;
    private float endTime;

    public AttackStateRevenant_ShootAOE(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        bossRange = enemy as RevenantBossRange;
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

        endTime = Time.time + Mathf.Max(bossRange.fireCooldown * 1.5f, 0.3f);
        nextShootTime = Time.time; // first shoot immedietly
    }

    /// <summary>
    /// While inside attack state, look towards the player and try shooting if fireCooldown allows. Else switch to recovery state
    /// </summary>
    public override void Update()
    {
        if (enemy.target == null)
        {
            stateMachine.ChangeState(bossRange.GetIdle());
            return;
        }

        Vector3 direction = enemy.target.position - enemy.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(direction);
            enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, look, bossRange.turnSpeedWhileAiming * Time.deltaTime); // turn towards

        }

        // now fire
        if (Time.time >= nextShootTime)
        {
            bossRange.FireAOE();
            nextShootTime = Time.time + bossRange.fireCooldown;
            bossRange.nextAttackAllowed = Time.time + bossRange.fireCooldown * 0.75f; // for re-entry
        }


        float distance = Vector3.Distance(enemy.transform.position, enemy.target.position);
        if (distance >= bossRange.attackRange * 1.2f && Time.time >= endTime + bossRange.attackPeriodLength)
        {
            // Chance to go into default recovery or special recovery barrage
            if (Random.value <= 0.5f)
            {
                stateMachine.ChangeState(bossRange.GetRecovery());
                Debug.Log("Revenant: Switching to Recovery State from AOE Attack State");
            }
            else
            {
                stateMachine.ChangeState(bossRange.GetLongRecovery());
                Debug.Log("Revenant: Switching to Special Recovery State from AOE Attack State");
            }
        }
    }
}
