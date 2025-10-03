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