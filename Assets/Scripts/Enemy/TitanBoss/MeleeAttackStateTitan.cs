using UnityEngine;

/// <summary>
/// Class - Placeholder for the Golem enemy melee logic.
/// </summary>
public class MeleeAttackStateTitan : EnemyState
{
    private EnemyTitan enemyTitan;
    private float stateTimer;

    public MeleeAttackStateTitan(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyTitan = enemy as EnemyTitan;
    }

    public override void Enter()
    {
        enemyTitan.PauseAgent();
        //Debug.Log("Titan boss entered Melee Attack State");
        stateTimer = 0f;
        enemyTitan.golemAnim.SetTrigger("AttackMelee");
    }

    public override void Update()
    {
        stateTimer += Time.deltaTime;

        float totalAnimationTime = enemyTitan.meleeAnim.length; // How long the whole state lasts
        float hitFrameTime = enemyTitan.meleeAnim.events[0].time; // The exact moment the punch lands

        // Keep turning to face the player until melee attack
        if (stateTimer < hitFrameTime)
        {
            enemyTitan.FaceTargetSmooth(enemyTitan.turnSpeedWhileAiming);
        }

        // Leave the state when the full animation time is over
        if (stateTimer >= totalAnimationTime)
        {
            // Set melee cooldown
            enemyTitan.nextAttackAllowed = Time.time + enemyTitan.attackCooldown;
            
            // Go to recovery to pause and wander
            stateMachine.ChangeState(enemyTitan.GetRecovery());
        }
    }

    public override void Exit()
    {
        enemyTitan.ResumeAgent();
    }
}

// using UnityEngine;

// /// <summary>
// /// Class - Represents the Attack State for Melee Enemy.
// /// Uses swept-sphere collision during the leap
// /// </summary>
// public class MeleeAttackStateGolem : EnemyState
// {
//     private EnemyGolem enemyGolem;
//     private AgentKnockBack knockBack;

//     private enum Phase { Windup, Leap }
//     private Phase phase;
//     private float timer;
//     private Vector3 velocity;
//     private bool hasLanded;

//     private float currentFlightTime;
//     private float estimatedFlightDuration;
//     private bool hasReachedPeak;

//     // Tracks phase transitions 
//     private Phase lastPhase;

//     public MeleeAttackStateGolem(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
//     {
//         enemyGolem = enemy as EnemyGolem;
//         knockBack = enemy.GetComponent<AgentKnockBack>();
//     }

//     /// <summary>
//     /// Enter: pause agent, reset flags, begin windup freeze.
//     /// </summary>
//     public override void Enter()
//     {
//         enemyGolem.PauseAgent();

//         enemyGolem.hitAppliedThisAttack = false;
//         enemyGolem.EnableHitBox(false);
//         enemyGolem.isInAir = false;

//         phase = Phase.Windup;
//         lastPhase = phase;
//         timer = enemyGolem.windupTime;
//         hasLanded = false;
//         hasReachedPeak = false;

//         // Squash animation as the slime winds up
//         enemyGolem.height.GetComponent<Animator>().SetTrigger("squash");
//     }


//     public override void Update()
//     {
//         switch (phase)
//         {
//             case Phase.Windup:
//                 HandleWindup();
//                 break;
//             case Phase.Leap:
//                 HandleLeap();
//                 break;
//         }

//         if (lastPhase == Phase.Windup && phase == Phase.Leap)
//         {
//             enemyGolem.PlayJumpSFX();
//             enemyGolem.PlayMeleePSVFX(enemyGolem.jumpPoofVFXPrefab, enemyGolem.jumpVFXAttackPoint);
//         }

//         lastPhase = phase;
//     }

//     private void HandleWindup()
//     {
//         // Wait for any active ground-knockback to finish before leaping
//         if (knockBack != null && knockBack.IsKnockbackActive)
//         {
//             timer = enemyGolem.windupTime;
//             return;
//         }

//         // stick to ground every frame during windup 
//         SnapToGround();

//         FaceTarget(enemy.turnSpeed * 2f);
//         timer -= Time.deltaTime;

//         if (timer <= 0f)
//         {
//             // If knockback pushed us too far, abort the leap and chase instead
//             if (enemy.target != null)
//             {
//                 float distToTarget = Vector3.Distance(
//                     enemy.transform.position, enemy.target.position);

//                 if (distToTarget > enemyGolem.leapAttackRange * 1.5f)
//                 {
//                     enemyGolem.ResumeAgent();
//                     stateMachine.ChangeState(enemyGolem.GetChase());
//                     return;
//                 }
//             }

//             StartLeap();
//         }
//     }

//     /// <summary>
//     /// Raycast from high above straight down to find the physics ground surface
//     /// </summary>
//     private void SnapToGround()
//     {
//         Vector3 origin = enemy.transform.position + Vector3.up * 5f;
//         if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10f,
//                 enemyGolem.groundMask, QueryTriggerInteraction.Ignore))
//         {
//             float halfHeight = enemy.agent != null ? enemy.agent.height * 0.7f : 0f;
//             Vector3 pos = enemy.transform.position;
//             pos.y = hit.point.y + (halfHeight + enemyGolem.startHeightAboveGround);
//             enemy.transform.position = pos;
//         }
//     }

//     private void StartLeap()
//     {
//         phase = Phase.Leap;
//         enemyGolem.EnableHitBox(true);
//         enemyGolem.isInAir = true;

//         // Stretch animation as the slime goes up
//         enemyGolem.height.GetComponent<Animator>().SetTrigger("stretch");

//         Vector3 startPos = enemy.transform.position;
//         Vector3 targetPos = startPos + enemy.transform.forward * 2f;

//         if (enemy.target != null)
//         {
//             Vector3 rawTargetPos = enemy.target.position;
//             Vector3 jumpDir = rawTargetPos - startPos;
//             jumpDir.y = 0;

//             // Lock the leap distance to where the player is NOW.
//             float distToPlayer = jumpDir.magnitude;
//             jumpDir = jumpDir.normalized;

//             float overshoot = Mathf.Min(enemyGolem.leapOverShootDistance, distToPlayer * 0.5f);
//             targetPos = startPos + jumpDir * (distToPlayer + overshoot);
//         }

//         velocity = enemyGolem.CalculateBallisticVelocity(
//             startPos, targetPos, enemyGolem.leapHeight, out estimatedFlightDuration);

//         // Face the jump direction
//         Vector3 hVel = new Vector3(velocity.x, 0, velocity.z);
//         if (hVel.sqrMagnitude > 0.001f)
//             enemy.transform.rotation = Quaternion.LookRotation(hVel);

//         enemyGolem.inAirVelocity = velocity;
//         currentFlightTime = 0f;
//         hasReachedPeak = false;
//     }

//     /// <summary>
//     /// Core leap- gravity -> swept-sphere move -> landing detection
//     /// Mid air knockback impulses are picked up from inAirVelocity
//     /// </summary>
//     private void HandleLeap()
//     {
//         if (hasLanded) return;

//         float dt = Time.deltaTime;
//         currentFlightTime += dt;

//         // Pick up any mid air knockback impulse that was added externally
//         velocity = enemyGolem.inAirVelocity;

//         // Gravity
//         velocity.y += Physics.gravity.y * enemyGolem.gravityScale * dt;

//         // track when the slime has gone up and start descending
//         if (!hasReachedPeak && velocity.y <= 0f)
//             hasReachedPeak = true;

//         // only check for landing after the slime has actually risen
//         if (hasReachedPeak)
//         {
//             if (enemyGolem.GroundCheck(out Vector3 gp))
//             {
//                 Land(gp);
//                 return;
//             }
//         }

//         velocity = enemyGolem.SweepMove(velocity, dt);
//         enemyGolem.inAirVelocity = velocity;

//         // Keep agent in sync 
//         if (enemy.agent != null && enemy.agent.isOnNavMesh)
//             enemy.agent.nextPosition = enemy.transform.position;

//         //prevent infinite flight
//         if (currentFlightTime > estimatedFlightDuration * 2.5f)
//         {
//             Land(enemy.transform.position);
//         }
//     }

//     /// <summary>
//     /// Snap to ground, clear in air state, resume agent, go to recovery
//     /// </summary>
//     private void Land(Vector3 groundPoint)
//     {
//         if (hasLanded) return;
//         hasLanded = true;

//         float halfHeight = enemy.agent != null ? enemy.agent.height * 0.7f : 0f;
//         enemy.transform.position = groundPoint + Vector3.up * (halfHeight + enemyGolem.startHeightAboveGround);
//         enemyGolem.isInAir = false;
//         enemyGolem.inAirVelocity = Vector3.zero;
//         enemyGolem.EnableHitBox(false);

//         // Squash animation on landing impact
//         enemyGolem.height.GetComponent<Animator>().SetTrigger("squash");

//         // Always enforce cooldown on landing so the slime
//         // can never immediately leap again after missing once
//         enemy.nextAttackAllowed = Time.time + enemy.attackCooldown;

//         // Snap back onto NavMesh  
//         enemyGolem.ResumeAgent();

//         stateMachine.ChangeState(enemyGolem.GetRecovery());
//     }

//     public override void Exit()
//     {
//         enemyGolem.EnableHitBox(false);
//         enemyGolem.isInAir = false;
//         enemyGolem.inAirVelocity = Vector3.zero;

//         // Safety- resume agent if Land() was never called
//         if (enemy.agent != null && !enemy.agent.updatePosition)
//         {
//             enemyGolem.ResumeAgent();
//         }
//     }
// }
