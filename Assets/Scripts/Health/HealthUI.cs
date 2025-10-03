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

        EventCallback<GeometryChangedEvent> geometryCallback = null;
        geometryCallback = (evt) =>
        {
            if (health != null)
            {
                healthBar.highValue = health.MaxHealth;
                healthBar.value = health.CurrentHealth;

                healthBar.title = $"Health: {health.CurrentHealth}/{health.MaxHealth}";

                healthBar.style.visibility = Visibility.Visible;

                health.healthChanged += OnHealthChange;
            }

            healthBar.UnregisterCallback<GeometryChangedEvent>(geometryCallback);
        };

        healthBar.RegisterCallback<GeometryChangedEvent>(geometryCallback);
    }

    void OnDestroy()
    {
        if (health != null)
            health.healthChanged -= OnHealthChange;
    }

    public void OnHealthChange(int currentHealth, int maxHealth)
    {
        if (healthBar == null) return;

        healthBar.highValue = maxHealth;
        healthBar.value = currentHealth;
        healthBar.title = $"Health: {currentHealth}/{maxHealth}";
    }
}
