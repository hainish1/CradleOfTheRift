using System;
using UnityEngine;
    using UnityEngine.SceneManagement;

public class PlayerHealth : HealthController
{
    // note to self - THIS is player MANAGER that INHERITS from entity
    private Entity playerEntity;
    public event Action<int, int> HealthChanged;
    public int CurrentHealth => this.currentHealth;
    public int MaxHealth => this.maxHealth;

    void Start()
    {
        playerEntity = GetComponent<Entity>();

        if (playerEntity != null)
        {
            maxHealth = Mathf.RoundToInt(playerEntity.Stats.Health);

            currentHealth = maxHealth;

            Debug.Log($"Player health initialized with heatlh-statsL {maxHealth}");
            HealthChanged?.Invoke(this.currentHealth, this.maxHealth);
        }
    }

    void Update()
    {
        if (playerEntity != null)
        {
            int newMaxHealth = Mathf.RoundToInt(playerEntity.Stats.Health);

            // If max health changed (due to item pickup), adjust current health proportionally
            if (newMaxHealth != maxHealth)
            {
                float healthRatio = (float)currentHealth / maxHealth;
                maxHealth = newMaxHealth;
                currentHealth = Mathf.RoundToInt(healthRatio * maxHealth);

                Debug.Log($"Max health updated to: {maxHealth}, Current: {currentHealth}");
                HealthChanged?.Invoke(this.currentHealth, this.maxHealth);
            }
        }
    }



    protected override void Die()
    {
        Debug.Log("[PLAYER HEALTH] Player is DEADDD lmao");

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        // end movement or change scene here if we want
    }

    // maybe useful later idk
    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);

        HealthChanged?.Invoke(this.currentHealth, this.maxHealth);
    }
}
