using System;
using System.Collections.Generic;
using UnityEngine;

public class LightningStrikeBase : IDisposable
{
    private readonly Entity owner;
    private readonly float baseDamage;
    private readonly float radius;
    private readonly float interval;
    private readonly GameObject strikeVFX;
    private readonly LayerMask enemyLayer;
    private readonly Collider[] overlapBuffer = new Collider[32];
    private readonly HashSet<Enemy> hitBuffer = new HashSet<Enemy>();

    private float bonusDamage;
    private float cooldownReduction;
    private int bonusStrikes;
    private float spreadRadius = 8f;
    private bool selfHeal;
    private float nextStrikeTime;
    private bool disposed;

    private const float MinInterval = 0.5f;

    private const float StrikeDelay = 0.5f;
    private const float StrikeHeight = 20f;
    private const float StrikeVfxDuration = 0.25f;

    private struct PendingStrike
    {
        public Vector3 groundPoint;
        public float triggerTime;
    }

    private readonly List<PendingStrike> pending = new List<PendingStrike>();

    private GameObject strikeStartObj;
    private GameObject strikeEndObj;

    public LightningStrikeBase(Entity owner, float damage, float radius, float interval, GameObject strikeVFX = null)
    {
        this.owner = owner;
        this.baseDamage = damage;
        this.radius = radius;
        this.interval = Mathf.Max(0.1f, interval);
        this.strikeVFX = strikeVFX;
        this.nextStrikeTime = Time.time + this.interval;
        enemyLayer = LayerMask.GetMask("Enemy");

        if (strikeVFX == null)
        {
            strikeStartObj = new GameObject("LStrikeStart");
            strikeEndObj = new GameObject("LStrikeEnd");
            strikeStartObj.SetActive(false);
            strikeEndObj.SetActive(false);
        }
    }

    public bool IsDisposed => disposed;

    public void AddDamageBonus(float amount) => bonusDamage += amount;
    public void RemoveDamageBonus(float amount) => bonusDamage -= amount;
    public void AddCooldownReduction(float amount) => cooldownReduction += amount;
    public void RemoveCooldownReduction(float amount) => cooldownReduction -= amount;
    public void AddBonusStrikes(int count) => bonusStrikes += count;
    public void RemoveBonusStrikes(int count) => bonusStrikes = Mathf.Max(0, bonusStrikes - count);
    public void SetSpreadRadius(float r) { if (r > 0f) spreadRadius = r; }
    public void SetSelfHeal(bool value) => selfHeal = value;

    private float EffectiveInterval => Mathf.Max(MinInterval, interval - cooldownReduction);

    public void Update(float dt)
    {
        if (disposed || owner == null) return;

        if (Time.time >= nextStrikeTime)
        {
            QueueStrike();
            nextStrikeTime = Time.time + EffectiveInterval;
        }

        ProcessPending();
    }

    private void QueueStrike()
    {
        Vector3 playerPos = owner.transform.position;
        float triggerTime = Time.time + StrikeDelay;

        Vector3 groundPoint = GetGroundPoint(playerPos);
        CreateWarningVfx(groundPoint);
        pending.Add(new PendingStrike { groundPoint = groundPoint, triggerTime = triggerTime });

        for (int i = 0; i < bonusStrikes; i++)
        {
            float bonusTriggerTime = triggerTime + UnityEngine.Random.Range(0.1f, 0.45f);
            Vector2 offset = UnityEngine.Random.insideUnitCircle * spreadRadius;
            Vector3 randomPos = playerPos + new Vector3(offset.x, 0f, offset.y);
            Vector3 randomGround = GetGroundPoint(randomPos);
            CreateWarningVfx(randomGround);
            pending.Add(new PendingStrike { groundPoint = randomGround, triggerTime = bonusTriggerTime });
        }
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
        float damage = baseDamage + bonusDamage;

        hitBuffer.Clear();
        int hitCount = Physics.OverlapSphereNonAlloc(position, radius, overlapBuffer, enemyLayer);
        for (int i = 0; i < hitCount; i++)
        {
            var enemy = overlapBuffer[i].GetComponentInParent<Enemy>();
            if (enemy == null || hitBuffer.Contains(enemy)) continue;
            hitBuffer.Add(enemy);
            LightningCore.ApplyLightningDamage(owner, enemy, damage);
        }

        bool playerHit = IsPlayerHit(position);
        if (playerHit)
        {
            if (selfHeal)
            {
                var health = owner.GetComponent<HealthController>();
                if (health != null && !health.IsDead)
                    health.Heal(damage);
            }
            else
            {
                var playerDamageable = owner.GetComponent<IDamageable>();
                if (playerDamageable != null && !playerDamageable.IsDead)
                    playerDamageable.TakeDamage(damage);
            }
        }

        CreateStrikeVfx(position);
        LightningStrikeEvents.FireStrikeLanded(owner, position, damage);

        if (playerHit)
            LightningStrikeEvents.FirePlayerSelfHit(owner, damage);
    }

    private bool IsPlayerHit(Vector3 strikePosition)
    {
        if (owner == null) return false;
        float maxDist = radius + 0.05f;
        Vector3 playerPos = owner.transform.position;
        Vector2 delta = new Vector2(playerPos.x - strikePosition.x, playerPos.z - strikePosition.z);
        return delta.sqrMagnitude <= maxDist * maxDist;
    }

    private void CreateWarningVfx(Vector3 groundPoint)
    {
        Vector3 pos = groundPoint + Vector3.up * StrikeHeight;
        var warning = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        warning.name = "LightningWarning";
        warning.transform.position = pos;
        warning.transform.localScale = Vector3.one * 0.4f;

        var col = warning.GetComponent<Collider>();
        if (col != null) UnityEngine.Object.Destroy(col);

        var renderer = warning.GetComponent<Renderer>();
        if (renderer != null)
        {
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(0.6f, 0.9f, 1f, 1f);
            renderer.material = mat;
        }

        UnityEngine.Object.Destroy(warning, StrikeDelay + 0.1f);
    }

    private void CreateStrikeVfx(Vector3 position)
    {
        if (strikeVFX != null)
        {
            var instance = UnityEngine.Object.Instantiate(strikeVFX, position, strikeVFX.transform.rotation);
            UnityEngine.Object.Destroy(instance, StrikeVfxDuration + 0.1f);
            return;
        }

        if (strikeStartObj == null || strikeEndObj == null) return;

        strikeStartObj.transform.position = position + Vector3.up * StrikeHeight;
        strikeEndObj.transform.position = position;
        strikeStartObj.SetActive(true);
        strikeEndObj.SetActive(true);

        LightningCore.CreateLightningVFX(
            strikeStartObj.transform,
            strikeEndObj.transform,
            StrikeHeight,
            0.3f,
            null,
            0f,
            0f,
            0.1f
        );

        strikeStartObj.SetActive(false);
        strikeEndObj.SetActive(false);
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

        float bestDist = float.MaxValue;
        RaycastHit best = hits[0];
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

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        pending.Clear();
        if (strikeStartObj != null) UnityEngine.Object.Destroy(strikeStartObj);
        if (strikeEndObj != null) UnityEngine.Object.Destroy(strikeEndObj);
    }
}
