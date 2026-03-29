using System;
using System.Collections.Generic;
using UnityEngine;

public class LightningStrikePlayerChain : IDisposable
{
    private readonly Entity owner;
    private readonly LayerMask enemyLayer;
    private readonly Collider[] overlapBuffer = new Collider[32];
    private readonly HashSet<Enemy> hitBuffer = new HashSet<Enemy>();
    private bool disposed;

    private const int MaxChainCount = 3;
    private const float DamagePercent = 0.5f;

    public LightningStrikePlayerChain(Entity owner)
    {
        this.owner = owner;
        enemyLayer = LayerMask.GetMask("Enemy");
        LightningStrikeEvents.PlayerSelfHit += OnPlayerSelfHit;
    }

    public bool IsDisposed => disposed;

    private void OnPlayerSelfHit(Entity hitOwner, float damage)
    {
        if (disposed || hitOwner != owner || owner == null) return;

        float chainDamage = damage * DamagePercent;
        if (chainDamage <= 0f) return;

        hitBuffer.Clear();
        float chainRange = ChainLightning.DefaultRange;
        Enemy first = FindClosestEnemy(owner.transform.position, chainRange);
        if (first == null) return;

        hitBuffer.Add(first);
        LightningCore.ApplyLightningDamage(owner, first, chainDamage);
        LightningCore.CreateLightningVFX(owner.transform, first.transform, chainRange, 0.2f, null, 0.5f, 0.5f, 0.18f);

        ChainFromEnemy(first, chainDamage, 0, chainRange);
    }

    private void ChainFromEnemy(Enemy from, float damage, int chainNum, float chainRange)
    {
        if (from == null || chainNum >= MaxChainCount) return;

        Enemy closest = FindClosestEnemy(from.transform.position, chainRange);
        if (closest == null) return;

        hitBuffer.Add(closest);
        LightningCore.ApplyLightningDamage(owner, closest, damage);
        LightningCore.CreateLightningVFX(from.transform, closest.transform, chainRange, 0.2f, null, 0.5f, 0.5f, 0.18f);

        ChainFromEnemy(closest, damage, chainNum + 1, chainRange);
    }

    private Enemy FindClosestEnemy(Vector3 fromPos, float range)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(fromPos, range, overlapBuffer, enemyLayer);
        Enemy closest = null;
        float minDist = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Enemy enemy = overlapBuffer[i].GetComponentInParent<Enemy>();
            if (enemy == null || hitBuffer.Contains(enemy)) continue;

            IDamageable dmg = enemy.GetComponent<IDamageable>();
            if (dmg == null || dmg.IsDead) continue;

            float dist = Vector3.Distance(fromPos, enemy.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = enemy;
            }
        }

        return closest;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        LightningStrikeEvents.PlayerSelfHit -= OnPlayerSelfHit;
    }
}
