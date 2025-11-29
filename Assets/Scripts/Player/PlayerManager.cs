using UnityEngine;

public class PlayerManager : Entity
{
    [Header("Debug Stats Display")]
    [SerializeField] private bool showStatsInConsole = true;

    private PlayerHealth playerHealth;
    private PlayerShooter playerShooter;
    private PlayerMovement playerMovement;
    private Vector3 baseScale;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerShooter = GetComponent<PlayerShooter>();
        playerMovement = GetComponent<PlayerMovement>();
        baseScale = transform.localScale;

        if (showStatsInConsole)
        {
            Debug.Log($"Player initialized with stats: {Stats.ToString()}");
        }
    }


    void LateUpdate()
    {
        transform.localScale = baseScale * Stats.CharacterSize;

        if (showStatsInConsole && Time.time % 5f < Time.deltaTime) // Every 5 seconds
        {
            string flightInfo = "";
            if (playerMovement != null)
            {
                float currentEnergy = playerMovement.GetCurrentFlightEnergy();
                flightInfo = $" | Flight Energy: {currentEnergy:F0}";
            }
            // Debug.Log($"Current Player Stats: {Stats.ToString()}{flightInfo}");
        }

        // Show stats when they change
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            string flightInfo = "";
            if (playerMovement != null)
            {
                float currentEnergy = playerMovement.GetCurrentFlightEnergy();
                flightInfo = $", Flight Energy: {currentEnergy:F0}";
            }
            Debug.Log($"Health: {Stats.Health}, MoveSpeed: {Stats.MoveSpeed}, MeleeAttackCooldown: {Stats.AttackSpeed}, ProjectileFireRate: {Stats.ProjectileFireRate}, FireChargeCooldown: {Stats.FireChargeCooldown} , Projectile Damage: {Stats.ProjectileDamage}, Melee Damage: {Stats.MeleeDamage}, Slam Damage: {Stats.SlamDamage}{flightInfo}");
        }
    }

    // Optional: Add method to test stat changes
    // THIS CAN ALSO BE USED TO DIRECTLY ADD A ITEM/MODIFIER TO OUR PLAYER WITHOUT NEEDING TO PICK UP AN ITEM
    [ContextMenu("Test Add Attack Item")]
    void TestAddAttack()
    {
        var modifier = new BasicStatsModifier(StatType.ProjectileDamage, -1, v => v + 5);
        Stats.Mediator.AddModifier(modifier);
        Debug.Log("Added +5 attack item");
    }

    [ContextMenu("Test Add Health Item")]
    void TestAddHealth()
    {
        var modifier = new BasicStatsModifier(StatType.Health, -1, v => v + 20);
        Stats.Mediator.AddModifier(modifier);
        Debug.Log("Added +20 health item");
    }


}
