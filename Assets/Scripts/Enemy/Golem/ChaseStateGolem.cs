using UnityEngine;

/// <summary>
/// Class - Represents the Chase State for Melee Enemy
/// </summary>
public class ChaseStateGolem : EnemyState
{
    private EnemyGolem enemyGolem;
    private AgentKnockBack knockBack;

    // Wandering variables
    private bool isWandering;
    private float wanderTimer;
    private float wanderInterval = 2f;  // Time between wander direction changes
    private float wanderRadius = 4f;    // Radius for wandering distance from its current position
    public ChaseStateGolem(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyGolem = enemy as EnemyGolem;
        knockBack = enemy.GetComponent<AgentKnockBack>();
    }

    /// <summary>
    /// What to do when Enemy enters the Chase State
    /// </summary>
    public override void Enter()
    {
        if (enemy?.agent != null)
        {
            enemy.agent.isStopped = false;
        }
        isWandering = false;
        wanderTimer = 0f;
    }

    /// <summary>
    /// Chase towards the player using navmeshagent
    /// stop at ideal range, then switch to rock throw state
    /// </summary>
    public override void Update()
    {
        if (enemy.target == null) return; 

        float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.target.position);

        // Player is too far so chase them directly
        if (distanceToPlayer > enemyGolem.shootingRange)
        {
            isWandering = false;
            if (enemy.agent != null)
            {
                enemy.agent.isStopped = false;
                SetAgentDestination(enemy.target.position); 
            }
        }
        // Player is too close so retreat (for now, we could replace this with a melee attack later)
        else if (distanceToPlayer < enemyGolem.minAttackDistance)
        {
            isWandering = false;
            Vector3 awayFromPlayer = enemy.transform.position - enemy.target.position;
            awayFromPlayer.y = 0f;
            awayFromPlayer.Normalize();

            Vector3 retreatPosition = enemy.target.position + awayFromPlayer * enemyGolem.shootingRange;
            if (enemy.agent != null)
            {
                enemy.agent.isStopped = false;
                SetAgentDestination(retreatPosition);
            }
        }
        // Attack or wander if the player is within the ideal range
        else
        {
            if (knockBack != null && knockBack.IsKnockbackActive) return;

            // If the cooldown is over, stop and attack!
            if (Time.time >= enemy.nextAttackAllowed)
            {
                isWandering = false;
                if (enemy.agent != null)
                {
                    enemy.agent.isStopped = true;
                    enemy.agent.velocity = Vector3.zero;
                }
                
                FaceTarget(enemy.turnSpeed);
                stateMachine.ChangeState(enemyGolem.GetAttack());
            }
            // If the attack is on cooldown, wander around to look busy
            else
            {
                if (!isWandering || wanderTimer <= 0f)
                {
                    PickWanderPoint();
                }
                else
                {
                    wanderTimer -= Time.deltaTime;
                    // Keep looking at the player while shuffling around
                    FaceTarget(enemy.turnSpeed);
                }
            }
        }
    }

    /// <summary>
    /// Picks a random point on the NavMesh near the Golem's current position to simulate pacing/strafing.
    /// </summary>
    private void PickWanderPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        Vector3 randomPos = enemyGolem.transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        if (UnityEngine.AI.NavMesh.SamplePosition(randomPos, out UnityEngine.AI.NavMeshHit hit, wanderRadius, UnityEngine.AI.NavMesh.AllAreas))
        {
            if (enemy.agent != null)
            {
                enemy.agent.isStopped = false;
                enemy.agent.SetDestination(hit.position);
            }
            isWandering = true;
            wanderTimer = wanderInterval; 
        }
        else
        {
            // If we couldn't find a valid point, try again next frame
            wanderTimer = 0f; 
        }
    }
}
