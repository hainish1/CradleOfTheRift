using UnityEngine;

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

    [Header("Stat effects")]
    public StatType statType = StatType.Health;
    public OperatorType operatorType = OperatorType.Add;
    public float value = 1f;
    public int duration = -1; // perm by default

    [Header("Stacking")]
    public bool canStack = true;
    public int maxStacks = 99;
    public StackingType stackingType = StackingType.Linear;

    [Header("Effect (non-stat)")]
    public ItemEffectKind effectKind = ItemEffectKind.None;

    [Range(0f, 1f)] public float healOnDamagePercentPerStack = 0.02f; // 2% per stack
    public float stompDamagePerStack = 10f; // base stomp damage
    public float stompBounceForce = 8f; // upward bounce force when stomping
    public float fallDamageBonusPerMeter = 2f; // extra slam damage per meter fallen
    public float fallDamageBonusPerStack = 1f; // multiplier per stack
    public float effectDuration = -1f; // -1 = permanent
    
    [Header("DOT Effect Settings")]
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

public enum ItemEffectKind
{
    None,
    HealOnDamage,
    StompDamage,
    FallDamageBonus,
    DotOnHit
}