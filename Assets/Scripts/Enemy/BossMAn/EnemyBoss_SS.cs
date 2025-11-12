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



    private IdleState_Boss idle;
    private SpawnBombState_Boss bombState;
    private RecoveryState_Boss recovery;

    public override void Start()
    {
        base.Start();
        idle = new IdleState_Boss(this, stateMachine);
        bombState = new SpawnBombState_Boss(this, stateMachine);
        recovery = new RecoveryState_Boss(this, stateMachine);
        stateMachine.Initialize(idle);
    }

    public override void Die()
    {
        base.Die();
    }

    public EnemyState GetIdle() => idle;
    public EnemyState GetBombState() => bombState;
    public EnemyState GetRecoveryState() => recovery;

}
