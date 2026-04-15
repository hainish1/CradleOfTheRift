using System.Collections;
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

            if (timer <= 0f)    // Hardcoded windup that should get replaced with animation events later
            {
                enemyGolem.ThrowRock();
                hasThrown = true;
                enemyGolem.golemAnim.SetTrigger("AttackRanged");

                // Set cooldown and go to recovery
                enemy.nextAttackAllowed = Time.time + enemyGolem.attackCooldown;
                stateMachine.ChangeState(enemyGolem.GetRecovery());
            }
        }
    }

    // private void ThrowRock()
    // {
    //     // Use the defined spawn point or default to slightly above the golem
    //     Vector3 spawnPos = enemyGolem.projectileSpawnPoint != null 
    //         ? enemyGolem.projectileSpawnPoint.position 
    //         : enemy.transform.position + Vector3.up * 1.5f;

    //     // Target the player's center mass rather than their feet
    //     Vector3 targetPos = enemy.target.position + Vector3.up * 5f; 
        
    //     // Pull from the Object Pool if available, else instantiate normally
    //     GameObject rockObj;
    //     if (ObjectPool.instance != null)
    //     {
    //         Transform spawnTransform = enemyGolem.projectileSpawnPoint != null ? enemyGolem.projectileSpawnPoint : enemyGolem.transform;
    //         rockObj = ObjectPool.instance.GetObject(enemyGolem.rockProjectilePrefab, spawnTransform);
    //         rockObj.transform.position = spawnPos;
    //         rockObj.transform.rotation = Quaternion.identity;
    //     }
    //     else
    //     {
    //         rockObj = Object.Instantiate(enemyGolem.rockProjectilePrefab, spawnPos, Quaternion.identity);
    //     }

    //     // Initialize the projectile
    //     if (rockObj.TryGetComponent<EnemyRockProjectile>(out EnemyRockProjectile rock))
    //     {
    //         float distanceXZ = Vector2.Distance(new Vector2(spawnPos.x, spawnPos.z), new Vector2(targetPos.x, targetPos.z));
    //         float timeToTarget = distanceXZ / enemyGolem.projectileVelocity;
    //         timeToTarget = Mathf.Max(0.1f, timeToTarget);

    //         Vector3 calculatedVelocity = CalculateLaunchVelocity(spawnPos, targetPos, timeToTarget);

    //         // Init: (Vector3 velocity, LayerMask mask, float damage, float knockback)
    //         rock.Init(calculatedVelocity, enemyGolem.projectileMask, enemyGolem.directDamage, enemyGolem.projectileKnockback);
    //     }
    // }

    // /// <summary>
    // /// Calculates the precise 3D velocity required to hit a target point over a specific duration, factoring in Unity's gravity.
    // /// </summary>
    // private Vector3 CalculateLaunchVelocity(Vector3 startPoint, Vector3 targetPoint, float timeToTarget)
    // {
    //     // Calculate displacement
    //     Vector3 displacement = targetPoint - startPoint;
    //     Vector3 displacementXZ = new Vector3(displacement.x, 0, displacement.z);

    //     // Calculate XZ (horizontal) velocity needed to cover the distance in timeToTarget
    //     Vector3 velocityXZ = displacementXZ / timeToTarget;

    //     // Calculate Y (vertical) velocity using the kinematic equation: d = vi*t + 1/2*a*t^2
    //     // Rearranged to solve for vi (initial velocity): vi = (d - 1/2*a*t^2) / t
    //     float velocityY = (displacement.y - (Physics.gravity.y * Mathf.Pow(timeToTarget, 2)) / 2f) / timeToTarget;

    //     // Combine horizontal and vertical velocities
    //     return velocityXZ + (Vector3.up * velocityY);
    // }

    public override void Exit()
    {
        enemyGolem.ResumeAgent();
    }
}
