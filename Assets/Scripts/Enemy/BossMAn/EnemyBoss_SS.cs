using UnityEngine;

public class EnemyBoss_SS : Enemy
{
    [Header("Boss Attack Settings")]
    public GameObject expEnemyPrefab;  // explodies
    public Transform[] spawnPoints;
    public float bombSpawnInterval = 4f;
    public float idleTime = 1f;
    public Transform firePoint;
    public float slimeArcDistance;
    public float slimeArcDuration;

    [Header("Ring Attack Settings")]
    public float maxRadius;
    public float duration;
    public float explosionDamage;
    public float explosionCameraShakeForce = 10f;
    public GameObject poofVFX;

    public GameObject explosionVFXPrefab;

    private IdleState_Boss idle;
    private SpawnBombState_Boss bombState;
    private RecoveryState_Boss recovery;
    private RingAttackState_Boss ringAttack;

    public override void Start()
    {
        base.Start();
        idle = new IdleState_Boss(this, stateMachine);
        bombState = new SpawnBombState_Boss(this, stateMachine);
        recovery = new RecoveryState_Boss(this, stateMachine);
        ringAttack = new RingAttackState_Boss(this, stateMachine, maxRadius, duration, explosionDamage, playerMask);

        stateMachine.Initialize(idle);
    }

    public override void Die()
    {
        base.Die();
    }

    public EnemyState GetIdle() => idle;
    public EnemyState GetBombState() => bombState;
    public EnemyState GetRecoveryState() => recovery;
    public EnemyState GetExploisionState() => ringAttack;

    public void CreatePoofVFX(Vector3 spawnPosition)
    {
        if (poofVFX == null) return;
        GameObject newFx = Instantiate(poofVFX);
        newFx.transform.position = spawnPosition;
        newFx.transform.rotation = Quaternion.identity;

        Destroy(newFx, 1); // destroy after one second
    }

    void OnDrawGizmos()
    {
        Gizmos.color = aggressionColor;
        Gizmos.DrawWireSphere(transform.position, maxRadius);
    }

}
