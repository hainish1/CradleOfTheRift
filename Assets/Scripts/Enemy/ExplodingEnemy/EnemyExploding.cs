using UnityEngine;

public class EnemyExploding : Enemy
{
    [Header("Explosion Settings")]
    public float explosionTimer = 3f;
    public float explosionRadius = 3.5f;
    public float explosionDamage = 10f;
    public GameObject explosionVFX;

    private float timer;
    private ChaseState_ExplodingEnemy chase;
    private ExplodeState_ExplodingEnemy explode;

    public override void Start()
    {
        base.Start();
        chase = new ChaseState_ExplodingEnemy(this, stateMachine);
        explode = new ExplodeState_ExplodingEnemy(this, stateMachine);
        stateMachine.Initialize(chase);
    }

    public void BeginExplosion()
    {
        stateMachine.ChangeState(explode);
    }

    public void ForceExplode() => BeginExplosion(); // in case I want to force it, when bullet it or something else

    public override void Die()
    {
        base.Die();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = aggressionColor;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

    public void CreateExplosionVFX()
    {
        if (explosionVFX == null) return;
        GameObject newFx = Instantiate(explosionVFX);
        newFx.transform.position = transform.position;
        newFx.transform.rotation = Quaternion.identity;

        Destroy(newFx, 1); // destroy after one second
    }
}
