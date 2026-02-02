using UnityEngine;

/// <summary>
/// Class - Represents the Attack State for Melee Enemy
/// </summary>
public class AttackState_Melee : EnemyState
{
    private EnemyMelee enemyMelee;

    private enum Phase { Windup, Leap, Charge }
    private Phase phase;
    private float timer;
    // private Vector3 chargeDirection;
    // private Quaternion lockedChargeRot;

    // private Vector3 leapStartPosition;
    // private Vector3 leapTargetPosition;
    private float leapTimer;
    private Vector3 velocity;
    private bool hasLanded;

    // safety variables
    private float currentFlightTime;
    private float estimatedFlightDuration;

    // float endTime;

    // Sorry Hainish!!! No Problem MAAAN
    // This is used to detect when the phase changes from
    // Windup to leap.
    // This helps to know when to play the slime jump sfx.
    private Phase lastPhase;

    public AttackState_Melee(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyMelee = enemy as EnemyMelee;

    }

    /// <summary>
    /// What to do when Enemy enters the Attack State. Take control from navmeshagent and control physics manually
    /// </summary>
    public override void Enter()
    {
        // // quick time window to do a hit
        // endTime = Time.time + 0.15f;
        if (enemy.agent != null)
        {
            enemy.agent.isStopped = true;
            enemy.agent.velocity = Vector3.zero;
            enemy.agent.ResetPath();
            enemy.agent.updateRotation = false;
        }

        enemyMelee.hitAppliedThisAttack = false;
        enemyMelee.EnableHitBox(false);

        // small windup, basically freeze in place
        phase = Phase.Windup;
        lastPhase = phase;
        timer = enemyMelee.windupTime;
        leapTimer = 0f;
        hasLanded = false;
        // TryApplyHit();
    }


    /// <summary>
    /// Face the Target, Enable the Attack Hitbox, and Charge towards the player, once done change to Recovery State
    /// </summary>
    public override void Update()
    {

        // timer -= Time.deltaTime;

        switch (phase)
        {
            case Phase.Windup:
                HandleWindup();
                break;
            case Phase.Leap:
                HandleLeap();
                break;
        }
        // If we just phase changed from windup to leap, play slime jump sfx.
        if (lastPhase == Phase.Windup && phase == Phase.Leap)
        {
            // Play the slime jump sound effect.
            enemyMelee.PlayJumpSFX();
        }

        lastPhase = phase;
    }

    private void HandleWindup()
    {
        FaceTarget(enemy.turnSpeed * 2f);
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            StartLeap();
        }
    }

    private void StartLeap()
    {
        phase = Phase.Leap;
        enemyMelee.EnableHitBox(true);
        if (enemy.agent != null) enemy.agent.updatePosition = false;


        // NEW overshoot logic
        Vector3 startPos = enemy.transform.position;
        Vector3 targetPos = startPos + enemy.transform.forward * 2f; // fallback

        if (enemy.target != null)
        {
            Vector3 rawTargetPos = enemy.target.position;

            // calc distance from enemy to player
            Vector3 jumpDir = (rawTargetPos - startPos).normalized;
            jumpDir.y = 0; // keep horizontal

            targetPos = rawTargetPos + jumpDir * enemyMelee.leapOverShootDistance;

        }

        // calculate Physics Trajectory and get duration
        velocity = enemyMelee.CalculateBallisticVelocity(
            startPos,
            targetPos,
            enemyMelee.leapHeight,
            out estimatedFlightDuration
        );

        // look now
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
        if (horizontalVelocity.sqrMagnitude > 0.001f)
        {
            enemy.transform.rotation = Quaternion.LookRotation(horizontalVelocity);
        }


        currentFlightTime = 0f;
    }

    private void HandleLeap()
    {
        float dt = Time.deltaTime;
        currentFlightTime += dt;

        // apply gravity
        velocity.y += Physics.gravity.y * enemyMelee.gravityScale * dt;

        // move
        enemy.transform.position += velocity * dt;


        // check landing
        if (currentFlightTime > estimatedFlightDuration * 0.5f)
        {
            if (velocity.y < 0)
            {
                CheckGroundLanding();
            }
        }

    }
    private void CheckGroundLanding()
    {
        if (Physics.Raycast(enemy.transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 0.8f, enemyMelee.groundMask))
        {
            Land(hit.point);
        }
    }

    private void Land(Vector3 groundPoint)
    {
        enemy.transform.position = groundPoint;
        if (hasLanded) return;
        hasLanded = true;

        enemyMelee.EnableHitBox(false);

        if (enemy.agent != null)
        {
            enemy.agent.ResetPath();
            enemy.agent.Warp(groundPoint);
            enemy.agent.nextPosition = enemy.transform.position;
            enemy.agent.velocity = Vector3.zero;

            enemy.agent.updatePosition = true;
            enemy.agent.updateRotation = true;
            enemy.agent.isStopped = true;
        }

        stateMachine.ChangeState(enemyMelee.GetRecovery());
    }


    /// <summary>
    /// When exiting the Attack State, disable hitbox, and give control back to navmeshagent
    /// </summary>
    public override void Exit()
    {
        enemyMelee.EnableHitBox(false);
        if (enemy.agent != null)
        {
            // enemy.agent.nextPosition = enemy.transform.position;
            enemy.agent.updateRotation = true; // give control back to agent
            enemy.agent.updatePosition = true;
            // enemy.agent.Warp(enemy.transform.position);
            enemy.agent.isStopped = false;
        }
    }
}
