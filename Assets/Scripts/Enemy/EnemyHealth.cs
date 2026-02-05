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
    [SerializeField] private GameObject deathVFX;
    [SerializeField] private Transform deathVFXSpawnPoint; // optional probably

    [Header("Sounds")]
    [SerializeField]
    private AK.Wwise.Event deathSFX;
    [SerializeField]
    private AK.Wwise.Event damagedSFX;


    /// <summary>
    /// Called when any Enemy dies, Resets navmesh agent and destroys gameObject(self)
    /// and does any other required cleanup
    /// </summary>
    protected override void Die()
    {
        // Debug.Log("[Enemy Health] Enemy died");
        if (damageVisuals != null)
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

        PlayDeathSFX();
        PlayPSVFX(deathVFX, deathVFXSpawnPoint != null ? deathVFXSpawnPoint : transform);

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

        if (damageVisuals != null && !IsDead)
        {
            damageVisuals.ShowDamageVisuals(damage);
        }

        if (damagedSFX != null && !IsDead)
        {
            damagedSFX.Post(gameObject);
        }


    }

    /// <summary>
    /// Posts the enemy death
    /// Wwise Event.
    /// </summary>
    private void PlayDeathSFX()
    {
        if (deathSFX != null)
            deathSFX.Post(gameObject);
    }

    public void PlayPSVFX(GameObject vfxPrefab, Transform spawnPos)
    {
        if (vfxPrefab == null) return;

        spawnPos = spawnPos != null ? spawnPos : transform;

        GameObject fx;
        if (ObjectPool.instance != null)
        {
            fx = ObjectPool.instance.GetObject(vfxPrefab, spawnPos);
            // fx.transform.SetPositionAndRotation(spawnPos.position, spawnPos.rotation);

        }
        else
        {
            fx = Instantiate(vfxPrefab, spawnPos.position, spawnPos.rotation);
            
        }

        float lifetime = EstimateParticleLifetime(fx);

        if (ObjectPool.instance != null)
        {
            ObjectPool.instance.ReturnObject(fx, lifetime);
        }
        else
        {
            Destroy(fx, lifetime);
        }
    }

    private float EstimateParticleLifetime(GameObject fx)
    {
        float max = 0.25f;

        var systems = fx.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            var main = ps.main;
            float startDelay = main.startDelay.constantMax;
            float duration = main.duration;
            float startLifetime = main.startLifetime.constantMax;

            float total = startDelay + duration + startLifetime;
            if (total > max) max = total;
        }

        return max;
    }
}
