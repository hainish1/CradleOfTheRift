using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[Serializable]
public class StatModSpec
{
    public StatType statType = StatType.Health;
    public OperatorType operatorType = OperatorType.Add;
    public float value = 1f;
    public int duration = -1; // -1 is perm
}

public enum ItemEffectKind
{
    None,
    HealOnDamage,
    HealOnPoison,
    StompDamage,
    FallDamageBonus,
    DotOnHit,
    PoisonPoolOnDash,
    BurnOnDamage,
    HomingProjectiles,
    ExplosiveProjectiles,
    ChainLightning,
    BounceProjectiles,
    DelayedProjectiles,
    DashDamage,
    ElementFusion,
    ArcStrike,
    ElementReactionExplosion,
    LightningStrike,
    FlyFire,
    OrbitingFireballs,
    LightningDash,
    OrbitingFireballsTest,
    ChainLightningTest,
    PassThroughSpear,
    ToxicAttackSpeed,
    FlameTrail,
    FinisherStrike,
    XPGrant,
    PureCore,
    PoisonPoolProjectile,
    LightningStrikeBase,
    LightningStrikeDamage,
    LightningStrikeChainBuff,
    LightningStrikePlayerChain,
    LightningStrikeElectrify,
    LightningStrikeCooldown,
    LightningStrikeCount,
    LightningStrikeSelfHeal,
    OrbitingFireballBase,
    OrbitingFireballBonusCount,
    OrbitingFireballBonusDamage,
    OrbitingFireballBonusSpeed,
    OrbitingFireballOnKill,
    GroundSlam,
    ZoomUpgrade,
    GloomUpgrade,
    OrbitingFireballOnLightning,
    PoisonCloud,
}

[Serializable]
public class EffectSpec
{
    public ItemEffectKind kind = ItemEffectKind.None;
    [Tooltip("description shown on the upgrade thing for this effect")]
    public string description = "";
    public float duration = -1f; // -1 : Perm

    // HEAL ON DAMAGE
    [Range(0f, 1f)] public float healOnDamagePercentPerStack = .02f;

    // Heal on Poison 
    public float healOnPoisonRange = 8f;
    public float healPerPoisonStackPerSecond = 0.5f;
    public float healOnPoisonTickInterval = 1f;

    // Stomp
    public float stompDamagePerStack = 10f;
    public float stompBounceForce = 8f;

    // FallDamageBonus
    public float fallDamageBonusPerMeter = 2f;
    public float fallDamageBonusPerStack = 1f;

    // DOT 
    public float dotDamagePerTick = 2f;
    public float dotTickInterval = 1f;
    public float dotDuration = 4f;
    public float dotDamagePerStack = 2f;
    public bool dotCanStack = true;
    public int dotMaxStacks = 5;
    public bool dotApplyImmediately = false;

    // Poison pool on dash
    public float poisonPoolRadius = 4f;
    public float poisonPoolLifetime = 4f;

    // Poison pool projectile 
    public GameObject poisonPoolProjectileVFX;

    // Homing Projectiles
    public int numberOfProjectiles = 3;
    public float projectileDamageMultiplier = 1.5f;

    // Explosive Projectiles
    public float explosiveAoeRadius = 3f;
    public float explosiveAoeDamageMultiplier = 0.5f;
    public float explosiveMaxRange = 0f;
    public float explosiveFireballSize = 7.5f;
    public float explosiveProjectileSpeed = 0.7f;
    public GameObject explosiveVFX;
    public GameObject explosiveTravelVFX;

    // Chain Lightning
    public float chainDamagePercent = 0.5f;
    public int maxChainCount = 3;
    public float chainRange = 8f;
    public GameObject chainLightningVFX;

    // Bounce Projectiles
    public float bounceRange = 10f;
    public int maxBounceCount = 3;
    [Range(0f, 1f)] public float damageMultiplierPerBounce = 0.8f;
    public GameObject bounceVFX;

    // Delayed Projectiles
    public float delayedDamageTime = 2f;
    public float delayedDamageMultiplier = 1f;
    public GameObject delayedMarkVFX;

    // Dash Damage
    public float dashDamage = 10f;
    public float dashDamageRange = 5f;

    // Element Fusion
    public ElementType fusionTriggerElement = ElementType.None;
    public ElementType fusionEffectElement = ElementType.None;

    // Burn Aura (Sunfire-like)
    public float burnAuraDamagePerSecond = 5f;
    public float burnAuraRange = 3f;
    public float burnAuraTickInterval = 1f;

    // Arc Strike (Poisson-based lightning)
    public float arcStrikeDamage = 10f;
    public float arcStrikeRange = 10f;
    public float arcStrikePoissonLambda = 5.5f;

    // Player Lightning Strike
    public float playerLightningStrikeDamage = 12f;
    public float playerLightningStrikeRadius = 3f;
    public float playerLightningStrikeInterval = 5f;
    public float playerLightningStrikeElectrifyDamage = 4f;
    public GameObject playerLightningStrikeVFX;

    // Flying Fire Spray
    public float flyingFireDamage = 8f;
    public float flyingFireRadius = 3f;
    public float flyingFireTickInterval = 0.5f;
    public float flyingFireOffset = 1.5f;

    // Element Reaction Explosion (Lightning + Fire)
    public float elementReactionExplosionDamage = 20f;
    public float elementReactionExplosionRadius = 3f;
    public float elementReactionExplosionCooldown = 1f;
    public GameObject elementReactionExplosionVFX;

    // Orbiting Fireballs
    public float orbitingFireballDamage = 10f;
    public float orbitingFireballRadius = 3f;
    public float orbitingFireballRotationSpeed = 60f;
    public GameObject orbitingFireballVFX;

    // Lightning Dash
    public float lightningDashDamage = 20f;
    public int lightningDashChainCount = 5;
    public float lightningDashChainRange = 15f;

    // Orbiting Fireballs Test
    public float orbitingFireballTestDamage = 10f;
    public float orbitingFireballTestDamageThreshold = 100f;
    public int orbitingFireballTestMaxCount = 20;
    public float orbitingFireballTestLifetime = 5f;

    // Chain Lightning Test
    public float chainLightningTestStrikeInterval = 5f;
    public float chainLightningTestBuffDuration = 3f;
    public float chainLightningTestChainDamagePercent = 0.5f;
    public int chainLightningTestMaxDepth = 2;
    public int chainLightningTestBranchesPerNode = 2;
    public float chainLightningTestChainRange = 10f;

    // Pass Through Spear
    [Min(0)] public int passThroughEnemyCount = 1;

    // Toxic Attack Speed
    public float toxicAttackSpeedRange = 10f;
    [Tooltip("Attack speed multiplier per poison stack (e.g. 0.02 = +2% per stack)")]
    [Range(0f, 0.2f)] public float toxicAttackSpeedPerStack = 0.02f;

    // Flame Trail
    public float flameTrailDamage = 15f;
    public float flameTrailRadius = 2f;
    public float flameTrailLifetime = 1.5f;
    public float flameTrailSpawnInterval = 0.2f;

    // Finisher Strike
    public float finisherDamageMultiplier = 1.5f;
    public float finisherRangeMultiplier = 1.3f;

    // XP Grant
    public int xpGrantAmount = 50;

    // Pure Core 
    [Tooltip("Damage multiplier when player has no elemental items (e.g. 1.15 = +15%)")]
    public float pureCoreDamageMultiplier = 1.15f;
    [Tooltip("Health multiplier when player has no elemental items (e.g. 1.2 = +20%)")]
    public float pureCoreHealthMultiplier = 1.2f;

    // Lightning Strike Damage Boost
    public float lightningStrikeBonusDamage = 5f;

    // Lightning Strike Cooldown Reduction
    public float lightningStrikeCooldownReduction = 0.5f;

    // Lightning Strike Bonus Count
    public int lightningStrikeBonusCount = 1;
    public float lightningStrikeSpreadRadius = 8f;

    // Ground Slam
    public float groundSlamDamage = 20f;

    // Zoom Upgrade 
    public float zoomUpgradeDamage = 15f;
    public float zoomUpgradeRange = 5f;
    public float zoomUpgradeDashDistanceBonus = 0f;
    public GameObject zoomUpgradeVFX;

    // Gloom Upgrade (poison pool)
    public float gloomDamagePerTick = 5f;
    public float gloomTickInterval = 0.5f;
    public float gloomPoolRadius = 4f;
    public float gloomPoolLifetime = 6f;
    public float gloomAttackSpeedBuff = 0.3f;
    public float gloomFireCooldownIncrease = 0f;
    public GameObject gloomPoolPrefab;

    // Orbiting Fireball Upgrades
    public int orbitingFireballBonusCount = 1;
    public float orbitingFireballBonusDamage = 3f;
    public float orbitingFireballBonusSpeed = 15f;
    public float orbitingFireballOnKillDuration = 5f;
    public float orbitingFireballOnLightningThreshold = 100f;
    public float orbitingFireballOnLightningDuration = 5f;

    // Poison cloud 
    public float poisonCloudDamagePerTick = 3f;
    public float poisonCloudDamageTickInterval = 1f;
    public float poisonCloudRadius = 3f;
    public float poisonCloudLifetime = 5f;
    public float poisonCloudBehindDistance = 1.25f;
    public GameObject poisonCloudVfxPrefab;

    [Header("Wwise SFX")]
    [Tooltip("Lightning sound")]
    public AK.Wwise.Event onLightning;
    [Tooltip("Fire/Fireball sound")]
    public AK.Wwise.Event onFire;
    [Tooltip("Explosion sound ")]
    public AK.Wwise.Event onExplosion;
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Legendary,
    Crossover
}

public enum StackingType
{
    Linear
}

// Legacy single-stat fields remain but they will be ignored when useMultipleStats is TRUE
[CreateAssetMenu(fileName = "New Item", menuName = "Items/Item")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName = "New Item";
    [TextArea(3, 5)]
    public string description = "Item Description Here";
    public Sprite icon;

    [Header("Rarity and visuals")]
    public ItemRarity rarity = ItemRarity.Common;
    public Color rarityColor = Color.white;

    [Header("Stacking")]
    public bool canStack = true;
    public int maxStacks = 99;
    public StackingType stackingType = StackingType.Linear;

    [Space]

    [Header("MULTIPLE Stat Effects")]
    public bool useMultipleStats = true;
    public List<StatModSpec> statMods = new List<StatModSpec>();

    [Header("MULTIPLE Runtime Effects")]
    public List<EffectSpec> effects = new List<EffectSpec>();

    [Space]

    // Legacy Single-Stat, I will ignore this when useMultipleStats is TRUE

    [Header("Stat effects - Only if 1 Stack")]
    public StatType statType = StatType.Health;
    public OperatorType operatorType = OperatorType.Add;
    public float value = 1f;
    public int duration = -1; // perm by default
    
    [Header("Identity")]
    public string itemId; // unique ID for saving / quering

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            itemId = System.Guid.NewGuid().ToString("N");
        }
    }

        public string GetFormattedDescription(int stackCount, bool showTotalStats)
    {
        // Calculate the Multiplier
        // For exponential stacking: Total = Value ^ StackCount
        float effectiveStack = showTotalStats ? Mathf.Max(1, stackCount) : 1f;
        float compoundedValue = Mathf.Pow(value, effectiveStack);

        float displayValue = 0f;

        // Format based on Operator Type
        if (operatorType == OperatorType.Percentage)
        {
            // Example: 0.8^2 = 0.64 -> 64%
            displayValue = compoundedValue * 100f;
        }
        else if (operatorType == OperatorType.Multiply)
        {
            if (compoundedValue < 1.0f)
                {
                    // For values like 0.8: (0.8^1) * 100 = 80% 
                    displayValue = compoundedValue * 100f;
                }
            else
            {
                // Example: 1.05^2 = 1.1025 -> 10.25% increase
                // Subtract 1 to show just the "bonus" percentage
                displayValue = (compoundedValue - 1f) * 100f;
            }
        }
        else if (operatorType == OperatorType.Add)
        {
            // Example: 5 * 2 = 10
            displayValue = value * effectiveStack;
        }

        try 
        {
            // Inject the calculated value into the {0} placeholder
            return string.Format(description, displayValue);
        }
        catch 
        {
            return description;
        }
    }
}



