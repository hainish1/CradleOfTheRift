using System;
using UnityEngine;

public class OrbitingFireballOnKill : IDisposable
{
    private OrbitingFireballs fireballs;
    private float duration;
    private bool disposed;

    public bool IsDisposed => disposed;

    public OrbitingFireballOnKill(OrbitingFireballs fireballs, float duration)
    {
        this.fireballs = fireballs;
        this.duration = duration;
        CombatEvents.DamageDealt += OnDamage;
    }

    private void OnDamage(Entity attacker, Component target, float damage, ElementType element)
    {
        if (disposed || fireballs == null || fireballs.IsDisposed) return;
        var damageable = target.GetComponent<IDamageable>();
        if (damageable != null && damageable.IsDead)
            fireballs.SpawnFireball(duration);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        CombatEvents.DamageDealt -= OnDamage;
    }
}
