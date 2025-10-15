using UnityEngine;

public enum StatType {ProjectileDamage, Health, MoveSpeed, DashCharges, JumpHeight, DashCooldown }

public class Stats
{
    readonly StatsMediator mediator;
    readonly BaseStats baseStats;

    public StatsMediator Mediator => mediator;

    public float Attack
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

    public float DashCharges
    {
        get
        {
            var q = new Query(StatType.DashCharges, baseStats.dashCharges);
            mediator.PerformQuery(this, q);
            return q.Value;
        }
    }
    public float JumpHeight
    {
        get
        {
            var q = new Query(StatType.JumpHeight, baseStats.JumpHeight);
            mediator.PerformQuery(this, q);
            return q.Value;
        }
    }
    public float DashCooldown
    {
        get
        {
            var q = new Query(StatType.DashCooldown, baseStats.DashCooldown);
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
        return $"Health: {Health}, MoveSpeed: {MoveSpeed:F1}, Projectile Damage: {Attack},DashCharges: {DashCharges},JumpHeight: {JumpHeight},DashCooldown: {DashCooldown}";
    }
}
