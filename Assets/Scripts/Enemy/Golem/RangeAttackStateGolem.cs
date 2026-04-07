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
    // private Vector3 CalculateLaunchVelocity(Vector3 target, float timeToTarget)
    // {
    //     // using a formula
    //     Vector3 direction = target - this.transform.position;
    //     Vector3 directionXZ = new Vector3(direction.x, 0, direction.z);

    //     Vector3 velocityXZ = directionXZ / timeToTarget; // calculate velocity based direction of throw and time to reach target

    //     // A DESC FOR THIS FORMULA
    //     // (Physics.gravity.y * Mathf.Pow(timeToTarget, 2)) / 2 :- this part calculates
    //     // d = 1/2 * g * t^2, d = displacement, g = gravity, t = time taken
    //     // (direction.y - (Physics.gravity.y * Mathf.Pow(timeToTarget, 2)) / 2) :- calculates the initial vertical displacement 
    //     // neeeded, taking into account the displacement due to gravity
    //     // finally dividing this displacement by time gives us the velocity we need = v = s/t

    //     float velocityY = (direction.y - (Physics.gravity.y * Mathf.Pow(timeToTarget, 2)) / 2) / timeToTarget;

    //     Vector3 launchVelocity = velocityXZ + (Vector3.up * velocityY);

    //     return launchVelocity;

    // }
    // public void SetupGrenade(LayerMask allyLayerMask, Vector3 target, float timeToTarget, float countdown, float impactPower, int grenadeDamage)
    // {
    //     canExplode = true;
    //     this.allyLayerMask = allyLayerMask;
    //     rb.linearVelocity = CalculateLaunchVelocity(target, timeToTarget);
    //     this.explosionTimer = countdown + timeToTarget; // so it starts actual countdown after reaching the target time
    //     this.impactPower = impactPower;
    //     this.grenadeDamage = grenadeDamage;
    // }

    public override void Exit()
    {
        enemyGolem.ResumeAgent();
    }
}
