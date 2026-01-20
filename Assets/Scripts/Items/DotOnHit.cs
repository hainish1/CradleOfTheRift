using System;
using UnityEngine;


public class DotOnHit : IDisposable
{
    private readonly Entity owner;
    private readonly float lifetime;
    private float lifeTimer;
    private bool disposed;

    public DotOnHit(Entity owner, float durationSec = -1f)
    {
        this.owner = owner;
        lifetime = durationSec;
        lifeTimer = durationSec;
        CombatEvents.DamageDealt += OnDamageDealt;
    }

    public void AddStack(int count = 1) { }

    public void Update(float dt)
    {
        if (lifetime < 0f || disposed) return;

        lifeTimer -= dt;
        if (lifeTimer <= 0f)
            Dispose();
    }

    private void OnDamageDealt(Entity attacker, Component target, float damage, ElementType triggerElement)
    {
        if (disposed || attacker != owner) return;
        if (!ElementSystem.CanTrigger(triggerElement, ElementType.Poison)) return;
        if (DotDebuff.IsProcessingDotDamage) return;

        var enemy = target as Enemy;
        if (!enemy) return;

        var core = PoisonCore.Active;
        if (core == null) return;
        core.ApplyTo(enemy, owner, true);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        CombatEvents.DamageDealt -= OnDamageDealt;
    }
}

