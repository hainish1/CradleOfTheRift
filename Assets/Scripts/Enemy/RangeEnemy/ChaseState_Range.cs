using UnityEngine;


/// <summary>
/// Class - Represents the Chase State for Range Enemy
/// </summary>
public class ChaseState_Range : EnemyState
{
    EnemyRange enemyRange;
    public ChaseState_Range(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyRange = enemy as EnemyRange;
    }

    /// <summary>
    /// When entering the chase state, give control to navmeshagent
    /// </summary>
    public override void Enter()
    {
        if (enemy != null)
        {
            if (enemy.agent != null)
            {
                enemy.agent.isStopped = false;
                enemy.agent.speed = enemyRange.chaseSpeed; // set navmesh speed
            }
        }
    }

    /// <summary>
    /// While inside chase state, face the player and chase towards it. If player is in attack range, then switch to Attack state
    /// </summary>
    public override void Update()
    {
        if (enemy.target == null) return;

        float distance = Vector3.Distance(enemy.transform.position, enemy.target.position); // go but keep distance
        if (distance > enemyRange.stopDistance * .8f)
        {
            if (enemy != null) SetAgentDestination(enemy.target.position);
        }
        else
        {
            if (enemy != null)
            {
                if (enemy.agent) enemy.agent.isStopped = true; // too close, stop there
            }
        }

        FaceTarget(enemy.turnSpeed);

        if (distance <= enemyRange.attackRange && Time.time >= enemy.nextAttackAllowed)
        {
            stateMachine.ChangeState(enemyRange.GetAttack());
        }

    }


    /// <summary>
    /// What to do when exiting the ChaseState
    /// </summary>
    public override void Exit()
    {
        if (enemy.agent) enemy.agent.isStopped = false; // free him again
    }
}
