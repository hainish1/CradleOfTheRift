using UnityEngine;

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
    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }

    // Helperign method
    protected bool PlayerInAggressionRange() => enemy.target != null && (enemy.target.position - enemy.transform.position).sqrMagnitude <= enemy.aggressionRange * enemy.aggressionRange;

    protected bool PlayerInAttackRange(float range) => enemy.target != null && (enemy.target.position - enemy.transform.position).sqrMagnitude <= range * range;

    protected void FaceTarget(float turnSpeed)
    {
        if (enemy.target == null) return;

        Vector3 direction = enemy.target.position - enemy.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;
        var targetRotation = Quaternion.LookRotation(direction);
        enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    protected void SetAgentDestination(Vector3 dest)
    {
        if (enemy.agent != null && enemy.agent.enabled == true)
        {
            enemy.agent.SetDestination(dest);
        }
    }

}
