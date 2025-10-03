using UnityEngine;
using UnityEngine.UIElements;

public class HealthUI : MonoBehaviour
{
    private ProgressBar healthBar;
    [SerializeField]
    private PlayerHealth playerHealth;

    void Awake()
    {
        // Initialize UI reference in Awake
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        string progressBarName = "HealthBar";
        this.healthBar = root.Q<ProgressBar>(name: progressBarName);
        
        if (healthBar == null)
        {
            Debug.LogWarning("HealthBar not found! Check UIDocument element name.");
        }
        else
        {
            Debug.Log("HealthBar found successfully.");
            this.healthBar.lowValue = 0;
        }
    }
    
    void Start()
    {
        // Subscribe to event in Start (after PlayerHealth.Start() sets initial values)
        if (playerHealth != null)
        {
            this.playerHealth.HealthChanged += OnHealthChange;
            // Force initial update - this now happens after PlayerHealth.Start()
            OnHealthChange(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }
    
    void OnDestroy()
    {
        // Clean up event subscription
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= OnHealthChange;
        }
    }

    public void OnHealthChange(int currentHealth, int maxHealth)
    {
        Debug.Log($"ONHEALTH : {currentHealth} / {maxHealth}");

        this.healthBar.value = currentHealth;
        this.healthBar.highValue = maxHealth;
        this.healthBar.title = $"{currentHealth} / {maxHealth}";

    }
}
