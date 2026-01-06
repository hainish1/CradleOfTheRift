using System;
using System.Collections.Generic;
using UnityEngine;

public class ChainLightning : IDisposable
{
    public static bool IsProcessingChain = false;

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

    public static GameObject LightningVFX { get; private set; }

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
        LightningVFX = lightningVFX;
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

        if (triggerElement != ElementType.None) return;

        Enemy enemy = target as Enemy;
        if (enemy == null) return;

        HashSet<Enemy> hit = new HashSet<Enemy> { enemy };
        float chainDamage = damage * chainDamagePercent;
        ChainFromEnemy(enemy, enemy.transform.position, chainDamage, 0, hit);
    }

    private void ChainFromEnemy(Enemy from, Vector3 fromPos, float baseDamage, int chainNum, HashSet<Enemy> hit)
    {
        if (chainNum >= maxChainCount) return;

        IsProcessingChain = true;

        Collider[] nearby = Physics.OverlapSphere(fromPos, chainRange, enemyLayer);
        Enemy closest = null;
        float minDist = float.MaxValue;

        foreach (Collider col in nearby)
        {
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
            
            IDamageable damageable = closest.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                damageable.TakeDamage(baseDamage);
                CombatEvents.ReportDamage(owner, closest, baseDamage, ElementType.Lightning);
                CreateLightningEffect(fromPos, closest.transform.position);
                ChainFromEnemy(closest, closest.transform.position, baseDamage, chainNum + 1, hit);
            }
        }

        IsProcessingChain = false;
    }

    private void CreateLightningEffect(Vector3 from, Vector3 to)
    {
        if (LightningVFX != null)
        {
            GameObject fx = UnityEngine.Object.Instantiate(LightningVFX);
            fx.transform.position = from;
            fx.transform.LookAt(to);
            UnityEngine.Object.Destroy(fx, 1f);
        }
        else
        {
            GameObject go = new GameObject("Lightning");
            go.transform.position = from;

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(0.8f, 0.9f, 1f, 1f);
            lr.endColor = new Color(0.5f, 0.7f, 1f, 0.8f);
            lr.startWidth = 0.3f;
            lr.endWidth = 0.15f;
            lr.positionCount = 8;
            lr.useWorldSpace = true;

            Vector3 dir = (to - from).normalized;
            Vector3 right = Vector3.Cross(dir, Vector3.up).normalized;
            if (right.sqrMagnitude < 0.1f) right = Vector3.Cross(dir, Vector3.right).normalized;

            for (int i = 0; i < 8; i++)
            {
                float t = i / 7f;
                Vector3 pos = Vector3.Lerp(from, to, t);
                float offset = Mathf.Sin(t * Mathf.PI) * 0.5f;
                pos += right * UnityEngine.Random.Range(-offset, offset);
                pos += UnityEngine.Random.insideUnitSphere * 0.2f;
                lr.SetPosition(i, pos);
            }

            UnityEngine.Object.Destroy(go, 0.2f);
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        LightningVFX = null;
        CombatEvents.DamageDealt -= OnDamageDealt;
    }
}

