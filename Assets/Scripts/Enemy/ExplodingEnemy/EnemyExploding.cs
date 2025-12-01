using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyExploding : Enemy
{
    [Header("Chase Speed")]
    [SerializeField] public float chaseSpeed = 10f;
    [Space]
    [Header("Explosion Settings")]
    public float explosionTimer = 3f;
    public float explosionRadius = 3.5f;
    public float explosionDamage = 10f;
    public GameObject explosionVFX;

    private float timer;
    private ChaseState_ExplodingEnemy chase;
    private ExplodeState_ExplodingEnemy explode;

    private Vector3 arcEnd;
    private Rigidbody rb;
    private bool arcing = false;
    private bool canDie = false;

    public override void Start()
    {
        base.Start();
        if(agent != null)
        {
            agent.speed = chaseSpeed;
        }
        rb = GetComponent<Rigidbody>();
        chase = new ChaseState_ExplodingEnemy(this, stateMachine);
        explode = new ExplodeState_ExplodingEnemy(this, stateMachine);
        stateMachine.Initialize(chase);
    }

    public override void Update()
    {
        base.Update();

        float playerDistance = Vector3.Distance(transform.position, target.position);

        if(playerDistance <= explosionRadius)
        {
            BeginExplosion();
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
        if(canDie) base.Die();
    }


    public void CreateExplosionVFX()
    {
        if (explosionVFX == null) return;
        GameObject newFx = Instantiate(explosionVFX);
        newFx.transform.position = transform.position;
        newFx.transform.rotation = Quaternion.identity;

        Destroy(newFx, 1); // destroy after one second
    }


    public void LaunchWithRigidbody(Vector3 end, float flightTime = 0.7f)
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError("EnemyExploding missing rigidbody");
                return; 
            }
        }
        arcEnd = end;
        if(agent != null)
        {
            agent.enabled = false;
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.useGravity = true;

        Vector3 velocity = CalculateLaunchVelocity(transform.position, arcEnd, flightTime, Physics.gravity.y);
        rb.linearVelocity = velocity;

        arcing = true;
        StartCoroutine(DetectLanding());


    }


    public static Vector3 CalculateLaunchVelocity(Vector3 start, Vector3 end, float flightTime, float gravity = -9.8f)
    {
        Vector3 distance = end - start;
        Vector3 distanceFlat = new Vector3(distance.x, 0f, distance.z);

        float distanceY = distance.y;
        float distanceNor = distanceFlat.magnitude;
        float t = Mathf.Max(0.01f, flightTime);
        
        float yVelocity = (distanceY - 0.5f * gravity * t * t) / t;
        float straightVelocity = distanceNor / t;
        Vector3 result = distanceFlat.normalized * straightVelocity;
        result.y = yVelocity;
        return result;
    }


    private IEnumerator DetectLanding()
    {
        while (arcing)
        {
            if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(arcEnd.x, 0, arcEnd.z)) < 0.2f ||
                Physics.Raycast(transform.position, Vector3.down, 0.9f, NavMesh.AllAreas))
            {
                arcing = false;
                break;
            }
            yield return null;
        }
        ResumeAgentFromRB();
    }

    private void ResumeAgentFromRB()
    {
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true; // take control from the rigidbody
        if (agent != null)
        {
            agent.enabled = true;
            agent.updatePosition = true;
            agent.updateRotation = true;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 1f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                agent.Warp(hit.position);
            }
            agent.isStopped = false;
            stateMachine.ChangeState(chase);
        }

    }

    public void SetCanDie(bool set) => canDie = set;

    void OnDrawGizmos()
    {
        Gizmos.color = aggressionColor;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
