using UnityEngine;

/// <summary>
/// Class : Used to define what states enemy has
/// </summary>
public abstract class EnemyState
{
    protected readonly Enemy enemy;
    protected readonly EnemyStateMachine stateMachine;

    protected EnemyState(Enemy enemy, EnemyStateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;

    }

    // Three states for now

    /// <summary>
    /// What Enemy should do when they Enter a state
    /// </summary>
    public virtual void Enter() { }

    /// <summary>
    /// What Enemy should do when they Exit a state
    /// </summary>
    public virtual void Exit() { }

    /// <summary>
    /// What Enemy should do when they are already in a state
    /// </summary>
    public virtual void Update() { }

    // Helperign method

    /// <summary>
    /// Check if player is in aggression range
    /// </summary>
    /// <returns></returns>
    protected bool PlayerInAggressionRange() => enemy.target != null && (enemy.target.position - enemy.transform.position).sqrMagnitude <= enemy.aggressionRange * enemy.aggressionRange;

    /// <summary>
    /// Check if player is in Attacking Range
    /// </summary>
    /// <param name="range"></param>
    /// <returns></returns>
    protected bool PlayerInAttackRange(float range) => enemy.target != null && (enemy.target.position - enemy.transform.position).sqrMagnitude <= range * range;

    /// <summary>
    /// Change GameObject direction to face its target
    /// </summary>
    /// <param name="turnSpeed"></param>
    protected void FaceTarget(float turnSpeed)
    {
        if (enemy.target == null) return;

        Vector3 direction = enemy.target.position - enemy.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;
        var targetRotation = Quaternion.LookRotation(direction);
        enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Set navmeshagent destination, move GameObject towards the destination
    /// </summary>
    /// <param name="dest"></param>
    protected void SetAgentDestination(Vector3 dest)
    {
        if (enemy.agent != null && enemy.agent.enabled == true)
        {
            enemy.agent.SetDestination(dest);
        }
    }

}
