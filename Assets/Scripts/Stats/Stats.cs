using UnityEngine;

public enum StatType {ProjectileDamage, Health, MoveSpeed, MeleeDamage, SlamDamage}

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

    public float MoveSpeed
    {
        get
        {
            var q = new Query(StatType.MoveSpeed, baseStats.moveSpeed);
            mediator.PerformQuery(this, q);
            return q.Value;
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
            _ => 0f,
        };
    }

    public override string ToString()
    {
        return $"Health: {Health}, MoveSpeed: {MoveSpeed:F1}, Projectile Damage: {ProjectileAttack}, Melee Damage: {MeleeDamage}, Slam Damage: {SlamDamage}";
    }
}
