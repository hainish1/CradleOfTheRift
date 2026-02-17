using System;
using System.Collections.Generic;
using UnityEngine;

public class HealOnPoison : IDisposable
{
    private readonly Entity owner;
    private readonly float range;
    private readonly float healPerPoisonStackPerSecond;
    private const float TickInterval = 1f;
    private float nextTickTime;
    private float duration;
    private float timer;
    private bool disposed;
    private readonly LayerMask enemyLayer;
    private readonly Collider[] overlapBuffer = new Collider[64];
    private readonly HashSet<Enemy> seenEnemies = new HashSet<Enemy>();

    public HealOnPoison(Entity owner, float range, float healPerPoisonStackPerSecond, float durationSec = -1f)
    {
        this.owner = owner;
        this.range = range;
        this.healPerPoisonStackPerSecond = Mathf.Max(0f, healPerPoisonStackPerSecond);
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
            TickHeal();
            nextTickTime = Time.time + TickInterval;
        }
    }

    private void TickHeal()
    {
        if (owner == null || owner.transform == null) return;

        var playerHealth = owner.GetComponent<PlayerHealth>();
        if (playerHealth == null || playerHealth.IsDead) return;

        seenEnemies.Clear();
        int hitCount = Physics.OverlapSphereNonAlloc(owner.transform.position, range, overlapBuffer, enemyLayer);
        int totalPoisonStacks = 0;

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
                totalPoisonStacks += dotDebuff.GetPoisonStackCount();
        }

        float healAmount = totalPoisonStacks * healPerPoisonStackPerSecond * TickInterval;
        if (healAmount > 0f)
            playerHealth.Heal(healAmount);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        seenEnemies.Clear();
    }
}
