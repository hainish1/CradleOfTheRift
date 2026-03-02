using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Class : Used to knock back a GameObject(Enemy) with a NavMeshAgent on it, which requires special handling
/// </summary>
public class AgentKnockBack : MonoBehaviour
{
    [Header("Tuning")]
    [SerializeField] float decay = 10f;
    [SerializeField] float maxDuration = 0.35f;
    [SerializeField] LayerMask collisionMask = ~0;

    [Header("Settings")]
    [Tooltip("If true, this script will snap the agent to navmesh after knockback")]
    public bool manageAgentPosition = true; // DEFAULT TRUE for GROUND Enemies

    NavMeshAgent agent;
    Vector3 externalVelocity;
    float timer;
    bool active;

    bool cached;
    bool prevUpdatePosition;
    bool prevUpdateRotation;
    bool prevIsStopped;

    EnemyMelee melee; // cached for airborne knockback redirect
    bool isFlyer; // wisp get different knockback 

    [Header("SoftBody")]
    public SoftBodyPhysics softBody;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        softBody = GetComponentInChildren<SoftBodyPhysics>();
        melee = GetComponent<EnemyMelee>();
        isFlyer = GetComponent<EnemyRange>() != null;
    }

    void Update()
    {
        if (!active) return;

        timer += Time.deltaTime;
        Vector3 delta = externalVelocity * Time.deltaTime;

        // for wall blocking
        if (delta.sqrMagnitude > 0.000001f)
        {
            if (Physics.Raycast(transform.position + Vector3.up * 0.2f, delta.normalized, out var hit, delta.magnitude, collisionMask, QueryTriggerInteraction.Ignore))
                delta = delta.normalized * Mathf.Max(0f, hit.distance - 0.02f);
        }

        transform.position += delta;

        // snap to ground surface during knockback to prevent floating
        if (manageAgentPosition && melee != null)
        {
            if (Physics.Raycast(transform.position + Vector3.up * 1f, Vector3.down,
                    out var groundHit, 3f, collisionMask, QueryTriggerInteraction.Ignore))
            {
                float halfH = agent != null ? agent.height * 0.7f : 0f;
                transform.position = new Vector3(transform.position.x, groundHit.point.y + (halfH + melee.startHeightAboveGround), transform.position.z);
            }
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.nextPosition = transform.position; // keep agent in sync
        }

        externalVelocity = Vector3.Lerp(externalVelocity, Vector3.zero, decay * Time.deltaTime);

        if (externalVelocity.sqrMagnitude < 0.0001f || timer >= maxDuration)
        {
            EndKnockback();
        }

    }

    /// <summary>
    /// Apply external impulse to this GameObject, pushing it back in that direction
    /// </summary>
    /// <param name="impulse"></param>
    public void ApplyImpulse(Vector3 impulse)
    {
        // If the enemy is mid leap (in air), redirect impulse into flight velocity
        // so the attack states swept collision physics handles it smoothly
        if (melee != null && melee.isInAir)
        {
            melee.inAirVelocity += impulse;
            if (softBody != null) softBody.Impulse();
            return;
        }

        if (!active)
        {
            active = true;
            timer = 0f;

            // pause steering 
            if (agent != null && agent.isOnNavMesh)
            {
                if (!cached)
                {
                    prevUpdatePosition = agent.updatePosition;
                    prevUpdateRotation = agent.updateRotation;
                    prevIsStopped = agent.isStopped;
                    cached = true;
                }

                if (agent.isActiveAndEnabled && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    // disable during physics control
                    agent.updatePosition = false;
                    agent.ResetPath(); // prevent post knockback glide
                }
            }
        }
        externalVelocity += impulse;
        if (!isFlyer) externalVelocity.y = 0f; // keep knockback horizontal for ground enemies only

        if (softBody != null)
        {
            softBody.Impulse();
        }
    }

    /// <summary>
    /// End the external KnockBack on this GameObject and
    /// give control back to the NavMeshAgent
    /// </summary>
    void EndKnockback()
    {
        active = false;
        externalVelocity = Vector3.zero;

        if (agent == null || !agent.isActiveAndEnabled) return;

        if (manageAgentPosition && melee != null)
        {
            // first find the true physics ground 
            Vector3 correctedPos = transform.position;
            Vector3 rayOrigin = correctedPos + Vector3.up * 5f;
            float halfHeight = agent != null ? agent.height * 0.7f : 0f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out var groundHit, 10f,
                    collisionMask, QueryTriggerInteraction.Ignore))
            {
                correctedPos.y = groundHit.point.y + (halfHeight + melee.startHeightAboveGround);
            }

            const float snapRadius = 6f;
            if (NavMesh.SamplePosition(correctedPos, out var hit, snapRadius, NavMesh.AllAreas))
            {
                // Use NavMesh for XZ but keep the physics-ground Y
                Vector3 finalPos = hit.position;
                finalPos.y = correctedPos.y;

                transform.position = finalPos;
                agent.Warp(finalPos);
                agent.nextPosition = finalPos;
            }
            else
            {
                transform.position = correctedPos;
                agent.nextPosition = correctedPos;
            }
        }
        else if (!manageAgentPosition)
        {
            // For Flying enemy, warp agent back to navmesh without moving actual transform
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(transform.position, out navHit, 80f, NavMesh.AllAreas))
            {
                Vector3 savedPos = transform.position;
                agent.Warp(navHit.position);
                transform.position = savedPos; // restore the actual flight position
            }
        }

        if (cached)
        {
            agent.updatePosition = prevUpdatePosition;
            agent.updateRotation = prevUpdateRotation;
            agent.isStopped = prevIsStopped;
            cached = false;
        }

        // avoid drift
        agent.velocity = Vector3.zero;
        agent.ResetPath();
    }

    /// <summary>
    /// Check if GameObject is in KnockBack state
    /// </summary>
    public bool IsKnockbackActive => active;
}
