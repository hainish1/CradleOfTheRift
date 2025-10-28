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
    public float stompBounceForce = 8f; //  bounce force when stomping
    public float fallDamageBonusPerMeter = 2f; // extra slam damage per meter fallen
    public float fallDamageBonusPerStack = 1f; // multiplier per stack
    public float effectDuration = -1f; // -1 = permanent
    
    [Header("DOT Effect Settings")]
    public float dotDamagePerTick = 2f;  // base dmg per tick
    public float dotTickInterval = 1f;  // how often to tick (seconds)
    public float dotDuration = 5f;  // how long DOT lasts total
    public float dotDamagePerStack = 1f;  // extra dmg per item stack
    public bool dotCanStack = true;  // can this DOT stack on same enemy
    public int dotMaxStacks = 5;  // max stacks allowed
    public bool dotApplyImmediately = false;  // first tick instant or delayed
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