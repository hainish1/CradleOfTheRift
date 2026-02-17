using System;
using System.Collections.Generic;
using UnityEngine;

public class ArcStrike : IDisposable
{
    private Entity owner;
    private float baseDamage;
    private float range;
    private float poissonLambda;
    private int stacks;
    private float duration;
    private float timer;
    private bool disposed;
    private LayerMask enemyLayer;
    
    private float nextTriggerTime;
    private Dictionary<Enemy, float> targetCooldowns = new Dictionary<Enemy, float>();
    private List<PendingArc> pendingArcs = new List<PendingArc>();
    
    private List<Enemy> candidatesBuffer = new List<Enemy>();
    private List<Enemy> toRemoveBuffer = new List<Enemy>();
    private List<PendingArc> toProcessBuffer = new List<PendingArc>();
    private Collider[] overlapBuffer = new Collider[64];
    
    private struct PendingArc
    {
        public Enemy target;
        public float damage;
        public float triggerTime;
    }

    public ArcStrike(Entity owner, float damage, float range, float poissonLambda = 5.5f, int initialStacks = 1, float durationSec = -1f)
    {
        this.owner = owner;
        this.baseDamage = damage;
        this.range = range;
        this.poissonLambda = poissonLambda;
        this.stacks = initialStacks > 0 ? initialStacks : 1;
        this.duration = durationSec;
        this.timer = durationSec;
        
        enemyLayer = LayerMask.GetMask("Enemy");
        nextTriggerTime = Time.time + GetNextPoissonInterval();
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

        UpdateCooldowns();
        ProcessPendingArcs();
        
        if (Time.time >= nextTriggerTime)
        {
            TriggerBurst();
            nextTriggerTime = Time.time + GetNextPoissonInterval();
        }
    }

    private float GetNextPoissonInterval()
    {
        float u = UnityEngine.Random.Range(0f, 1f);
        return -Mathf.Log(1f - u) / poissonLambda;
    }

    private void TriggerBurst()
    {
        int arcCount = GetBurstCount();
        float baseDelay = 0f;
        
        for (int i = 0; i < arcCount; i++)
        {
            Enemy target = PickWeightedTarget();
            if (target == null) continue;
            
            float microDelay = baseDelay + UnityEngine.Random.Range(0f, 0.12f);
            float preDelay = UnityEngine.Random.Range(0.04f, 0.08f);
            float triggerTime = Time.time + preDelay + microDelay;
            
            pendingArcs.Add(new PendingArc
            {
                target = target,
                damage = baseDamage,
                triggerTime = triggerTime
            });
            
            baseDelay += microDelay;
        }
    }

    private int GetBurstCount()
    {
        float roll = UnityEngine.Random.Range(0f, 1f);
        if (roll < 0.7f) return 1;
        if (roll < 0.95f) return 2;
        return 3;
    }

    private Enemy PickWeightedTarget()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(owner.transform.position, range, overlapBuffer, enemyLayer);
        candidatesBuffer.Clear();
        
        for (int i = 0; i < hitCount; i++)
        {
            Enemy enemy = overlapBuffer[i].GetComponentInParent<Enemy>();
            if (enemy == null) continue;
            
            IDamageable damageable = enemy.GetComponent<IDamageable>();
            if (damageable == null || damageable.IsDead) continue;
            
            if (IsOnCooldown(enemy)) continue;
            
            candidatesBuffer.Add(enemy);
        }
        
        if (candidatesBuffer.Count == 0) return null;
        
        int index = UnityEngine.Random.Range(0, candidatesBuffer.Count);
        return candidatesBuffer[index];
    }

    private bool IsOnCooldown(Enemy enemy)
    {
        if (!targetCooldowns.TryGetValue(enemy, out float cooldownEnd)) return false;
        return Time.time < cooldownEnd;
    }

    private void UpdateCooldowns()
    {
        toRemoveBuffer.Clear();
        foreach (var kvp in targetCooldowns)
        {
            if (Time.time >= kvp.Value)
                toRemoveBuffer.Add(kvp.Key);
        }
        foreach (var enemy in toRemoveBuffer)
            targetCooldowns.Remove(enemy);
    }

    private void ProcessPendingArcs()
    {
        toProcessBuffer.Clear();
        for (int i = pendingArcs.Count - 1; i >= 0; i--)
        {
            if (Time.time >= pendingArcs[i].triggerTime)
            {
                toProcessBuffer.Add(pendingArcs[i]);
                pendingArcs.RemoveAt(i);
            }
        }
        
        foreach (var arc in toProcessBuffer)
        {
            if (arc.target == null || arc.target.GetComponent<IDamageable>()?.IsDead == true)
                continue;
            
            LightningCore.ApplyLightningDamage(owner, arc.target, arc.damage);
            targetCooldowns[arc.target] = Time.time + 0.35f;
            
            float distance = Vector3.Distance(owner.transform.position, arc.target.transform.position);
            float flightTime = Mathf.Lerp(0.07f, 0.14f, distance / range) + 0.3f;
            float startHeight = UnityEngine.Random.Range(0.4f, 0.6f);
            float extendTime = Mathf.Lerp(0.15f, 0.25f, distance / range);
            
            
            LightningCore.CreateLightningVFX(owner.transform, arc.target.transform, range, flightTime, null, startHeight, 0.5f, extendTime, enableArcBend: true);
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        pendingArcs.Clear();
        targetCooldowns.Clear();
    }
}

