using UnityEngine;

public class EnemyMelee : Enemy
{
    [Header("Melee related stuff")]
    public float chaseSpeed = 4f;
    public float attackRange = 1.2f;
    // public int damage = 10; 
    public float knockbackPower = 10f; // how far can push the enemy
    public float recoveryTime { get; private set; } = 0.25f;


    IdleState_Melee idle;
    ChaseState_Melee chase;
    AttackState_Melee attack;
    RecoveryState_Melee recovery;

    public override void Start()
    {
        base.Start(); // run stuff that we wrote in base enemy class first

        agent.speed = chaseSpeed;

        idle = new IdleState_Melee(this, stateMachine);
        chase = new ChaseState_Melee(this, stateMachine);
        attack = new AttackState_Melee(this, stateMachine);
        recovery = new RecoveryState_Melee(this, stateMachine);

        stateMachine.Initialize(idle);
        
    }


    public EnemyState GetIdle()    => idle;
    public EnemyState GetChase()   => chase;
    public EnemyState GetAttack()  => attack;
    public EnemyState GetRecovery() => recovery;
}
