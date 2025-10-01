using System;
using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : HealthController
{

    [SerializeField] private float cleanupDelay = 0f;
    public event Action<EnemyHealth> EnemyDied;

    protected override void Die()
    {
        Debug.Log("[Enemy Health] Enemy died");

        var agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            if (agent.isOnNavMesh) agent.isStopped = true;
            agent.enabled = false;
        }

        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        EnemyDied?.Invoke(this);
        PlayerGold.Instance.AddGold(3); // Set it to 3 for now

        Destroy(gameObject, cleanupDelay);
    }
}
