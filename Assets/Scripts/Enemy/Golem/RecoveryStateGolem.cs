using UnityEngine;

/// <summary>
/// Class - Represents the Recovery State for Melee Enemy
/// </summary>
public class RecoveryStateGolem : EnemyState
{
    private EnemyGolem enemyGolem;
    private float endTime;
    private float startWanderTime;
    private float wanderTimer;
    private bool isWandering;

    public RecoveryStateGolem(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyGolem = enemy as EnemyGolem;
    }


    /// <summary>
    /// What to do when enemy enters the Recovery state
    /// </summary>
    public override void Enter()
    {
        endTime = Time.time + Random.Range(enemyGolem.minWanderTime, enemyGolem.maxWanderTime); // post rock attack wandering
        startWanderTime = Time.time + enemyGolem.postAttackCooldown; // time before it starts wandering around
        
        wanderTimer = 0f;
        isWandering = false;
        if (enemy.agent != null)
        {
            enemy.agent.isStopped = true;
            enemy.agent.velocity = Vector3.zero;
            enemy.agent.ResetPath();
        }
    }

    /// <summary>
    /// Wait for recovery time to finish, then if player is in aggro range, chase it, else switch to idle
    /// </summary>
    public override void Update()
    {
        if (enemy.target == null)
        {
            stateMachine.ChangeState(enemyGolem.GetIdle());
            return;
        }

        float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.target.position);
        // Interrupt wandering if player gets too close -> trigger melee attack immediately
        if (distanceToPlayer <= enemyGolem.minAttackDistance)
        {
            stateMachine.ChangeState(enemyGolem.GetMeleeAttack());
            return;
        }

        // Interrupt wandering if player ran out of range -> chase them until in range
        if (distanceToPlayer > enemyGolem.shootingRange)
        {
            stateMachine.ChangeState(enemyGolem.GetChase());
            return;
        }

        // Wander while waiting for the pause timer to end
        if (Time.time < endTime)
        {
            // First pause after attack
            if (Time.time >= startWanderTime) 
            {
                if (!isWandering || wanderTimer <= 0f)
                {
                    PickWanderPoint();
                }
                else
                {
                    wanderTimer -= Time.deltaTime;
                }

                // Keep the body aligned with travel so one forward walk animation works for wandering.
                enemyGolem.FaceMovementDirectionSmooth(enemyGolem.turnSpeedWhileAiming);
            }
        }
        else
        {
            // Pause time is over, go back to chase (which will immediately trigger a new attack)
            stateMachine.ChangeState(enemyGolem.GetChase());
        }
    }
    private void PickWanderPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * enemyGolem.wanderRadius;
        Vector3 randomPos = enemyGolem.transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        if (UnityEngine.AI.NavMesh.SamplePosition(randomPos, out UnityEngine.AI.NavMeshHit hit, enemyGolem.wanderRadius, UnityEngine.AI.NavMesh.AllAreas))
        {
            if (enemy.agent != null && enemy.agent.isOnNavMesh)
            {
                enemy.agent.isStopped = false;
                enemy.agent.SetDestination(hit.position);
            }
            isWandering = true;
            wanderTimer = enemyGolem.wanderInterval; 
        }
        else
        {
            // If invalid spot, wait half a second before checking again
            wanderTimer = 0.5f; 
        }
    }

}
