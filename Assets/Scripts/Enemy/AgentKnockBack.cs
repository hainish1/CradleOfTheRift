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

    NavMeshAgent agent;
    Vector3 externalVelocity;
    float timer;
    bool active;

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
        agent.nextPosition = transform.position; // keep agent in sync

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
                agent.isStopped = true;
                agent.updatePosition = false;
            }
        }
        externalVelocity += impulse;
        externalVelocity.y = 0f;

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

        if (agent != null)
        {
            agent.Warp(transform.position);
            agent.updatePosition = true;
            agent.isStopped = false;
        }
    }

    /// <summary>
    /// Check if GameObject is in KnockBack state
    /// </summary>
    public bool IsKnockbackActive => active;
}
