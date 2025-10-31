using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Class : Handles Enemy Health, implements methods defined in the health controller
/// </summary>
public class EnemyHealth : HealthController
{

    [SerializeField] private float cleanupDelay = 0f;
    public event Action<EnemyHealth> EnemyDied;
    public float baseHealth = 3;

    [Header("Visuals")]
    [SerializeField] private EnemyDamageVisuals damageVisuals;


    /// <summary>
    /// Called when any Enemy dies, Resets navmesh agent and destroys gameObject(self)
    /// and does any other required cleanup
    /// </summary>
    protected override void Die()
    {
        // Debug.Log("[Enemy Health] Enemy died");
        if(damageVisuals != null)
        {
            damageVisuals.SetDeadForVisuals();
        }

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

    /// <summary>
    /// Initializes Enemy health when it is created
    /// </summary>
    /// <param name="newHealth"></param>
    public void InitializeHealth(float newHealth)
    {
        // baseHealth = this.maxHealth;
        // this.maxHealth = Mathf.CeilToInt(newHealth);
        this.maxHealth = newHealth;
        this.currentHealth = this.maxHealth;
        Debug.Log("Max Health: " + this.maxHealth); // not needed anymore
    }

    /// <summary>
    /// Get current MaxHealth of enemy
    /// </summary>
    /// <returns></returns>
    public float GetMaxHealth()
    {
        return this.maxHealth;
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);

        if(damageVisuals != null && !IsDead)
        {
            damageVisuals.ShowDamageVisuals(damage);
        }
    }
}
