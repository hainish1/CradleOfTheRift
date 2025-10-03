using UnityEngine;
using UnityEngine.UIElements;

public class HealthUI : MonoBehaviour
{
    private ProgressBar healthBar;
    [SerializeField]
    private Health health;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        string progressBarName = "HealthBar";

        this.healthBar = root.Q<ProgressBar>(name: progressBarName);
        int zeroValue = 0;

        this.healthBar.lowValue = zeroValue;
        this.healthBar.highValue = this.health.MaxHealth;
        this.healthBar.value = this.health.MaxHealth;

        this.health.healthChanged += this.OnHealthChange;
    }

    public void OnHealthChange(int healthChange)
    {
        this.healthBar.value = healthChange;
    }
}
