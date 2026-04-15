using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLightningStrike : IDisposable
{
    private readonly Entity owner;
    private readonly float baseDamage;
    private readonly float radius;
    private readonly float interval;
    private readonly float electrifyDamage;
    private readonly GameObject strikeVFX;
    private readonly LayerMask enemyLayer;
    private int stacks;
    private float duration;
    private float timer;
    private float nextStrikeTime;
    private bool disposed;

    private const int ChainOnPlayerHitMaxCount = 3;
    private const float ChainOnPlayerHitDamagePercent = 0.5f;
    private const float ChainOnPlayerHitRangeMultiplier = 1.5f;
    private const float StrikeDelay = 0.5f;
    private const float StrikeHeight = 10f;
    private const float StrikeVfxDuration = 0.25f;
    /// <summary>Pool electrify search radius - larger than damage radius so projectile-created pools (far from player) can be electrified.</summary>
    private const float ElectrifyPoolSearchRadius = 25f;

    private struct PendingStrike
    {
        public Vector3 groundPoint;
        public float triggerTime;
    }

    private readonly List<PendingStrike> pending = new List<PendingStrike>();

    public PlayerLightningStrike(
        Entity owner,
        float damage,
        float radius,
        float interval,
        float electrifyDamage,
        int initialStacks = 1,
        float durationSec = -1f,
        GameObject strikeVFX = null)
    {
        this.owner = owner;
        this.baseDamage = damage;
        this.radius = radius;
        this.interval = Mathf.Max(0.1f, interval);
        this.electrifyDamage = Mathf.Max(0f, electrifyDamage);
        this.strikeVFX = strikeVFX;
        this.stacks = initialStacks > 0 ? initialStacks : 1;
        this.duration = durationSec;
        this.timer = durationSec;
        this.nextStrikeTime = Time.time + this.interval;
        enemyLayer = LayerMask.GetMask("Enemy");

        // now triggers on every projectile throw regardless of weapon
        PlayerShooter.OnProjectileFired += OnProjectileFired;
    }

    private void OnProjectileFired(Vector3 origin, Vector3 direction, HeldWeaponType weapon)
    {
        if (disposed || owner == null) return;

        Vector3 groundPoint = GetGroundPoint(origin + direction.normalized * Mathf.Max(radius, 2f));
        pending.Add(new PendingStrike
        {
            groundPoint = groundPoint,
            triggerTime = Time.time + StrikeDelay
        });

        nextStrikeTime = Time.time + interval;
    }

    public void AddStack(int count = 1)
    {
        stacks += count;
        if (stacks <= 0) Dispose();
    }

    public void Update(float dt)
    {
        if (disposed || owner == null) return;

        if (duration > 0f)
        {
            timer -= dt;
            if (timer <= 0f)
            {
                Dispose();
                return;
            }
        }

        if (Time.time >= nextStrikeTime)
        {
            QueueStrike();
            nextStrikeTime = Time.time + interval;
        }

        ProcessPending();
    }

    private void QueueStrike()
    {
        Vector3 groundPoint = GetGroundPoint(owner.transform.position);

        pending.Add(new PendingStrike
        {
            groundPoint = groundPoint,
            triggerTime = Time.time + StrikeDelay
        });
    }

    private void ProcessPending()
    {
        for (int i = pending.Count - 1; i >= 0; i--)
        {
            if (Time.time < pending[i].triggerTime) continue;
            ExecuteStrike(pending[i].groundPoint);
            pending.RemoveAt(i);
        }
    }

    private void ExecuteStrike(Vector3 position)
    {
        float damage = baseDamage * stacks;
        Collider[] hits = Physics.OverlapSphere(position, radius, enemyLayer);
        HashSet<Enemy> unique = new HashSet<Enemy>();

        foreach (var col in hits)
        {
            var enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null || unique.Contains(enemy)) continue;
            unique.Add(enemy);
            LightningCore.ApplyLightningDamage(owner, enemy, damage);
        }

        var playerDamageable = owner.GetComponent<IDamageable>();
        bool playerHit = IsPlayerHit(position);
        if (playerDamageable != null && !playerDamageable.IsDead && playerHit)
        {
            playerDamageable.TakeDamage(damage);
            TriggerChainFromPlayer(damage * ChainOnPlayerHitDamagePercent);
        }

        CreateStrikeVfx(position);
        ElectrifyPools(position);
    }

    private void ElectrifyPools(Vector3 position)
    {
        if (electrifyDamage <= 0f) return;

        Collider[] hits = Physics.OverlapSphere(position, ElectrifyPoolSearchRadius);
        HashSet<PoisonPool> unique = new HashSet<PoisonPool>();

        foreach (var col in hits)
        {
            var pool = col.GetComponentInParent<PoisonPool>();
            if (pool == null || unique.Contains(pool)) continue;
            unique.Add(pool);
            pool.Electrify(electrifyDamage);
        }
    }

    private Vector3 GetGroundPoint(Vector3 origin)
    {
        Collider[] ownerCols = owner != null ? owner.GetComponentsInChildren<Collider>() : null;
        Vector3 start = origin + Vector3.up * 2f;
        Vector3 end = origin + Vector3.down * 5f;
        Vector3 dir = (end - start).normalized;
        float distance = Vector3.Distance(start, end);

        RaycastHit[] hits = Physics.RaycastAll(start, dir, distance, ~0, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0) return GetFallbackPoint(origin);

        RaycastHit best = hits[0];
        float bestDist = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            if (IsOwnerCollider(hits[i].collider, ownerCols)) continue;
            if (hits[i].normal.y < 0.2f) continue;
            if (hits[i].distance < bestDist)
            {
                bestDist = hits[i].distance;
                best = hits[i];
            }
        }

        if (bestDist == float.MaxValue) return GetFallbackPoint(origin);
        return best.point + Vector3.up * 0.02f;
    }

    private bool IsOwnerCollider(Collider col, Collider[] ownerCols)
    {
        if (col == null || ownerCols == null) return false;
        for (int i = 0; i < ownerCols.Length; i++)
        {
            if (ownerCols[i] == col) return true;
        }
        return false;
    }

    private Vector3 GetFallbackPoint(Vector3 origin)
    {
        var controller = owner.GetComponent<CharacterController>();
        if (controller != null)
        {
            Vector3 p = origin;
            p.y = controller.bounds.min.y + 0.02f;
            return p;
        }
        origin.y -= 0.1f;
        return origin;
    }

    private void CreateStrikeVfx(Vector3 position)
    {
        if (strikeVFX != null)
        {
            var instance = UnityEngine.Object.Instantiate(strikeVFX, position, strikeVFX.transform.rotation);
            UnityEngine.Object.Destroy(instance, StrikeVfxDuration + 0.1f);
            return;
        }

        Vector3 startPos = position + Vector3.up * StrikeHeight;
        Vector3 endPos = position;

        GameObject startObj = new GameObject("LightningStart");
        startObj.transform.position = startPos;

        GameObject endObj = new GameObject("LightningEnd");
        endObj.transform.position = endPos;

        LightningCore.CreateLightningVFX(
            startObj.transform,
            endObj.transform,
            StrikeHeight,
            0.3f,
            null,
            0f,
            0f,
            0.15f
        );

        UnityEngine.Object.Destroy(startObj, StrikeVfxDuration + 0.1f);
        UnityEngine.Object.Destroy(endObj, StrikeVfxDuration + 0.1f);
    }

    private bool IsPlayerHit(Vector3 strikePosition)
    {
        if (owner == null) return false;
        float maxDist = radius + 0.05f;
        Vector3 playerPos = owner.transform.position;
        Vector2 delta = new Vector2(playerPos.x - strikePosition.x, playerPos.z - strikePosition.z);
        return delta.sqrMagnitude <= maxDist * maxDist;
    }

    private void TriggerChainFromPlayer(float chainDamage)
    {
        if (chainDamage <= 0f || owner == null) return;

        float chainRange = ChainLightning.DefaultRange;
        HashSet<Enemy> hit = new HashSet<Enemy>();
        Enemy first = FindClosestEnemy(owner.transform.position, chainRange, hit);
        if (first == null) return;

        hit.Add(first);
        LightningCore.ApplyLightningDamage(owner, first, chainDamage);
        LightningCore.CreateLightningVFX(owner.transform, first.transform, chainRange, 0.2f, null, 0.5f, 0.5f, 0.18f);

        ChainFromEnemy(first, first.transform.position, chainDamage, 0, chainRange, hit);
    }

    private void ChainFromEnemy(Enemy from, Vector3 fromPos, float damage, int chainNum, float chainRange, HashSet<Enemy> hit)
    {
        if (from == null || chainNum >= ChainOnPlayerHitMaxCount) return;

        Enemy closest = FindClosestEnemy(fromPos, chainRange, hit);
        if (closest == null) return;

        hit.Add(closest);
        LightningCore.ApplyLightningDamage(owner, closest, damage);
        LightningCore.CreateLightningVFX(from.transform, closest.transform, chainRange, 0.2f, null, 0.5f, 0.5f, 0.18f);

        ChainFromEnemy(closest, closest.transform.position, damage, chainNum + 1, chainRange, hit);
    }

    private Enemy FindClosestEnemy(Vector3 fromPos, float chainRange, HashSet<Enemy> hit)
    {
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

        return closest;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        PlayerShooter.OnProjectileFired -= OnProjectileFired;
        pending.Clear();
    }
}
