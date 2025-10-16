using System;
using UnityEngine;

public enum StatType
{
    ProjectileDamage,
    Health,
    MoveSpeed,
    JumpHeight,
    DashSpeed,
    DashDistance,
    DashCooldown,
    DashCharges,
    MeleeDamage,
    SlamDamage,
    SlamRadius
}

public class Stats
{
    readonly StatsMediator mediator;
    readonly BaseStats baseStats;

    public StatsMediator Mediator => mediator;

    public float ProjectileAttack
    {
        get
        {
            // return value with modifiers applied
            var q = new Query(StatType.ProjectileDamage, baseStats.projectileDamage);
            mediator.PerformQuery(this, q);
            return q.Value;

        }
    }

    public float Health
    {
        get
        {
            // return value with modifiers appleid
            var q = new Query(StatType.Health, baseStats.health);
            mediator.PerformQuery(this, q);
            return q.Value;
        }
    }

    public float JumpHeight
    {
        get
        {
            var q = new Query(StatType.JumpHeight, baseStats.jumpHeight);
            mediator.PerformQuery(this, q);
            return q.Value;
        }
    }

    public float MoveSpeed
    {
        get
        {
            var q = new Query(StatType.MoveSpeed, baseStats.moveSpeed);
            mediator.PerformQuery(this, q);
            return q.Value;
        }
    }

    public float DashSpeed
    {
        get
        {
            var q = new Query(StatType.DashSpeed, baseStats.dashSpeed);
            mediator.PerformQuery(this, q);
            return q.Value;
        }
    }

    public float DashDistance
    {
        get
        {
            var q = new Query(StatType.DashDistance, baseStats.dashDistance);
            mediator.PerformQuery(this, q);
            return q.Value;
        }
    }

    public float DashCooldown
    {
        get
        {
            var q = new Query(StatType.DashCooldown, baseStats.dashCooldown);
            mediator.PerformQuery(this, q);
            return q.Value;
        }
    }

    public int DashCharges
    {
        get
        {
            var q = new Query(StatType.DashCharges, baseStats.dashCharges);
            mediator.PerformQuery(this, q);
            return Mathf.CeilToInt(q.Value);
        }
    }

    public float MeleeDamage
    {
        get
        {
            var q = new Query(StatType.MeleeDamage, baseStats.meleeDamage);
            mediator.PerformQuery(this, q);
            return q.Value;
        }
    }

    public float SlamDamage
    {
        get
        {
            var q = new Query(StatType.SlamDamage, baseStats.slamDamage);
            mediator.PerformQuery(this, q);
            return q.Value;
        }
    }

    public float SlamRadius
    {
        get
        {
            var q = new Query(StatType.SlamRadius, baseStats.slamAttackRadius);
            mediator.PerformQuery(this, q);
            return q.Value;
        }
    }

    public Stats(StatsMediator mediator, BaseStats baseStats)
    {
        this.mediator = mediator;
        this.baseStats = baseStats;
    }

    public float BaseValueForStat(StatType type)
    {
        return type switch
        {
            StatType.Health => baseStats.health,
            StatType.ProjectileDamage => baseStats.projectileDamage,
            StatType.MoveSpeed => baseStats.moveSpeed,
            StatType.JumpHeight => baseStats.jumpHeight,
            StatType.MeleeDamage => baseStats.meleeDamage,
            StatType.SlamDamage => baseStats.slamDamage,
            StatType.SlamRadius => baseStats.slamAttackRadius,
            StatType.DashSpeed => baseStats.dashSpeed,
            StatType.DashDistance => baseStats.dashDistance,
            StatType.DashCooldown => baseStats.dashCooldown,
            StatType.DashCharges => baseStats.dashCharges,
            _ => 0f,
        };
    }

    public override string ToString()
    {
        return $"Health: {Health}, MoveSpeed: {MoveSpeed:F1}, Projectile Damage: {ProjectileAttack}, Melee Damage: {MeleeDamage}, Slam Damage: {SlamDamage}";
    }
}
