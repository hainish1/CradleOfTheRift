using UnityEngine;

public abstract class HealthController : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] protected float maxHealth = 5;
    protected float currentHealth;

    public bool IsDead { get; private set; }

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public virtual void TakeDamage(float damage)
    {
        if (IsDead) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    protected abstract void Die();
}
