using UnityEngine;


/// <summary>
/// Class - Represents the Chase State for Revenant enemy boss
/// </summary>
public class ChaseStateRevenant : EnemyState
{
    RevenantBossRange bossRange;
    public ChaseStateRevenant(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        bossRange = enemy as RevenantBossRange;
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
                enemy.agent.speed = bossRange.chaseSpeed; // set navmesh speed
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
        if (distance > bossRange.stopDistance * .8f)
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

        if (distance <= bossRange.attackRange && Time.time >= enemy.nextAttackAllowed)
        {
            // Can randomize between different attacks here
            float random = Random.value;
            if (random < 0.6f) // barrage attack has a higher chance to be chosen (idk if this is a good idea)
            {
                stateMachine.ChangeState(bossRange.GetAttack());
                //Debug.Log("Revenant: Switching to Attack State from Chase State");
            }
            else
            {
                stateMachine.ChangeState(bossRange.GetAOEAttack());
                //Debug.Log("Revenant: Switching to AOE Attack State from Chase State");
            }
            //stateMachine.ChangeState(bossRange.GetAttack());
            //Debug.Log("Revenant: Switching to Attack State from Chase State");
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
