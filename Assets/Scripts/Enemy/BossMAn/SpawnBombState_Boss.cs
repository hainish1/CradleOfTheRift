using UnityEngine;

public class SpawnBombState_Boss : EnemyState
{
    private EnemyBoss_SS boss;
    private float timer;


    public SpawnBombState_Boss(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        this.boss = enemy as EnemyBoss_SS;
    }

    public override void Enter()
    {
        base.Enter();
        if(boss.firePoint != null)
        {
            Vector3 direction = (boss.target ? (boss.target.position + Vector3.up * .5f) - boss.firePoint.position : boss.transform.forward).normalized;

            Vector3 spawnPoint = boss.firePoint.position + direction * 0.1f;
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

            GameObject slimeObj = GameObject.Instantiate(boss.expEnemyPrefab, spawnPoint, rotation);
        }
        timer = boss.bombSpawnInterval;
    }


    public override void Update()
    {
        base.Update();
        timer -= Time.deltaTime;
        if(timer <= 0)
        {
            stateMachine.ChangeState(boss.GetRecoveryState());
        }
    }
}