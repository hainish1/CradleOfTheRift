using System;
using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : HealthController
{

    [SerializeField] private float cleanupDelay = 0f;
    public event Action<EnemyHealth> EnemyDied;

    public int baseHealth = 3;

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

    // public void InitializeHealth(float healthMultiplier)
    // {
    //     this.maxHealth = Mathf.CeilToInt(this.maxHealth * healthMultiplier);
    //     this.currentHealth = this.maxHealth;
    //     Debug.Log("Max Health: " + this.maxHealth);
    // }
    public void InitializeHealth(float newHealth)
    {
        // baseHealth = this.maxHealth;
        this.maxHealth = Mathf.CeilToInt(newHealth);
        this.currentHealth = this.maxHealth;
        Debug.Log("Max Health: " + this.maxHealth);
    }


    public int GetMaxHealth()
    {
        return this.maxHealth;
    }
}
