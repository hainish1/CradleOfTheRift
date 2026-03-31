using System;
using UnityEngine;

public class OrbitingFireballOnLightning : IDisposable
{
    private OrbitingFireballs fireballs;
    private float duration;
    private float damageThreshold;
    private float accumulated;
    private bool disposed;

    public bool IsDisposed => disposed;

    public OrbitingFireballOnLightning(OrbitingFireballs fireballs, float damageThreshold, float duration)
    {
        this.fireballs = fireballs;
        this.damageThreshold = Mathf.Max(1f, damageThreshold);
        this.duration = duration;
        CombatEvents.DamageDealt += OnDamage;
    }

    private void OnDamage(Entity attacker, Component target, float damage, ElementType element)
    {
        if (disposed || fireballs == null || fireballs.IsDisposed) return;
        if (element != ElementType.Lightning) return;

        accumulated += damage;

        while (accumulated >= damageThreshold)
        {
            accumulated -= damageThreshold;
            fireballs.SpawnFireball(duration);
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        CombatEvents.DamageDealt -= OnDamage;
    }
}
