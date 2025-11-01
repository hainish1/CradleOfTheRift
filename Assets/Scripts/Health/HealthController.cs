using UnityEngine;

public abstract class HealthController : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] protected float maxHealth = 5;
    protected float currentHealth;

    public bool IsDead { get; private set; }

    protected virtual void Awake()
    {
        currentHealth = Mathf.Max(1f, maxHealth);
        IsDead = false;
    }

    public virtual void TakeDamage(float damage)
    {
        if (IsDead) return;

        currentHealth -= Mathf.Max(0f, damage);
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            IsDead = true;
            Die();
        }
    }

    public virtual void Heal(float amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.Max(0f, amount)); // either this or that
    }

    protected abstract void Die();
}
