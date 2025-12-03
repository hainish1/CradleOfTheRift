using UnityEngine;
using UnityEngine.AI;

public class SpawnBombState_Boss : EnemyState
{
    private EnemyBoss_SS boss;
    private float timer;
    private int bombsThrown;


    public SpawnBombState_Boss(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        this.boss = enemy as EnemyBoss_SS;
    }

    public override void Enter()
    {
        base.Enter();

        bombsThrown = 0;

        ThrowBomb();

        timer = boss.bombSpawnInterval;
    }

    private void ThrowBomb()
    {
        if (boss.firePoint != null)
        {
            Vector3 direction = (boss.target ? (boss.target.position + Vector3.up * .5f) - boss.firePoint.position : boss.transform.forward).normalized;

            Vector3 spawnPoint = boss.firePoint.position + direction * 0.1f;
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

            GameObject slimeObj = GameObject.Instantiate(boss.expEnemyPrefab, spawnPoint, rotation);

            var arcScript = slimeObj.GetComponent<EnemyExploding>();
            if (arcScript != null)
            {
                Vector3 targetPos = boss.firePoint.position + direction * boss.slimeArcDistance - Vector3.up;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(targetPos, out hit, 5.0f, NavMesh.AllAreas))
                {
                    targetPos = hit.position;
                }
                else
                {
                    targetPos.y = boss.firePoint.position.y;
                    Debug.LogWarning("No valid NavMesh below arc end point");
                }
                // arcScript.LaunchAsArc(targetPos, boss.slimeArcDuration, boss.slimeArcHeight, boss.slimeArcSpeed);
                boss.CreatePoofVFX(spawnPoint);
                arcScript.LaunchWithRigidbody(targetPos, boss.slimeArcDuration);
            }
        }
    }

    public override void Update()
    {
        base.Update();
        timer -= Time.deltaTime;
        if(timer <= 0)
        {
            bombsThrown++;

            if(bombsThrown >= boss.bombsPerCycle)
            {
                stateMachine.ChangeState(boss.GetRecoveryState());
                return;
            }

            ThrowBomb();
            timer = boss.bombSpawnInterval;
        }
    }
}