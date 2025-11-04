using System.Collections;
using UnityEngine;

public class ExplodeState_ExplodingEnemy : EnemyState
{
    private EnemyExploding enemyExploding;
    private bool exploded;


    public ExplodeState_ExplodingEnemy(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {
        this.enemyExploding = enemy as EnemyExploding;
    }

    public override void Enter()
    {
        base.Enter();

        exploded = false;
        if (enemyExploding.agent != null)
        {
            enemyExploding.agent.isStopped = true;
            enemyExploding.agent.velocity = Vector3.zero;
            enemyExploding.StartCoroutine(ExplodeRoutine());
        }
    }

    private IEnumerator ExplodeRoutine()
    {
        enemyExploding.CreateExplosionVFX();

        DamageNearby();
        exploded = true;
        yield return new WaitForSeconds(0.1f);
        enemyExploding.Die(); // dont need this baby boy anymore

    }


    private void DamageNearby()
    {

        Collider[] hits = Physics.OverlapSphere(enemyExploding.transform.position, enemyExploding.explosionRadius, enemyExploding.playerMask);

        foreach (var col in hits)
        {
            var dmg = col.GetComponentInParent<IDamageable>();
            if (dmg != null && dmg.IsDead != true)
            {
                dmg.TakeDamage(enemyExploding.explosionDamage);
            }
        }
    }
    
    
}