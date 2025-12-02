using UnityEngine;
using UnityEngine.AI;

public class LeapAttackState_Boss : EnemyState
{
    private EnemyBoss_SS boss;
    private float leapTimer;
    private float timer;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Quaternion lockedChargeRot;

    private enum Phase { Windup, Leap }
    private Phase phase;

    public LeapAttackState_Boss(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        boss = enemy as EnemyBoss_SS;
    }


    public override void Enter()
    {
        base.Enter();

        if(boss.firePoint != null)
            {
                Vector3 direction = (boss.target ? (boss.target.position + Vector3.up * .5f) - boss.firePoint.position : boss.transform.forward).normalized;
                Vector3 spawnPoint = boss.firePoint.position + direction * 0.1f;
                boss.CreateVFX(boss.flashVFX, spawnPoint, 5);
            }
        if (enemy.agent != null)
        {
            enemy.agent.isStopped = true;
            enemy.agent.velocity = Vector3.zero;
            enemy.agent.updateRotation = false;
        }
        boss.hitAppliedThisAttack = false;
        boss.EnableHitBox(false);

        phase = Phase.Windup;
        timer = boss.windupTime;
        leapTimer = 0f;

    }


    public override void Update()
    {
        base.Update();
        timer -= Time.deltaTime;
        switch (phase)
        {
            case Phase.Windup:
                HandleWindup();
                break;
            case Phase.Leap:
                HandleLeap();
                break;
        }
    }

    private void HandleWindup()
    {


        if (timer <= 0f)
        {
            if (enemy.target != null)
            {
                startPosition = enemy.transform.position;

                Vector3 flatToPlayer = enemy.target.position;
                flatToPlayer.y = startPosition.y;
                // targetPosition = enemy.target.position;
                // targetPosition.y += 0.2f;
                targetPosition = flatToPlayer;
                Vector3 dir = targetPosition - startPosition;
                dir.y = 0f;

                if (dir.sqrMagnitude > 0.001f)
                {
                    dir.Normalize();
                    lockedChargeRot = Quaternion.LookRotation(dir, Vector3.up);
                    boss.transform.rotation = lockedChargeRot;
                }

                NavMeshHit hit;
                if (NavMesh.SamplePosition(targetPosition, out hit, 2f, NavMesh.AllAreas))
                    targetPosition = new Vector3(hit.position.x, startPosition.y, hit.position.z);
            }
            phase = Phase.Leap;
            timer = boss.leapDuration;
            leapTimer = 0f;

            if (enemy.agent != null)
            {
                enemy.agent.isStopped = true;
                enemy.agent.updateRotation = false;
                enemy.agent.updatePosition = false;
                enemy.agent.velocity = Vector3.zero;
            }
        }
    }

    private void HandleLeap()
    {
        boss.EnableHitBox(true);
        leapTimer += Time.deltaTime;
        float t = Mathf.Clamp01(leapTimer / boss.leapDuration);
        Vector3 pos = Vector3.Lerp(startPosition, targetPosition, t);
        pos.y += boss.leapHeight * Mathf.Sin(t * Mathf.PI);

        if (enemy.agent != null)
        {
            enemy.agent.updatePosition = false;
            enemy.transform.position = pos;
        }
        else
        {
            enemy.transform.position = pos;
        }
        enemy.transform.rotation = lockedChargeRot;

        if (timer <= 0f || t >= 1f)
        {
            boss.EnableHitBox(false);

            if (boss.agent != null)
            {
                enemy.agent.updatePosition = true;
                enemy.agent.updateRotation = true;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(enemy.transform.position, out hit, 2f, NavMesh.AllAreas))
                {
                    enemy.transform.position = new Vector3(hit.position.x, hit.position.y, hit.position.z);
                    enemy.agent.Warp(enemy.transform.position);
                }
                enemy.agent.isStopped = false;
            }
            boss.nextAttackAllowed = Time.time + boss.attackCooldown;
            stateMachine.ChangeState(boss.GetRecoveryState());
        }
    }

    public override void Exit()
    {
        base.Exit();
        boss.EnableHitBox(false);
        if (enemy.agent != null)
        {
            enemy.agent.nextPosition = enemy.transform.position;
            enemy.agent.updateRotation = true; // give control back to agent
            enemy.agent.Warp(enemy.transform.position);
            enemy.agent.updatePosition = true;
            enemy.agent.isStopped = false;
        }
    }
}