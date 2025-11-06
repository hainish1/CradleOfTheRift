using System;
using UnityEngine;

public enum StatType
{
    // Attack Enums
    ProjectileDamage,
    MeleeDamage,
    SlamDamage,
    SlamRadius,
    ProjectileSpread,
    HomingProjectiles
}

public class Stats
{
    readonly StatsMediator mediator;
    readonly BaseStats baseStats;

    public StatsMediator Mediator => mediator;

    // Attack Properties
    
    public float ProjectileDamage { get { return FloatStatQuery(StatType.ProjectileDamage, baseStats.projectileDamage); } }

    public float MeleeDamage { get { return FloatStatQuery(StatType.MeleeDamage, baseStats.meleeDamage); } }

    public float SlamDamage { get { return FloatStatQuery(StatType.SlamDamage, baseStats.slamDamage); } }

    public float SlamRadius { get { return FloatStatQuery(StatType.SlamRadius, baseStats.slamRadius); } }

    public float AttackSpeed { get { return FloatStatQuery(StatType.AttackSpeed, baseStats.attackSpeed); } }

    public float ProjectileSpread { get { return FloatStatQuery(StatType.ProjectileSpread, baseStats.projectileSpread); } }

    // Health Properties
    
    public float Health { get { return FloatStatQuery(StatType.Health, baseStats.health); } }

    // Movement Properties
    
    public float MoveSpeed { get { return FloatStatQuery(StatType.MoveSpeed, baseStats.moveSpeed); } }

    // Knockback Properties

    public float KbDamping { get { return FloatStatQuery(StatType.KbDamping, baseStats.kbDamping); } }

    public float KbControlsLockTime { get { return FloatStatQuery(StatType.KbControlsLockTime, baseStats.kbControlsLockTime); } }

    public float KbDashLockTime { get { return FloatStatQuery(StatType.KbDashLockTime, baseStats.kbDashLockTime); } }

    // Dash Properties

    public float DashDistance { get { return FloatStatQuery(StatType.DashDistance, baseStats.dashDistance); } }

    public int HomingProjectiles
    {
        get
        {
            var q = new Query(StatType.HomingProjectiles, baseStats.enableHomingProjectiles);
            mediator.PerformQuery(this, q);
            return Mathf.CeilToInt(q.Value);
        }
    }

    public Stats(StatsMediator mediator, BaseStats baseStats)
    {
        this.mediator = mediator;
        this.baseStats = baseStats;
    }

    /// <summary>
    ///   <para>
    ///     Gets the base value for the provided stat type.
    ///   </para>
    /// </summary>
    /// <param name="type"> The stat type. </param>
    /// <returns> The base stat value. </returns>
    public float BaseValueForStat(StatType type)
    {
        return type switch
        {
            StatType.ProjectileDamage => baseStats.projectileDamage,
            StatType.MeleeDamage => baseStats.meleeDamage,
            StatType.SlamDamage => baseStats.slamDamage,
            StatType.SlamRadius => baseStats.slamRadius,
            StatType.AttackSpeed => baseStats.attackSpeed,
            StatType.Health => baseStats.health,
            StatType.MoveSpeed => baseStats.moveSpeed,
            StatType.KbDamping => baseStats.kbDamping,
            StatType.KbControlsLockTime => baseStats.kbControlsLockTime,
            StatType.KbDashLockTime => baseStats.kbDashLockTime,
            StatType.DashDistance => baseStats.dashDistance,
            StatType.DashSpeed => baseStats.dashSpeed,
            StatType.DashCooldown => baseStats.dashCooldown,
            StatType.DashCharges => baseStats.dashCharges,
            StatType.AttackSpeed => baseStats.attackSpeed,
            StatType.HomingProjectiles => baseStats.enableHomingProjectiles,
            _ => 0f,
        };
    }

    public override string ToString()
    {
        return $"Health: {Health}, MoveSpeed: {MoveSpeed:F1}, Projectile Damage: {ProjectileDamage}, Melee Damage: {MeleeDamage}, Slam Damage: {SlamDamage}, Attack Speed: {AttackSpeed}";
    }

    /// <summary>
    ///   <para>
    ///     Queries for float stat values.
    ///   </para>
    /// </summary>
    /// <param name="statType"> The stat type. </param>
    /// <param name="value"> The current stat value. </param>
    /// <returns> A float value. </returns>
    private float FloatStatQuery(StatType statType, float value)
    {
        // return value with modifiers applied
        var q = new Query(statType, value);
        mediator.PerformQuery(this, q);
        return q.Value;
    }

    /// <summary>
    ///   <para>
    ///     Queries for int stat values.
    ///   </para>
    /// </summary>
    /// <param name="statType"> The stat type. </param>
    /// <param name="value"> The current stat value. </param>
    /// <returns> An int value. </returns>
    private int IntStatQuery(StatType statType, float value)
    {
        var q = new Query(statType, value);
        mediator.PerformQuery(this, q);
        return Mathf.CeilToInt(q.Value);
    }
}
