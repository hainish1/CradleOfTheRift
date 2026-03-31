using System.Collections.Generic;
using UnityEngine;

public class GloomPool : MonoBehaviour
{
    private Entity owner;
    private float damage;
    private float tickInterval;
    private float radius;
    private float lifetime;
    private float attackSpeedBuff;

    private float nextTickTime;
    private float endTime;
    private bool initialized;
    private LayerMask enemyLayerMask;

    private Entity buffedPlayer;
    private StatModifier attackSpeedModifier;

    public void Initialize(Entity owner, float damage, float tickInterval, float radius, float lifetime, float attackSpeedBuff)
    {
        this.owner = owner;
        this.damage = damage;
        this.tickInterval = Mathf.Max(0.1f, tickInterval);
        this.radius = radius;
        this.lifetime = lifetime;
        this.attackSpeedBuff = attackSpeedBuff;

        enemyLayerMask = LayerMask.GetMask("Enemy");
        nextTickTime = Time.time + this.tickInterval;
        endTime = Time.time + lifetime;
        initialized = true;

        var trigger = GetComponent<SphereCollider>();
        if (trigger == null) trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = radius;
        trigger.center = Vector3.zero;
    }

    private void Update()
    {
        if (!initialized) return;

        if (Time.time >= endTime)
        {
            Destroy(gameObject);
            return;
        }

        if (Time.time >= nextTickTime)
        {
            ApplyPoisonDamage();
            nextTickTime = Time.time + tickInterval;
        }
    }

    private void ApplyPoisonDamage()
    {
        if (owner == null) return;

        Vector3 center = transform.position;
        Collider[] hits = Physics.OverlapSphere(center, radius, enemyLayerMask);
        HashSet<Enemy> unique = new HashSet<Enemy>();

        foreach (var col in hits)
        {
            var enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null || unique.Contains(enemy)) continue;
            unique.Add(enemy);

            var damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                damageable.TakeDamage(damage);
                CombatEvents.ReportDamage(owner, enemy, damage, ElementType.Poison);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!initialized || attackSpeedBuff <= 0f) return;
        if (!other.CompareTag("Player")) return;

        if (buffedPlayer == null)
        {
            buffedPlayer = other.GetComponentInParent<Entity>();
            if (buffedPlayer != null && buffedPlayer.Stats != null)
            {
                float mult = 1f + attackSpeedBuff;
                attackSpeedModifier = new BasicStatsModifier(
                    StatType.MeleeAnimationSpeed, -1f, v => v * mult);
                buffedPlayer.Stats.Mediator.AddModifier(attackSpeedModifier);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        RemoveBuff();
    }

    private void RemoveBuff()
    {
        if (attackSpeedModifier != null)
        {
            attackSpeedModifier.Dispose();
            attackSpeedModifier = null;
        }
        buffedPlayer = null;
    }

    private void OnDestroy()
    {
        RemoveBuff();
    }
}
