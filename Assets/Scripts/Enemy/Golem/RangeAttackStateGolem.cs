using UnityEngine;

/// <summary>
/// Class - Represents the Attack State for Melee Enemy.
/// Uses swept-sphere collision during the leap
/// </summary>
public class RangeAttackStateGolem : EnemyState
{
    private EnemyGolem enemyGolem;
    private float timer;
    private bool hasThrown;

    public RangeAttackStateGolem(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyGolem = enemy as EnemyGolem;
    }

    /// <summary>
    /// Enter: pause agent, reset flags, begin attack with windup.
    /// </summary>
    public override void Enter()
    {
        enemyGolem.PauseAgent();
        timer = Random.Range(enemyGolem.minWindupTime, enemyGolem.maxWindupTime);
        hasThrown = false;

        // Trigger throwing animation (not yet implemented)
        //enemy.animator.SetTrigger("ThrowRock");
    }


    public override void Update()
    {
        if (enemy.target == null)
        {
            stateMachine.ChangeState(enemyGolem.GetIdle());
            return;
        }

        // Keep facing the player during windup
        enemyGolem.FaceTargetSmooth(enemyGolem.turnSpeedWhileAiming);
        
        if (!hasThrown)
        {
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                ThrowRock();
                hasThrown = true;

                // Set cooldown and go to recovery
                enemy.nextAttackAllowed = Time.time + enemyGolem.attackCooldown;
                stateMachine.ChangeState(enemyGolem.GetRecovery());
            }
        }
    }

    private void ThrowRock()
    {
        // Use the defined spawn point or default to slightly above the golem
        Vector3 spawnPos = enemyGolem.projectileSpawnPoint != null 
            ? enemyGolem.projectileSpawnPoint.position 
            : enemy.transform.position + Vector3.up * 1.5f;

        // TODO: Throw a straight projectile for now, add arc and prediction later
        
        GameObject rock = Object.Instantiate(enemyGolem.rockProjectilePrefab, spawnPos, Quaternion.identity);
        
        // Aim for player's center
        Vector3 toTarget = (enemy.target.position + Vector3.up * 1.5f) - spawnPos; 
        
        if (rock.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.linearVelocity = toTarget.normalized * enemyGolem.projectileVelocity;
        }

        // Play throw sound effect
        enemyGolem.PlayThrowSFX();
    }

    public override void Exit()
    {
        enemyGolem.ResumeAgent();
    }
}
