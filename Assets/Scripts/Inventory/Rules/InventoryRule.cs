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
    BlockLootItem,

    // upgrade pool changes
    AddToUpgradePool,
    RemoveFromUpgradePool
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

    [Header("Condition Logic")]
    [Tooltip("AND: all requirements must be met (default). OR: any one requirement is enough.")]
    public bool useOrLogic = false;

    [Header("Actions")]
    public List<InventoryRuleActionSpec> actions = new();

    [Header("Behavior")]
    [Tooltip("If true, this rule fires only once per run and won't repeat even if conditions stay met.")]
    public bool oneWayUnlock = true;

    public bool IsMet(PlayerInventory inventory)
    {
        if (inventory == null || requirements.Count == 0) return false;

        if (useOrLogic)
        {
            // OR: at least one requirement must be satisfied
            foreach (var req in requirements)
            {
                if (req.item == null) continue;
                if (inventory.GetItemCount(req.item) >= Mathf.Max(1, req.minCount))
                    return true;
            }
            return false;
        }
        else
        {
            // AND: every requirement must be satisfied
            foreach (var req in requirements)
            {
                if (req.item == null) return false;
                if (inventory.GetItemCount(req.item) < Mathf.Max(1, req.minCount))
                    return false;
            }
            return true;
        }
    }
}

