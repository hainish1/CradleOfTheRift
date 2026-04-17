using System;
using System.Collections.Generic;
using UnityEngine;

public class ChainLightning : IDisposable
{
    public static bool IsProcessingChain = false;
    public const float DefaultRange = 16f;
    private const float VFXDuration = 0.5f;

    private Entity owner;
    private float baseChainDamagePercent;
    private int baseMaxChainCount;
    private float baseChainRange;
    private LayerMask enemyLayer;
    private int stacks;
    private float duration;
    private float timer;
    private bool disposed;

    private float chainDamagePercent;
    private int maxChainCount;
    private float chainRange;
    
    private HashSet<Enemy> hitEnemiesCache = new HashSet<Enemy>();

    private static readonly Collider[] s_overlapBuffer = new Collider[64];

    public float CurrentRange => chainRange;

    public ChainLightning(Entity owner, float chainDamagePercent, int maxChainCount, float chainRange, int initialStacks = 1, float durationSec = -1f, GameObject lightningVFX = null)
    {
        this.owner = owner;
        baseChainDamagePercent = chainDamagePercent;
        baseMaxChainCount = maxChainCount;
        baseChainRange = chainRange;
        stacks = initialStacks > 0 ? initialStacks : 1;
        duration = durationSec;
        timer = durationSec;

        enemyLayer = LayerMask.GetMask("Enemy");
        UpdateValues();
        CombatEvents.DamageDealt += OnDamageDealt;
    }

    public void AddStack(int count = 1)
    {
        stacks += count > 0 ? count : 1;
        if (stacks <= 0) Dispose();
        else UpdateValues();
    }

    private void UpdateValues()
    {
        chainDamagePercent = baseChainDamagePercent + (stacks - 1) * 0.05f;
        maxChainCount = baseMaxChainCount + (stacks - 1);
        chainRange = baseChainRange;
    }

    public void Update(float dt)
    {
        if (duration < 0f || disposed) return;
        timer -= dt;
        if (timer <= 0f) Dispose();
    }

    private void OnDamageDealt(Entity attacker, Component target, float damage, ElementType triggerElement)
    {
        if (disposed || attacker != owner || IsProcessingChain) return;

        if (!ElementSystem.CanTrigger(triggerElement, ElementType.Lightning)) return;

        Enemy enemy = target as Enemy;
        if (enemy == null) return;

        // reset the hit list and start the chain
        hitEnemiesCache.Clear();
        hitEnemiesCache.Add(enemy);
        float chainDamage = damage * chainDamagePercent;
        ChainFromEnemy(enemy, enemy.transform.position, chainDamage, 0, hitEnemiesCache);
    }

    private void ChainFromEnemy(Enemy from, Vector3 fromPos, float baseDamage, int chainNum, HashSet<Enemy> hit)
    {
        if (chainNum >= maxChainCount) return;

        IsProcessingChain = true;

        int hitCount = Physics.OverlapSphereNonAlloc(fromPos, chainRange, s_overlapBuffer, enemyLayer);
        Enemy closest = null;
        float minDist = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = s_overlapBuffer[i];
            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null || hit.Contains(enemy)) continue;

            IDamageable dmg = enemy.GetComponent<IDamageable>();
            if (dmg == null || dmg.IsDead) continue;

            float dist = Vector3.Distance(fromPos, enemy.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = enemy;
            }
        }

        if (closest != null)
        {
            hit.Add(closest);
            
            LightningCore.ApplyLightningDamage(owner, closest, baseDamage);
            LightningCore.CreateLightningVFX(from.transform, closest.transform, chainRange, VFXDuration, null, 0.5f, 0.5f, 0.18f);
            
            ChainFromEnemy(closest, closest.transform.position, baseDamage, chainNum + 1, hit);
        }

        IsProcessingChain = false;
    }


    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        CombatEvents.DamageDealt -= OnDamageDealt;
    }
}

