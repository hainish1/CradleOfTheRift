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

    [Header("SoftBody")]
    public SoftBodyPhysics softBody;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        softBody = GetComponentInChildren<SoftBodyPhysics>();
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
        if (!active)
        {
            active = true;
            timer = 0f;

            // pause steering 
            if (agent != null)
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
        externalVelocity.y = 0f; // keep knockback horizontal

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

        if (agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled)
        {
            if (manageAgentPosition)
            {
                agent.Warp(transform.position); // snap to mesh
                // agent.updatePosition = true; // give control back to agent
                agent.nextPosition = transform.position;
            }
            if (cached)
            {
                agent.updatePosition = prevUpdatePosition;
                agent.updateRotation = prevUpdateRotation;
                agent.isStopped = prevIsStopped;
                cached = false;
            }

            // for Flying enemy, we stay where we are in the air, and simply unpause agent logic

            // agent.isStopped = false;
        }
    }

    /// <summary>
    /// Check if GameObject is in KnockBack state
    /// </summary>
    public bool IsKnockbackActive => active;
}
