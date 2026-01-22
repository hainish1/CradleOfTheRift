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
    FlyFire
}

[Serializable]
public class EffectSpec
{
    public ItemEffectKind kind = ItemEffectKind.None;
    public float duration = -1f; // -1 : Perm

    // HEAL ON DAMAGE
    [Range(0f, 1f)] public float healOnDamagePercentPerStack = .02f;

    // Stomp
    public float stompDamagePerStack = 10f;
    public float stompBounceForce = 8f;

    // FallDamageBonus
    public float fallDamageBonusPerMeter = 2f;
    public float fallDamageBonusPerStack = 1f;

    // DOT

    // Poison pool on dash
    public float poisonPoolRadius = 4f;
    public float poisonPoolLifetime = 4f;

    // Homing Projectiles
    public int numberOfProjectiles = 3;
    public float projectileDamageMultiplier = 1.5f;

    // Explosive Projectiles
    public float explosiveAoeRadius = 3f;
    public float explosiveAoeDamageMultiplier = 0.5f;
    public float explosiveMaxRange = 0f;
    public GameObject explosiveVFX;

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
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Legendary   // Do we need 4 tiers?
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

}



