using System;
using System.Collections.Generic;
using UnityEngine;

public class ToxicAttackSpeed : IDisposable
{
    private const float TickInterval = 0.5f;

    private readonly Entity owner;
    private readonly float range;
    private readonly float attackSpeedPerStack;
    private float nextTickTime;
    private float duration;
    private float timer;
    private bool disposed;
    private readonly LayerMask enemyLayer;
    private readonly Collider[] overlapBuffer = new Collider[64];
    private readonly HashSet<Enemy> seenEnemies = new HashSet<Enemy>();

    private StatModifier meleeSpeedModifier;

    public ToxicAttackSpeed(Entity owner, float range, float attackSpeedPerStack, float durationSec = -1f)
    {
        this.owner = owner;
        this.range = Mathf.Max(0.5f, range);
        this.attackSpeedPerStack = Mathf.Max(0f, attackSpeedPerStack);
        this.duration = durationSec;
        this.timer = durationSec;
        this.nextTickTime = Time.time + TickInterval;
        enemyLayer = LayerMask.GetMask("Enemy");
    }

    public bool IsDisposed => disposed;

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
            TickUpdate();
            nextTickTime = Time.time + TickInterval;
        }
    }

    private void TickUpdate()
    {
        if (owner == null || owner.transform == null) return;

        var stats = owner.Stats;
        if (stats == null || stats.Mediator == null) return;

        int totalPoisonStacks = CountPoisonStacksInRange();

        RemoveModifiers();

        if (totalPoisonStacks > 0 && attackSpeedPerStack > 0f)
        {
            float mult = 1f + totalPoisonStacks * attackSpeedPerStack;
            meleeSpeedModifier = new BasicStatsModifier(StatType.MeleeAnimationSpeed, -1f, v => v * mult);
            stats.Mediator.AddModifier(meleeSpeedModifier);
        }
    }

    /// <summary>
    /// 0 GC: uses OverlapSphereNonAlloc + reused HashSet, no allocations in hot path.
    /// </summary>
    private int CountPoisonStacksInRange()
    {
        seenEnemies.Clear();
        int hitCount = Physics.OverlapSphereNonAlloc(owner.transform.position, range, overlapBuffer, enemyLayer);
        int total = 0;

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = overlapBuffer[i];
            if (col == null) continue;
            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null || seenEnemies.Contains(enemy)) continue;
            if (enemy.GetComponent<IDamageable>()?.IsDead == true) continue;

            seenEnemies.Add(enemy);
            var dotDebuff = enemy.GetComponent<DotDebuff>();
            if (dotDebuff != null)
                total += dotDebuff.GetPoisonStackCount();
        }

        return total;
    }

    private void RemoveModifiers()
    {
        if (meleeSpeedModifier != null)
        {
            meleeSpeedModifier.Dispose();
            meleeSpeedModifier = null;
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        RemoveModifiers();
        seenEnemies.Clear();
    }
}
