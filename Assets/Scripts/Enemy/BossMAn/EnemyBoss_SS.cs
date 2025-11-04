using UnityEngine;

public class EnemyBoss_SS : Enemy
{
    [Header("Boss Attack Settings")]
    public GameObject expEnemyPrefab;  // explodies
    public Transform[] spawnPoints;
    public float slimemSpawnInterval = 4f;


    private IdleState_Boss idle;
    private SpawnBombState_Boss explosions;
    private RecoveryState_Boss recovery;
    
}
