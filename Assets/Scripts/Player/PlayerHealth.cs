using UnityEngine;

public class PlayerHealth : HealthController
{
    // note to self - THIS is player MANAGER that INHERITS from entity
    private Entity playerEntity;

    void Start()
    {
        playerEntity = GetComponent<Entity>();

        if (playerEntity != null)
        {
            maxHealth = playerEntity.Stats.Health;
            currentHealth = maxHealth;

            Debug.Log($"Player health initialized with heatlh-statsL {maxHealth}");
        }
    }

    void Update()
    {
        if (playerEntity != null)
        {
            int newMaxHealth = playerEntity.Stats.Health;

            // If max health changed (due to item pickup), adjust current health proportionally
            if (newMaxHealth != maxHealth)
            {
                float healthRatio = (float)currentHealth / maxHealth;
                maxHealth = newMaxHealth;
                currentHealth = Mathf.RoundToInt(healthRatio * maxHealth);

                Debug.Log($"Max health updated to: {maxHealth}, Current: {currentHealth}");
            }
        }
    }



    protected override void Die()
    {
        Debug.Log("[PLAYER HEALTH] Player is DEADDD lmao");

        // end movement or change scene here if we want
    }
    
    // maybe useful later idk
    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }
}
