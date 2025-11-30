using System;
using UnityEngine;

public enum StatType
{
    // Melee Attack Enums
    MeleeDamage,

    // Ranged Attack Enums
    ProjectileDamage,
    AttackSpeed,
    ProjectileFireRate,
    FireCharges,
    FireChargeCooldown,
    ProjectileSpread,
    HomingProjectiles,

    // Shockwave Enums
    SlamDamage,
    SlamRadius,

    // Health Enums
    Health,

    // Movement Enums
    MoveSpeed,

    // Knockback Enums
    KbDamping,
    KbControlsLockTime,
    KbDashLockTime,

    // Dash Enums
    DashDistance,
    DashSpeed,
    DashCooldown,
    DashCharges,

    // Jump Enums
    JumpForce,

    // Drift Enums
    DriftDescentDivisor,

    // Flight Enums
    FlightMaxSpeed,
    FlightMaxEnergy,
    FlightRegenerationRate,
    FlightDepletionRate,

    // Character Enums
    CharacterSize
}

public class Stats
{
    readonly StatsMediator mediator;
    readonly BaseStats baseStats;

    public StatsMediator Mediator => mediator;

    // Melee Attack Properties
    
    public float MeleeDamage { get { return FloatStatQuery(StatType.MeleeDamage, baseStats.meleeDamage); } }

    // Ranged Attack Properties
    
    public float ProjectileDamage { get { return FloatStatQuery(StatType.ProjectileDamage, baseStats.projectileDamage); } }
    
    public float AttackSpeed { get { return FloatStatQuery(StatType.AttackSpeed, baseStats.attackSpeed); } }

    public float ProjectileFireRate { get { return FloatStatQuery(StatType.ProjectileFireRate, baseStats.projectileFireRate); } }

    public int FireCharges { get { return IntStatQuery(StatType.FireCharges, baseStats.fireCharges); } }

    public float FireChargeCooldown { get { return FloatStatQuery(StatType.FireChargeCooldown, baseStats.fireChargeCooldown); } }

    public float ProjectileSpread { get { return FloatStatQuery(StatType.ProjectileSpread, baseStats.projectileSpread); } }

    public int HomingProjectiles { get { return IntStatQuery(StatType.HomingProjectiles, baseStats.enableHomingProjectiles); } }

    // Shockwave Properties
    public float SlamDamage { get { return FloatStatQuery(StatType.SlamDamage, baseStats.slamDamage); } }

    public float SlamRadius { get { return FloatStatQuery(StatType.SlamRadius, baseStats.slamRadius); } }

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

    public float DashSpeed { get { return FloatStatQuery(StatType.DashSpeed, baseStats.dashSpeed); } }

    public float DashCooldown { get { return FloatStatQuery(StatType.DashCooldown, baseStats.dashCooldown); } }

    public int DashCharges { get { return IntStatQuery(StatType.DashCharges, baseStats.dashCharges); } }

    // Jump Properties

    public float JumpForce { get { return FloatStatQuery(StatType.JumpForce, baseStats.jumpForce); } }

    // Drift Properties

    public float DriftDescentDivisor { get { return FloatStatQuery(StatType.DriftDescentDivisor, baseStats.driftDescentDivisor); } }

    // Flight Properties

    public float FlightMaxSpeed { get { return FloatStatQuery(StatType.FlightMaxSpeed, baseStats.flightMaxSpeed); } }

    public int FlightMaxEnergy { get { return IntStatQuery(StatType.FlightMaxEnergy, baseStats.flightMaxEnergy); } }

    public float FlightRegenerationRate { get { return FloatStatQuery(StatType.FlightRegenerationRate, baseStats.flightRegenerationRate); } }

    public float FlightDepletionRate { get { return FloatStatQuery(StatType.FlightDepletionRate, baseStats.flightDepletionRate); } }

    public float CharacterSize { get { return FloatStatQuery(StatType.CharacterSize, baseStats.characterSize); } }

    /// <summary>
    ///   <para>
    ///     Constructs a stats object.
    ///   </para>
    /// </summary>
    /// <param name="mediator"> The StatsMediator reference. </param>
    /// <param name="baseStats"> The BaseStats reference. </param>
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
            StatType.MeleeDamage => baseStats.meleeDamage,
            StatType.ProjectileDamage => baseStats.projectileDamage,
            StatType.AttackSpeed => baseStats.attackSpeed,
            StatType.ProjectileFireRate => baseStats.projectileFireRate,
            StatType.FireCharges => baseStats.fireCharges,
            StatType.FireChargeCooldown => baseStats.fireChargeCooldown,
            StatType.ProjectileSpread => baseStats.projectileSpread,
            StatType.HomingProjectiles => baseStats.enableHomingProjectiles,
            StatType.SlamDamage => baseStats.slamDamage,
            StatType.SlamRadius => baseStats.slamRadius,
            StatType.Health => baseStats.health,
            StatType.MoveSpeed => baseStats.moveSpeed,
            StatType.KbDamping => baseStats.kbDamping,
            StatType.KbControlsLockTime => baseStats.kbControlsLockTime,
            StatType.KbDashLockTime => baseStats.kbDashLockTime,
            StatType.DashDistance => baseStats.dashDistance,
            StatType.DashSpeed => baseStats.dashSpeed,
            StatType.DashCooldown => baseStats.dashCooldown,
            StatType.DashCharges => baseStats.dashCharges,
            StatType.JumpForce => baseStats.jumpForce,
            StatType.DriftDescentDivisor => baseStats.driftDescentDivisor,
            StatType.FlightMaxSpeed => baseStats.flightMaxSpeed,
            StatType.FlightRegenerationRate => baseStats.flightRegenerationRate,
            StatType.FlightMaxEnergy => baseStats.flightMaxEnergy,
            StatType.FlightDepletionRate => baseStats.flightDepletionRate,
            StatType.CharacterSize => baseStats.characterSize,
            _ => 0f,
        };
    }

    public override string ToString()
    {
        return $"Health: {Health}, MoveSpeed: {MoveSpeed:F1}, AttackSpeed: {AttackSpeed}, FireChargeCooldown: {FireChargeCooldown} ,Projectile Damage: {ProjectileDamage}, Melee Damage: {MeleeDamage}, Slam Damage: {SlamDamage}";
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
