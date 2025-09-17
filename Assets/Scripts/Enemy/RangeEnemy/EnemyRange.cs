using UnityEngine;

public class EnemyRange : Enemy
{
    [Header("Hover and movement")]
    public float hoverHeight = 2f; // height above ground
    public float hoverBobAmplitude = 0.25f; // up n down
    public float hoverBobSpeed = 2f;
    public float chaseSpeed = 3.5f;
    public float stopDistance = 7f; // how far away from player should it stop 
    public float attackRange = 12f;
    public float turnSpeedWhileAiming = 12f;

    [Header("Shooting")]
    public Transform firePoint; // where bullet come from
    public EnemyProjectile projectilePrefab;
    public float projectileSpeed = 50f;
    public float fireCooldown = .6f;
    public LayerMask projectileMask = ~0;
    public float spawnOffset = 0.1f; // a little away fro fire point, safety

    [Header("Reccovery")]
    public float recoveryTime = 0.4f;

    IdleState_Range idle;
    ChaseState_Range chase;
    AttackState_Range attack;
    RecoveryState_Range recovery;

    float bobPhase;


    public override void Start()
    {
        base.Start();

        if (agent != null)
        {
            agent.speed = chaseSpeed;
            agent.angularSpeed = 720f;
            agent.acceleration = 100f;
            agent.autoBraking = true;
            agent.stoppingDistance = stopDistance * 0.8f;
        }

        idle = new IdleState_Range(this, stateMachine);
        chase = new ChaseState_Range(this, stateMachine);
        attack = new AttackState_Range(this, stateMachine);
        recovery = new RecoveryState_Range(this, stateMachine);

        stateMachine.Initialize(idle); // enter idle first

    }

    public override void Update()
    {
        base.Update();
        UpdateHover();
    }

    void UpdateHover()
    {
        if (agent == null) return;

        bobPhase += Time.deltaTime * hoverBobSpeed;
        agent.baseOffset = hoverHeight + Mathf.Sin(bobPhase) * hoverBobAmplitude; // usign sin formula for bobbing
    }

    // HELPERS

    public EnemyState GetIdle() => idle;
    public EnemyState GetChase() => chase;
    public EnemyState GetAttack() => attack;
    public EnemyState GetRecovery() => recovery;

    public void FireOnce()
    {
        if (!firePoint || !projectilePrefab) return;

        Vector3 direction = (target ? (target.position + Vector3.up * .5f) - firePoint.position : transform.forward).normalized;

        Vector3 spawnPoint = firePoint.position + direction * spawnOffset;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        EnemyProjectile projectile = Instantiate(projectilePrefab, spawnPoint, rotation);
        projectile.Init(direction * projectileSpeed, projectileMask);
    }

}
