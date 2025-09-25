using UnityEngine;

public enum StatType {Attack, Health}

public class Stats
{
    readonly StatsMediator mediator;
    readonly BaseStats baseStats;


    public int Attack
    {
        get
        {
            // return value with modifiers applied
            var q = new Query(StatType.Attack, baseStats.projectileDamage);
            mediator.PerformQuery(this, q);
            return q.Value;

        }
    }

    public int Health
    {
        get
        {
            // return value with modifiers appleid
            var q = new Query(StatType.Health, baseStats.health);
            mediator.PerformQuery(this, q);
            return q.Value;
        }
    }

    public Stats(StatsMediator mediator, BaseStats baseStats)
    {
        this.mediator = mediator;
        this.baseStats = baseStats;
    }

    public override string ToString()
    {
        return $"Projectile Damage: {Attack}, Health: {Health}";
    }
}
