// <summary>
//   <authors>
//     Hainish Acharya, Samuel Rigby
//   </authors>
//   <para>
//     Written by Hainish Acharya for GAMES 4500, University of Utah, September 2025.
//     Contributed to by Samuel Rigby.
//          -Added properties for player movement.
//          -Refactored properties to work with FloatStatQuery and IntStatQuery methods
//           for readability and scalability.
//   </para>
// </summary>

using System;
using UnityEngine;

public enum StatType
{
    // Melee Attack Enums
    MeleeDamage,
    SpearMeleeDamage,
    AxeMeleeDamage,
    MaceMeleeDamage,
    StaffMeleeDamage,
    MeleeAttackRate,
    SpearMeleeAttackRate,
    AxeMeleeAttackRate,
    MaceMeleeAttackRate,
    StaffMeleeAttackRate,
    MeleeAnimationSpeed,

    // Ranged Attack Enums
    ProjectileDamage,
    SpearProjectileDamage,
    AxeProjectileDamage,
    MaceProjectileDamage,
    StaffProjectileDamage,
    ProjectileFireRate,
    SpearProjectileFireRate,
    AxeProjectileFireRate,
    MaceProjectileFireRate,
    StaffProjectileFireRate,
    ProjectileAnimationSpeed,
    FireCharges,
    FireChargeCooldown,
    ProjectileSpread,
    HomingProjectiles,

    // Shockwave Enums
    ShockwaveDamage,
    ShockwaveRadius,
    ShockwaveKnockback,
    ShockwaveCooldown,

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

    public float SpearMeleeDamage => WeaponMeleeDamage(StatType.SpearMeleeDamage, baseStats.spearMeleeDamage);
    public float AxeMeleeDamage => WeaponMeleeDamage(StatType.AxeMeleeDamage, baseStats.axeMeleeDamage);
    public float MaceMeleeDamage => WeaponMeleeDamage(StatType.MaceMeleeDamage, baseStats.maceMeleeDamage);
    public float StaffMeleeDamage => WeaponMeleeDamage(StatType.StaffMeleeDamage, baseStats.staffMeleeDamage);

    private float WeaponMeleeDamage(StatType weaponStat, float weaponBaseValue)
    {
        float weaponBase = weaponBaseValue > 0f ? weaponBaseValue : baseStats.meleeDamage;
        float afterWeaponMods = FloatStatQuery(weaponStat, weaponBase);
        float globalDelta = MeleeDamage - baseStats.meleeDamage;
        return afterWeaponMods + globalDelta;
    }

    public float MeleeDamageForWeapon(HeldWeaponType weapon)
    {
        return weapon switch
        {
            HeldWeaponType.Spear => SpearMeleeDamage,
            HeldWeaponType.Axe => AxeMeleeDamage,
            HeldWeaponType.Mace => MaceMeleeDamage,
            HeldWeaponType.Staff => StaffMeleeDamage,
            _ => MeleeDamage,
        };
    }

    public float MeleeAttackRate { get { return FloatStatQuery(StatType.MeleeAttackRate, baseStats.meleeAttackRate); } }

    public float SpearMeleeAttackRate => WeaponMeleeAttackRate(StatType.SpearMeleeAttackRate, baseStats.spearMeleeAttackRate);
    public float AxeMeleeAttackRate => WeaponMeleeAttackRate(StatType.AxeMeleeAttackRate, baseStats.axeMeleeAttackRate);
    public float MaceMeleeAttackRate => WeaponMeleeAttackRate(StatType.MaceMeleeAttackRate, baseStats.maceMeleeAttackRate);
    public float StaffMeleeAttackRate => WeaponMeleeAttackRate(StatType.StaffMeleeAttackRate, baseStats.staffMeleeAttackRate);

    private float WeaponMeleeAttackRate(StatType weaponStat, float weaponBaseValue)
    {
        float weaponBase = weaponBaseValue > 0f ? weaponBaseValue : baseStats.meleeAttackRate;
        float afterWeaponMods = FloatStatQuery(weaponStat, weaponBase);
        float globalDelta = MeleeAttackRate - baseStats.meleeAttackRate;
        return afterWeaponMods + globalDelta;
    }

    public float MeleeAttackRateForWeapon(HeldWeaponType weapon)
    {
        return weapon switch
        {
            HeldWeaponType.Spear => SpearMeleeAttackRate,
            HeldWeaponType.Axe => AxeMeleeAttackRate,
            HeldWeaponType.Mace => MaceMeleeAttackRate,
            HeldWeaponType.Staff => StaffMeleeAttackRate,
            _ => MeleeAttackRate,
        };
    }

    public float MeleeAnimationSpeed { get { return FloatStatQuery(StatType.MeleeAnimationSpeed, baseStats.meleeAnimationSpeed); } }

    // Ranged Attack Properties

    public float ProjectileDamage { get { return FloatStatQuery(StatType.ProjectileDamage, baseStats.projectileDamage); } }

    public float SpearProjectileDamage => WeaponProjectileDamage(StatType.SpearProjectileDamage, baseStats.spearProjectileDamage);
    public float AxeProjectileDamage => WeaponProjectileDamage(StatType.AxeProjectileDamage, baseStats.axeProjectileDamage);
    public float MaceProjectileDamage => WeaponProjectileDamage(StatType.MaceProjectileDamage, baseStats.maceProjectileDamage);
    public float StaffProjectileDamage => WeaponProjectileDamage(StatType.StaffProjectileDamage, baseStats.staffProjectileDamage);

    // Per-weapon base, then applies weaponspecific modifiers,
    // then global pickups buff every weapon
    private float WeaponProjectileDamage(StatType weaponStat, float weaponBaseValue)
    {
        float weaponBase = weaponBaseValue > 0f ? weaponBaseValue : baseStats.projectileDamage;
        float afterWeaponMods = FloatStatQuery(weaponStat, weaponBase);
        float globalDelta = ProjectileDamage - baseStats.projectileDamage;
        return afterWeaponMods + globalDelta;
    }

    public float ProjectileDamageForWeapon(HeldWeaponType weapon)
    {
        return weapon switch
        {
            HeldWeaponType.Spear => SpearProjectileDamage,
            HeldWeaponType.Axe => AxeProjectileDamage,
            HeldWeaponType.Mace => MaceProjectileDamage,
            HeldWeaponType.Staff => StaffProjectileDamage,
            _ => ProjectileDamage,
        };
    }

    public float ProjectileFireRate { get { return FloatStatQuery(StatType.ProjectileFireRate, baseStats.projectileFireRate); } }

    public float SpearProjectileFireRate => WeaponProjectileFireRate(StatType.SpearProjectileFireRate, baseStats.spearProjectileFireRate);
    public float AxeProjectileFireRate => WeaponProjectileFireRate(StatType.AxeProjectileFireRate, baseStats.axeProjectileFireRate);
    public float MaceProjectileFireRate => WeaponProjectileFireRate(StatType.MaceProjectileFireRate, baseStats.maceProjectileFireRate);
    public float StaffProjectileFireRate => WeaponProjectileFireRate(StatType.StaffProjectileFireRate, baseStats.staffProjectileFireRate);

    private float WeaponProjectileFireRate(StatType weaponStat, float weaponBaseValue)
    {
        float weaponBase = weaponBaseValue > 0f ? weaponBaseValue : baseStats.projectileFireRate;
        float afterWeaponMods = FloatStatQuery(weaponStat, weaponBase);
        float globalDelta = ProjectileFireRate - baseStats.projectileFireRate;
        return afterWeaponMods + globalDelta;
    }

    public float ProjectileFireRateForWeapon(HeldWeaponType weapon)
    {
        return weapon switch
        {
            HeldWeaponType.Spear => SpearProjectileFireRate,
            HeldWeaponType.Axe => AxeProjectileFireRate,
            HeldWeaponType.Mace => MaceProjectileFireRate,
            HeldWeaponType.Staff => StaffProjectileFireRate,
            _ => ProjectileFireRate,
        };
    }

    public float ProjectileAnimationSpeed { get { return FloatStatQuery(StatType.ProjectileAnimationSpeed, baseStats.projectileAnimationSpeed); } }

    public int FireCharges { get { return IntStatQuery(StatType.FireCharges, baseStats.fireCharges); } }

    public float FireChargeCooldown { get { return FloatStatQuery(StatType.FireChargeCooldown, baseStats.fireChargeCooldown); } }

    public float ProjectileSpread { get { return FloatStatQuery(StatType.ProjectileSpread, baseStats.projectileSpread); } }

    public int HomingProjectiles { get { return IntStatQuery(StatType.HomingProjectiles, baseStats.enableHomingProjectiles); } }

    // Shockwave Properties

    public float ShockwaveDamage { get { return FloatStatQuery(StatType.ShockwaveDamage, baseStats.shockwaveDamage); } }

    public float ShockwaveRadius { get { return FloatStatQuery(StatType.ShockwaveRadius, baseStats.shockwaveRadius); } }

    public float ShockwaveKnockback { get { return FloatStatQuery(StatType.ShockwaveKnockback, baseStats.shockwaveKnockback); } }

    public float ShockwaveCooldown { get { return FloatStatQuery(StatType.ShockwaveCooldown, baseStats.shockwaveCooldown); } }

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
            StatType.SpearMeleeDamage => baseStats.spearMeleeDamage,
            StatType.AxeMeleeDamage => baseStats.axeMeleeDamage,
            StatType.MaceMeleeDamage => baseStats.maceMeleeDamage,
            StatType.StaffMeleeDamage => baseStats.staffMeleeDamage,
            StatType.MeleeAttackRate => baseStats.meleeAttackRate,
            StatType.SpearMeleeAttackRate => baseStats.spearMeleeAttackRate,
            StatType.AxeMeleeAttackRate => baseStats.axeMeleeAttackRate,
            StatType.MaceMeleeAttackRate => baseStats.maceMeleeAttackRate,
            StatType.StaffMeleeAttackRate => baseStats.staffMeleeAttackRate,
            StatType.MeleeAnimationSpeed => baseStats.meleeAnimationSpeed,
            StatType.ProjectileDamage => baseStats.projectileDamage,
            StatType.SpearProjectileDamage => baseStats.spearProjectileDamage,
            StatType.AxeProjectileDamage => baseStats.axeProjectileDamage,
            StatType.MaceProjectileDamage => baseStats.maceProjectileDamage,
            StatType.StaffProjectileDamage => baseStats.staffProjectileDamage,
            StatType.ProjectileFireRate => baseStats.projectileFireRate,
            StatType.SpearProjectileFireRate => baseStats.spearProjectileFireRate,
            StatType.AxeProjectileFireRate => baseStats.axeProjectileFireRate,
            StatType.MaceProjectileFireRate => baseStats.maceProjectileFireRate,
            StatType.StaffProjectileFireRate => baseStats.staffProjectileFireRate,
            StatType.ProjectileAnimationSpeed => baseStats.projectileAnimationSpeed,
            StatType.FireCharges => baseStats.fireCharges,
            StatType.FireChargeCooldown => baseStats.fireChargeCooldown,
            StatType.ProjectileSpread => baseStats.projectileSpread,
            StatType.HomingProjectiles => baseStats.enableHomingProjectiles,
            StatType.ShockwaveDamage => baseStats.shockwaveDamage,
            StatType.ShockwaveRadius => baseStats.shockwaveRadius,
            StatType.ShockwaveKnockback => baseStats.shockwaveKnockback,
            StatType.ShockwaveCooldown => baseStats.shockwaveCooldown,
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
        return $"Health: {Health}, MoveSpeed: {MoveSpeed:F1}, AttackSpeed: {MeleeAnimationSpeed}, FireChargeCooldown: {FireChargeCooldown} ,Projectile Damage: {ProjectileDamage}, Melee Damage: {MeleeDamage}, Slam Damage: {ShockwaveDamage}";
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
