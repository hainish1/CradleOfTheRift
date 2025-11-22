using System;
using UnityEngine;
    using UnityEngine.SceneManagement;

public class PlayerHealth : HealthController
{
    // note to self - THIS is player MANAGER that INHERITS from entity
    private Entity playerEntity;
    public event Action LoseScreen;
    public static bool GameIsOver = false; // true when win/lose screen is up
    public event Action<float, float> healthChanged;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private bool canTakeDamage = true;

    public static PlayerHealth instance;
    protected override void Awake()
    {
        base.Awake();
        instance = this;
        GameIsOver = false;

    }

    void Start()
    {
        playerEntity = GetComponent<Entity>();
        GameIsOver = false;
        if (playerEntity != null)
        {
            // maxHealth = Mathf.RoundToInt(playerEntity.Stats.Health);
            maxHealth = Mathf.Max(1f, playerEntity.Stats.Health);

            currentHealth = maxHealth;

            Debug.Log($"Player health initialized with heatlh-statsL {maxHealth}");
            healthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    void Update()
    {
        if (playerEntity == null || playerEntity.Stats == null)
        {
            return;
        }
        // int newMaxHealth = Mathf.RoundToInt(playerEntity.Stats.Health);
        float newMaxHealth = playerEntity.Stats.Health;

        // If max health changed (due to item pickup), adjust current health proportionally
        if (newMaxHealth != maxHealth)
        {
            float healthRatio = (float)currentHealth / maxHealth;
            maxHealth = newMaxHealth;
            // currentHealth = Mathf.RoundToInt(healthRatio * maxHealth);
            currentHealth = maxHealth;

            Debug.Log($"Max health updated to: {maxHealth}, Current: {currentHealth}");
            healthChanged?.Invoke(currentHealth, maxHealth);
        }
    }



    protected override void Die()
    {
        Debug.Log("[PLAYER HEALTH] Player is DEADDD lmao");
        this.LoseScreen?.Invoke();
        GameIsOver = true;


        // SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        // end movement or change scene here if we want
    }

    // maybe useful later idk
    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }

    public override void TakeDamage(float damage)
    {
        if (canTakeDamage == false || IsDead) return;
        base.TakeDamage(damage);
        healthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"[PLAYER HEALTH] Player took {damage} damage, current health: {currentHealth}/{maxHealth}");
    }

    public virtual void RestoreFullHealth()
    {
        currentHealth = maxHealth;
        healthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log("Health Fully Resotored");
    }

    public virtual void SetCanTakeDamage(bool enable)
    {
        this.canTakeDamage = enable;
    }

    public override void Heal(float amount)
    {
        base.Heal(amount);
        healthChanged?.Invoke(currentHealth, maxHealth); // notify UI
    }
}
