using UnityEngine;

public class PlayerManager : Entity
{
    [Header("Debug Stats Display")]
    [SerializeField] private bool showStatsInConsole = true;

    private PlayerHealth playerHealth;
    private PlayerShooter playerShooter;

    void Start()
    {
        // Get references to player components
        playerHealth = GetComponent<PlayerHealth>();
        playerShooter = GetComponent<PlayerShooter>();

        if (showStatsInConsole)
        {
            Debug.Log($"Player initialized with stats: {Stats.ToString()}");
        }
    }


    void LateUpdate()
    {
        if (showStatsInConsole && Time.time % 5f < Time.deltaTime) // Every 5 seconds
        {
            Debug.Log($"Current Player Stats: {Stats.ToString()}");
        }
        
        // Show stats when they change
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Debug.Log($"=== CURRENT STATS ===");
            Debug.Log($"Health: {Stats.Health}");
            Debug.Log($"MoveSpeed: {Stats.MoveSpeed}");
            Debug.Log($"Attack: {Stats.Attack}");
            Debug.Log($"===================");
        }
    }
    
    // Optional: Add method to test stat changes
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
