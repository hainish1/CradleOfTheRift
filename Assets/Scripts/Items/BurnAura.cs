using System;
using System.Collections.Generic;
using UnityEngine;

public class BurnAura : IDisposable
{
    private Entity owner;
    private float damagePerSecond;
    private float range;
    private int stacks;
    private float duration;
    private float timer;
    private bool disposed;
    private float tickInterval;
    private float nextTickTime;
    private LayerMask enemyLayer;

    public BurnAura(Entity owner, float damagePerSecond, float range, int initialStacks = 1, float durationSec = -1f, float tickInterval = 1f)
    {
        this.owner = owner;
        this.damagePerSecond = damagePerSecond;
        this.range = range;
        this.stacks = initialStacks > 0 ? initialStacks : 1;
        this.duration = durationSec;
        this.timer = durationSec;
        this.tickInterval = Mathf.Max(0.1f, tickInterval);
        this.nextTickTime = Time.time + this.tickInterval;

        enemyLayer = LayerMask.GetMask("Enemy");
    }

    public void AddStack(int count = 1)
    {
        stacks += count > 0 ? count : 1;
        if (stacks <= 0) Dispose();
    }

    public void Update(float dt)
    {
        if (disposed) return;
        
        if (duration > 0f)
        {
            timer -= dt;
            if (timer <= 0f)
            {
                Dispose();
                return;
            }
        }

        if (Time.time >= nextTickTime)
        {
            ApplyBurnDamage();
            nextTickTime = Time.time + tickInterval;
        }
    }

    private void ApplyBurnDamage()
    {
        if (owner == null || owner.transform == null) return;

        float damagePerTick = damagePerSecond * tickInterval;
        
        Collider[] nearby = Physics.OverlapSphere(owner.transform.position, range, enemyLayer);
        HashSet<Enemy> hitEnemies = new HashSet<Enemy>();
        
        foreach (Collider col in nearby)
        {
            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null || hitEnemies.Contains(enemy)) continue;

            IDamageable damageable = enemy.GetComponent<IDamageable>();
            if (damageable == null || damageable.IsDead) continue;

            hitEnemies.Add(enemy);
            damageable.TakeDamage(damagePerTick);
            CombatEvents.ReportDamage(owner, enemy, damagePerTick, ElementType.Fire);
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
    }
}

