using System;
using UnityEngine;

public class GloomUpgrade : IDisposable
{
    private bool disposed;
    private StatModifier cooldownModifier;
    private Entity owner;

    public bool IsDisposed => disposed;

    public static bool IsEnabled { get; private set; }
    public static Entity Owner { get; private set; }
    public static GameObject PoolPrefab { get; private set; }
    public static float Damage { get; private set; }
    public static float TickInterval { get; private set; }
    public static float Radius { get; private set; }
    public static float PoolLifetime { get; private set; }
    public static float AttackSpeedBuff { get; private set; }

    public GloomUpgrade(Entity owner, float damage, float tickInterval, float radius,
        float poolLifetime, float attackSpeedBuff, float fireCooldownIncrease,
        GameObject poolPrefab)
    {
        this.owner = owner;

        Owner = owner;
        Damage = damage;
        TickInterval = tickInterval;
        Radius = radius;
        PoolLifetime = poolLifetime;
        AttackSpeedBuff = attackSpeedBuff;
        PoolPrefab = poolPrefab;
        IsEnabled = true;

        if (fireCooldownIncrease > 0f && owner.Stats != null)
        {
            cooldownModifier = new BasicStatsModifier(
                StatType.FireChargeCooldown, -1f, v => v + fireCooldownIncrease);
            owner.Stats.Mediator.AddModifier(cooldownModifier);
        }

        Debug.Log($"[GloomUpgrade] Activated! {damage} poison damage, {radius}m radius, {poolLifetime}s lifetime, +{attackSpeedBuff * 100}% attack speed buff");
    }

    public void Update(float dt) { }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        if (cooldownModifier != null)
        {
            cooldownModifier.Dispose();
            cooldownModifier = null;
        }

        IsEnabled = false;
        Owner = null;
        PoolPrefab = null;
        Damage = 0f;
        TickInterval = 0f;
        Radius = 0f;
        PoolLifetime = 0f;
        AttackSpeedBuff = 0f;

        Debug.Log("[GloomUpgrade] Disposed");
    }
}
