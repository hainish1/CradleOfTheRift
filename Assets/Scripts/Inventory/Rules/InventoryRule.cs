using System;
using System.Collections.Generic;
using UnityEngine;
public enum InventoryRuleActionType
{
    None,

    //inventory changes
    AddStacks,
    RemoveStacks,
    RemoveAllStacks,
    SetCount,
    TransformItem,

    // loot state changes
    UnlockLootItem,
    BlockLootItem
}

[Serializable]
public struct InventoryRuleActionSpec
{
    public InventoryRuleActionType type;

    // [Header("Primary")]
    public ItemData item; // used by most actions
    public int amount; // stacks/count

    // [Header("Transform")]
    public ItemData otherItem; // used by TransformItem

    public int SafeAmount => Mathf.Max(1, amount);
}

[CreateAssetMenu(fileName = "InventoryRule", menuName = "Items/Inventory Rule")]
public class InventoryRule : ScriptableObject
{
    [Serializable]
    public class Requirement
    {
        public ItemData item;
        public int minCount = 1;
    }

    public List<Requirement> requirements = new();




    [Header("Actions")]
    public List<InventoryRuleActionSpec> actions = new();

    [Header("Behavior")]
    public bool oneWayUnlock = true;

    public bool IsMet(PlayerInventory inventory)
    {
        if (inventory == null) return false;

        foreach (var req in requirements)
        {
            if (req.item == null) return false;
            int have = inventory.GetItemCount(req.item);
            if (have < Mathf.Max(1, req.minCount)) return false;
        }
        return true;
    }
}

