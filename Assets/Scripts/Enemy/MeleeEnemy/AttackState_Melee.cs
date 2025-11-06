using System;
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
    private Vector3 chargeDirection;
    private Quaternion lockedChargeRot;

    private Vector3 leapStartPosition;
    private Vector3 leapTargetPosition;
    private float leapTimer;

    float endTime;

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

            enemy.agent.updateRotation = false;
        }

        enemyMelee.hitAppliedThisAttack = false;
        enemyMelee.EnableHitBox(false);

        // small windup, basically freeze in place
        phase = Phase.Windup;
        timer = enemyMelee.windupTime;
        leapTimer = 0f;
        // TryApplyHit();
    }


    /// <summary>
    /// Face the Target, Enable the Attack Hitbox, and Charge towards the player, once done change to Recovery State
    /// </summary>
    public override void Update()
    {
        
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
        FaceTarget(enemy.turnSpeed);

        if(timer <= 0f)
        {
            if (enemy.target != null)
            {
                leapStartPosition = enemy.transform.position;
                leapTargetPosition = enemy.target.position;

                leapTargetPosition.y += 0.2f; // THIS MIGHT NEED CHANGE

                chargeDirection = leapTargetPosition - leapStartPosition;
                chargeDirection.y = 0f; // keep horizontal for rotation

                if (chargeDirection.sqrMagnitude > 0.0001f)
                {
                    chargeDirection.Normalize();
                    lockedChargeRot = Quaternion.LookRotation(chargeDirection, Vector3.up);
                    enemy.transform.rotation = lockedChargeRot;
                }
            }

            phase = Phase.Leap;
            timer = enemyMelee.leapDuration;
            leapTimer = 0f;

        }
    }

    private void HandleLeap()
    {
        leapTimer += Time.deltaTime;
        float t = Mathf.Clamp01(leapTimer / enemyMelee.leapDuration);
        Vector3 currentPos = Vector3.Lerp(leapStartPosition, leapTargetPosition, t);
        float heightOffset = enemyMelee.leapHeight * Mathf.Sin(t * Mathf.PI);
        currentPos.y += heightOffset;

        if (enemy.agent != null)
        {
            enemy.agent.updatePosition = false;
            enemy.transform.position = currentPos;
        }
        else
        {
            enemy.transform.position = currentPos;
        }
        enemy.transform.rotation = lockedChargeRot;


        enemyMelee.EnableHitBox(true);

        if (timer <= 0f || t >= 1f)
        {
            if (enemy.agent != null)
            {
                enemy.agent.updatePosition = true;
                enemy.agent.Warp(enemy.transform.position);
            }
            enemyMelee.EnableHitBox(false);
            stateMachine.ChangeState(enemyMelee.GetRecovery());
        }
    }



    /// <summary>
    /// When exiting the Attack State, disable hitbox, and give control back to navmeshagent
    /// </summary>
    public override void Exit()
    {
        enemyMelee.EnableHitBox(false);
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
