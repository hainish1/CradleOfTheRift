using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLightningStrike : IDisposable
{
    private readonly Entity owner;
    private readonly float baseDamage;
    private readonly float radius;
    private readonly float interval;
    private readonly float delay;
    private readonly float height;
    private readonly float vfxDuration;
    private readonly float electrifyDuration;
    private readonly float electrifyDamage;
    private readonly LayerMask enemyLayer;
    private int stacks;
    private float duration;
    private float timer;
    private float nextStrikeTime;
    private bool disposed;

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
        float delay,
        float height,
        float vfxDuration,
        float electrifyDuration,
        float electrifyDamage,
        int initialStacks = 1,
        float durationSec = -1f)
    {
        this.owner = owner;
        this.baseDamage = damage;
        this.radius = radius;
        this.interval = Mathf.Max(0.1f, interval);
        this.delay = Mathf.Max(0f, delay);
        this.height = Mathf.Max(1f, height);
        this.vfxDuration = Mathf.Max(0.05f, vfxDuration);
        this.electrifyDuration = Mathf.Max(0.1f, electrifyDuration);
        this.electrifyDamage = Mathf.Max(0f, electrifyDamage);
        this.stacks = initialStacks > 0 ? initialStacks : 1;
        this.duration = durationSec;
        this.timer = durationSec;
        this.nextStrikeTime = Time.time + this.interval;

        enemyLayer = LayerMask.GetMask("Enemy");
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
        CreateWarningVfx(groundPoint);

        pending.Add(new PendingStrike
        {
            groundPoint = groundPoint,
            triggerTime = Time.time + delay
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
        if (playerDamageable != null && !playerDamageable.IsDead)
        {
            playerDamageable.TakeDamage(damage);
        }

        CreateStrikeVfx(position);
        ElectrifyPools(position);
    }

    private void ElectrifyPools(Vector3 position)
    {
        if (electrifyDamage <= 0f && electrifyDuration <= 0f) return;

        Collider[] hits = Physics.OverlapSphere(position, radius);
        HashSet<PoisonPool> unique = new HashSet<PoisonPool>();

        foreach (var col in hits)
        {
            var pool = col.GetComponentInParent<PoisonPool>();
            if (pool == null || unique.Contains(pool)) continue;
            unique.Add(pool);
            pool.Electrify(electrifyDuration, electrifyDamage);
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

    private void CreateWarningVfx(Vector3 groundPoint)
    {
        Vector3 startPos = groundPoint + Vector3.up * height;
        GameObject warningObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        warningObj.name = "LightningWarning";
        warningObj.transform.position = startPos;
        warningObj.transform.localScale = Vector3.one * 0.4f;

        var warningCollider = warningObj.GetComponent<Collider>();
        if (warningCollider != null) UnityEngine.Object.Destroy(warningCollider);

        var renderer = warningObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = LightningCore.CalculateLightningColor();
            renderer.material = mat;
        }

        UnityEngine.Object.Destroy(warningObj, delay);
    }

    private void CreateStrikeVfx(Vector3 position)
    {
        Vector3 startPos = position + Vector3.up * height;
        Vector3 endPos = position;

        GameObject startObj = new GameObject("LightningStart");
        startObj.transform.position = startPos;

        GameObject endObj = new GameObject("LightningEnd");
        endObj.transform.position = endPos;

        LightningCore.CreateLightningVFX(
            startObj.transform,
            endObj.transform,
            height,
            vfxDuration,
            null,
            0f,
            0f,
            0.15f
        );

        UnityEngine.Object.Destroy(startObj, vfxDuration + 0.1f);
        UnityEngine.Object.Destroy(endObj, vfxDuration + 0.1f);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        pending.Clear();
    }
}
