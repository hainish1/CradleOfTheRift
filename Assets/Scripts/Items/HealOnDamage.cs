using System;
using UnityEngine;

public class HealOnDamage : IDisposable
{
    private readonly Entity owner;
    private readonly PlayerHealth ownerHealth;
    private readonly float percentPerStack;
    private int stacks;
    private readonly float duration;
    private float timer;
    private bool disposed;

    public HealOnDamage(Entity owner, float percentPerStack, int initialStacks, float durationSec = -1f)
    {
        this.owner = owner;
        this.ownerHealth = owner.GetComponent<PlayerHealth>();
        this.percentPerStack = percentPerStack;
        this.stacks = Mathf.Max(1, initialStacks);
        this.duration = durationSec;
        this.timer = durationSec;

        CombatEvents.DamageDealt += OnDamageDealt;
    }
    
    public void AddStack(int count = 1) => stacks += Mathf.Max(1, count);

    public void Update(float dt)
    {
        if (duration < 0f || disposed) return;
        timer -= dt;
        if (timer <= 0f) Dispose();
    }

    private void OnDamageDealt(Entity attacker, Component target, float damage, ElementType triggerElement)
    {
        if (disposed || attacker != owner || ownerHealth == null) return;
        float heal = 1 * stacks;
        if (heal > 0f) ownerHealth.Heal(heal);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        CombatEvents.DamageDealt -= OnDamageDealt;
    }
}
