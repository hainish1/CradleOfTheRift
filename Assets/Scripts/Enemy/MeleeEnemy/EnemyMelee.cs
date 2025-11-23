using UnityEngine;

/// <summary>
/// Class - Represents a Melee Enemy, inherits from Base Enemy class 
/// and defines functionality of its own
/// </summary>
public class EnemyMelee : Enemy
{
    [Header("Melee related stuff")]
    public float chaseSpeed = 4f;
    public float attackRange = 1.2f;
    // public int damage = 10; 
    public float knockbackPower = 10f; // how far can push the enemy
    public float recoveryTime { get; private set; } = 0.25f;

    [Header("Slime drag stuff")]
    public float dragSpeed = 6f;
    public float dragDuration = 0.35f;
    public float restDuration = 0.25f;


    [Header("Slam attack")]
    public float slamDamage = 1;
    public float windupTime = .15f;
    public float chargeSpeed = 12f;
    public float chargeTime = .18f;

    [Header("AttackHitbox")]
    [SerializeField] private EnemyMeleeHitbox hitbox;
    [HideInInspector] public bool hitAppliedThisAttack;

    [Header("Leap Attack Settings")]
    public float leapAttackRange = 5f; // distance to start leap
    public float minAttackDistance = 3f; // min safe dist
    public float leapHeight = 1f; // vertical arc
    public float leapDuration = .5f; // time for leap




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

    /// <summary>
    /// Enable the hitbox used to detect gameobjects that this enemy hit
    /// </summary>
    /// <param name="enable"></param>
    public void EnableHitBox(bool enable)
    {
        if (hitbox != null && hitbox.gameObject.activeSelf != enable)
        {
            hitbox.gameObject.SetActive(enable);
        }
    }

    /// <summary>
    /// Try to apply damage and impulse to the player GameObject caught in colliders 
    /// </summary>
    /// <param name="playerCol"></param>
    public void TryApplyHit(Collider playerCol)
    {
        if (hitAppliedThisAttack) return;
        if (Time.time < nextAttackAllowed) return;

        Vector3 toPlayer = playerCol.transform.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f) return;

        toPlayer.Normalize();

        var pm = playerCol.GetComponentInParent<PlayerMovement>();
        if (pm != null)
        {
            pm.ApplyImpulse(toPlayer * knockbackPower);

            var damageable = pm.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                damageable.TakeDamage(slamDamage);
            }
        }
        hitAppliedThisAttack = true;
        nextAttackAllowed = Time.time + attackCooldown;
        EnableHitBox(false);


    }

    /// <summary>
    /// Initialize damage done by this enemy, can be updated by enemy spawner
    /// </summary>
    /// <param name="newDamage"></param>
    public void InitializeSlamDamage(float newDamage)
    {
        // this.slamDamage = Mathf.CeilToInt(newDamage);
        this.slamDamage = newDamage;
        Debug.Log("Slam Damage: " + this.slamDamage);
    }

    //Getters for States that this Melee Enemy has
    public EnemyState GetIdle() => idle;
    public EnemyState GetChase() => chase;
    public EnemyState GetAttack() => attack;
    public EnemyState GetRecovery() => recovery;

    /// <summary>
    /// Get Base Damage for this enemy
    /// </summary>
    /// <returns></returns>
    public float GetBaseDamage() => slamDamage;

}
