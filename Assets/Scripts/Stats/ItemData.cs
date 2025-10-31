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
    BurnOnDamage
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
    public float dotDamagePerTick = 2f;
    public float dotTickInterval = 1f;
    public float dotDuration = 5f;
    public float dotDamagePerStack = 1f;
    public bool dotCanStack = true;
    public int dotMaxStacks = 5;
    public bool dotApplyImmediately = false;
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Legendary
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



