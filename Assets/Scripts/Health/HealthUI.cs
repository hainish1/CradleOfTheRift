using System;
using UnityEngine;
using UnityEngine.UIElements;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth health; 

    private ProgressBar healthBar;

    void Start()
    {
        if(health == null)
        {
            health = PlayerLocator.FindPlayerComponent<PlayerHealth>();
        }

        if(health == null)
        {
            Debug.LogWarning("Health component not on player or player missing");
            return;
        }

        var root = GetComponent<UIDocument>().rootVisualElement;
        healthBar = root.Q<ProgressBar>("HealthBar");
        healthBar.lowValue = 0;

        healthBar.highValue = health.MaxHealth;
        healthBar.value = health.CurrentHealth;

        healthBar.title = $"Health: {health.CurrentHealth}/{health.MaxHealth}";

        healthBar.style.visibility = Visibility.Visible;

        health.healthChanged += OnHealthChange;
    }

    void OnDestroy()
    {
        if (health != null)
            health.healthChanged -= OnHealthChange;
    }

    public void OnHealthChange(float currentHealth, float maxHealth)
    {
        if (healthBar == null) return;

        healthBar.highValue = maxHealth;
        healthBar.value = currentHealth;
        healthBar.title = $"Health: {(int)currentHealth}/{maxHealth}";
    }
}
