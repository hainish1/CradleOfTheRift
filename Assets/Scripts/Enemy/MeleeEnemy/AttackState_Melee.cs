using UnityEngine;

public class AttackState_Melee : EnemyState
{
    private EnemyMelee enemyMelee;

    private enum Phase {Windup, Charge}
    private Phase phase;
    private float timer;
    private Vector3 chargeDirection;
    private Quaternion lockedChargeRot;

    float endTime;

    public AttackState_Melee(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        enemyMelee = enemy as EnemyMelee;

    }

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
        // TryApplyHit();
    }

    public override void Update()
    {

        timer -= Time.deltaTime;
        if (phase == Phase.Windup)
        {
            FaceTarget(enemy.turnSpeed); // only face during windup


            if (timer <= 0f)
            {
                // lock in straight direction
                chargeDirection = enemy.target ? (enemy.target.position - enemy.transform.position) : enemy.transform.forward;
                chargeDirection.y = 0f;
                if (chargeDirection.sqrMagnitude > 0.0001f)
                {
                    chargeDirection.Normalize();
                }
                lockedChargeRot = Quaternion.LookRotation(chargeDirection, Vector3.up);
                enemy.transform.rotation = lockedChargeRot;

                enemyMelee.EnableHitBox(true);

                phase = Phase.Charge;
                timer = enemyMelee.chargetTime;
            }
        }
        else // Charge phase
        {
            enemy.transform.rotation = lockedChargeRot;
            if (enemy.agent != null)
                enemy.agent.Move(chargeDirection * enemyMelee.chargeSpeed * Time.deltaTime);
            else
                enemy.transform.position += chargeDirection * enemyMelee.chargeSpeed * Time.deltaTime;

            // TryApplyHit(); // nbw the trigger will decide player's fate

            if (timer <= 0f)
                stateMachine.ChangeState(enemyMelee.GetRecovery());
        }
    }


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
