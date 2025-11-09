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


    private Vector3 arcStart, arcEnd;
    private float arcHeight;
    private float arcDuration;
    private float arcSpeed;
    private float arcTimer = 0;
    private bool isArcing = false;

    public override void Start()
    {
        base.Start();
        chase = new ChaseState_ExplodingEnemy(this, stateMachine);
        explode = new ExplodeState_ExplodingEnemy(this, stateMachine);
        stateMachine.Initialize(chase);
    }

    public override void Update()
    {
        base.Update();

        if (isArcing)
        {
            arcTimer += Time.deltaTime;
            float t = Mathf.Clamp01(arcTimer / (arcDuration / arcSpeed));
            Vector3 pos = Vector3.Lerp(arcStart, arcEnd, t);
            pos.y += Mathf.Sin(Mathf.PI *t) * arcHeight;
            transform.position = pos;

            // face direction of movement
            Vector3 look = arcEnd - arcStart; look.y = 0;
            if (look.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(look);
            }
            

            if(t >= 1f)
            {
                isArcing = false;
                if (agent)
                {
                    transform.position = arcEnd;
                    agent.enabled = true;

                    if (agent.isOnNavMesh)
                    {
                        agent.Warp(transform.position);
                        stateMachine.ChangeState(chase);
                    }
                }
            }
        }

        
    }

    public void BeginExplosion()
    {
        stateMachine.ChangeState(explode);
    }

    public void ForceExplode() => BeginExplosion(); // in case I want to force it, when bullet it or something else

    public override void Die()
    {
        ForceExplode();
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

    public void LaunchAsArc(Vector3 end, float height, float duration, float speed)
    {
        if (agent) agent.enabled = false;

        arcStart = transform.position;
        arcEnd = end;
        arcHeight = height;
        arcDuration = duration;
        arcSpeed = speed;
        arcTimer = 0;
        isArcing = true;

    }
}
