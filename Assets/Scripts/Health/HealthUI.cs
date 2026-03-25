using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth health;

    [Tooltip("How long to hold the red damage bar before it starts shrinking")]
    [SerializeField] private float damageHoldDuration = 0.5f;

    [Tooltip("How long the red bar takes to drain down to the new health value")]
    [SerializeField] private float damageDrainDuration = 0.4f;

    private ProgressBar healthBar;
    private ProgressBar damageBar;

    private Coroutine drainCoroutine;

    void Start()
    {
        if (health == null)
            health = PlayerLocator.FindPlayerComponent<PlayerHealth>();

        if (health == null)
        {
            Debug.LogWarning("Health component not on player or player missing");
            return;
        }

        var root = GetComponent<UIDocument>().rootVisualElement;

        // Damage bar sits behind the health bar (declared first in UXML)
        damageBar = root.Q<ProgressBar>("DamageBar");
        healthBar  = root.Q<ProgressBar>("HealthBar");

        InitBar(healthBar);
        InitBar(damageBar);

        healthBar.value  = health.CurrentHealth;
        damageBar.value  = health.CurrentHealth;
        healthBar.title  = FormatTitle(health.CurrentHealth, health.MaxHealth);

        healthBar.style.visibility = Visibility.Visible;
        damageBar.style.visibility = Visibility.Visible;

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

        float previousHealth = healthBar.value;

        // Update the main health bar immediately
        healthBar.highValue  = maxHealth;
        healthBar.value      = currentHealth;
        healthBar.title      = FormatTitle(currentHealth, maxHealth);

        damageBar.highValue  = maxHealth;

        // Only play the damage animation when health decreases
        if (currentHealth < previousHealth)
        {
            // Keep damage bar at the old value, then drain it
            damageBar.value = previousHealth;

            if (drainCoroutine != null)
                StopCoroutine(drainCoroutine);

            drainCoroutine = StartCoroutine(DrainDamageBar(previousHealth, currentHealth));
        }
        else
        {
            // Healing: snap damage bar to match instantly (no ghost needed)
            damageBar.value = currentHealth;
        }
    }

    private IEnumerator DrainDamageBar(float fromValue, float toValue)
    {
        // Hold the red bar briefly so the player can see it
        yield return new WaitForSeconds(damageHoldDuration);

        // Smoothly drain down to the new health value
        float elapsed = 0f;
        while (elapsed < damageDrainDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / damageDrainDuration);
            // Ease-out so it decelerates as it reaches the target
            t = 1f - (1f - t) * (1f - t);
            damageBar.value = Mathf.Lerp(fromValue, toValue, t);
            yield return null;
        }

        damageBar.value = toValue;
        drainCoroutine  = null;
    }

    private void InitBar(ProgressBar bar)
    {
        bar.lowValue  = 0;
        bar.highValue = health.MaxHealth;
        bar.value     = health.MaxHealth;
    }

    private static string FormatTitle(float current, float max)
        => $"HP  {(int)current} / {(int)max}";
}
