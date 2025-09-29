using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField]
    private int maxHealth;
    private int currentHealth;
    public event Action<int> healthChanged;
    public int CurrentHealth
    {
        get => this.currentHealth;
        set
        {
            this.currentHealth = value;
            this.healthChanged.Invoke(this.currentHealth);
        }
    }

    void Awake()
    {
        this.currentHealth = maxHealth;
    }

    void TakeDamage(int damage)
    {
        CurrentHealth = Math.Max(0, CurrentHealth - damage);
    }

    public int MaxHealth => this.maxHealth;
}
