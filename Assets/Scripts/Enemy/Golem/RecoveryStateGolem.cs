using UnityEngine;

/// <summary>
/// Class - Represents the Recovery State for Melee Enemy
/// </summary>
public class RecoveryStateGolem : EnemyState
{
    private EnemyGolem enemyGolem;

    float endTime;
    private bool needsRetreat;

    public RecoveryStateGolem(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyGolem = enemy as EnemyGolem;
    }


    /// <summary>
    /// What to do when enemy enters the Recovery state
    /// </summary>
    public override void Enter()
    {
        endTime = Time.time + enemyGolem.recoveryTime; // post attack pause
        if (enemy.agent != null)
        {
            enemy.agent.isStopped = true;
            enemy.agent.velocity = Vector3.zero;
            enemy.agent.ResetPath();
        }

        //check if too close to the player
        if (enemy.target != null)
        {
            float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.target.position);
            needsRetreat = distanceToPlayer < enemyGolem.minAttackDistance;
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
                stateMachine.ChangeState(enemyGolem.GetIdle());
            }
            return;
        }
        if (needsRetreat)
        {

            // Retreat while recovering
            Vector3 awayFromPlayer = enemy.transform.position - enemy.target.position;
            awayFromPlayer.y = 0f;
            if (awayFromPlayer.sqrMagnitude > 0.0001f)
            {
                Vector3 retreatPosition = enemy.target.position + awayFromPlayer.normalized * enemyGolem.shootingRange;
                if (enemy.agent != null && enemy.agent.enabled)
                {
                    enemy.agent.isStopped = false;
                    enemy.agent.SetDestination(retreatPosition);
                }
            }
        }
        if (Time.time >= endTime)
        {
            needsRetreat = false;
            if (PlayerInAggressionRange())
            {
                stateMachine.ChangeState(enemyGolem.GetChase());
            }
            else
            {
                stateMachine.ChangeState(enemyGolem.GetIdle());
            }
        }
    }


}
