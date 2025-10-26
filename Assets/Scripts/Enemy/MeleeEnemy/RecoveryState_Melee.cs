using UnityEngine;

/// <summary>
/// Class - Represents the Recovery State for Melee Enemy
/// </summary>
public class RecoveryState_Melee : EnemyState
{
    private EnemyMelee enemyMelee;

    float endTime;
    private bool needsRetreat;

    public RecoveryState_Melee(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyMelee = enemy as EnemyMelee;
    }


    /// <summary>
    /// What to do when enemy enters the Recovery state
    /// </summary>
    public override void Enter()
    {
        endTime = Time.time + enemyMelee.recoveryTime; // post attack pause
        if (enemy.agent != null) enemy.agent.isStopped = false;

        //check if too close to the player
        if (enemy.target != null)
        {
            float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.target.position);
            needsRetreat = distanceToPlayer < enemyMelee.minAttackDistance;
        }
        else
        {
            needsRetreat = false;
        }
    }

    /// <summary>
    /// Wait for recovery time to finish, then if player is in aggro range, chase it, else switch to idle
    /// </summary>
    public override void Update()
    {

        if (enemy.target == null)
        {
            if (Time.time >= endTime)
            {
                stateMachine.ChangeState(enemyMelee.GetIdle());
            }
            return;
        }

        float currentDistance = Vector3.Distance(enemy.transform.position, enemy.target.position);
        if (needsRetreat && currentDistance < enemyMelee.minAttackDistance)
        {
            // calc retreat position
            Vector3 awayFromPlayer = enemy.transform.position - enemy.target.position;
            awayFromPlayer.y = 0f;
            if (awayFromPlayer.sqrMagnitude > 0.0001f)
            {
                awayFromPlayer.Normalize();
                Vector3 retreatPosition = enemy.target.position + awayFromPlayer * enemyMelee.leapAttackRange;

                if (enemy.agent != null && enemy.agent.enabled)
                {
                    enemy.agent.SetDestination(retreatPosition);
                }
            }

        }
        else
            {
                needsRetreat = false;
                if (Time.time >= endTime)
                {
                    if (PlayerInAggressionRange())
                    {
                        stateMachine.ChangeState(enemyMelee.GetChase());
                    }
                    else
                    {
                        stateMachine.ChangeState(enemyMelee.GetIdle());
                    }
                }
            }
    }


}
