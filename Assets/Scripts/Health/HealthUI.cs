using System;
using UnityEngine;
using UnityEngine.UIElements;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth health; 

    private ProgressBar healthBar;

    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        healthBar = root.Q<ProgressBar>("HealthBar");
        healthBar.lowValue = 0;

        healthBar.highValue = Mathf.CeilToInt(health.MaxHealth);
        healthBar.value = Mathf.CeilToInt(health.CurrentHealth);

        healthBar.title = $"Health: {Mathf.CeilToInt(health.CurrentHealth)}/{Mathf.CeilToInt(health.MaxHealth)}";

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

        healthBar.highValue = Mathf.CeilToInt(maxHealth);
        healthBar.value = Mathf.CeilToInt(currentHealth);
        healthBar.title = $"Health: {Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
    }
}
