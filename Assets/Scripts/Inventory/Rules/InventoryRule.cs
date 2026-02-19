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
        [Tooltip("Items in the same group use OR logic. AND logic is applied across different groups.\nExample: Group 0 = A/B/C, Group 1 = D/E → (A OR B OR C) AND (D OR E)")]
        public int groupId = 0;
    }

    public List<Requirement> requirements = new();

    [Header("Actions")]
    public List<InventoryRuleActionSpec> actions = new();

    [Header("Behavior")]
    [Tooltip("If true, this rule fires only once per run and won't repeat even if conditions stay met.")]
    public bool oneWayUnlock = true;

    public bool IsMet(PlayerInventory inventory)
    {
        if (inventory == null || requirements.Count == 0) return false;

        // group requirements by groupId; within each group use OR, across groups use AND
        var groups = new Dictionary<int, List<Requirement>>();
        foreach (var req in requirements)
        {
            if (req.item == null) continue;
            if (!groups.ContainsKey(req.groupId))
                groups[req.groupId] = new List<Requirement>();
            groups[req.groupId].Add(req);
        }

        if (groups.Count == 0) return false;

        foreach (var group in groups.Values)
        {
            bool groupMet = false;
            foreach (var req in group)
            {
                if (inventory.GetItemCount(req.item) >= Mathf.Max(1, req.minCount))
                {
                    groupMet = true;
                    break;
                }
            }
            if (!groupMet) return false;
        }

        return true;
    }
}

