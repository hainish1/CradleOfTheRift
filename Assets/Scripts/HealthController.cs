using UnityEngine;

public abstract class HealthController : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] protected int maxHealth = 5;
    protected int currentHealth;

    public void InitializeHealth(float healthMultiplier)
    {
        this.maxHealth = Mathf.CeilToInt(this.maxHealth * healthMultiplier);
        this.currentHealth = this.maxHealth;
        Debug.Log("Max Health: " + this.maxHealth);
    }

    public bool IsDead { get; private set; }

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
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
