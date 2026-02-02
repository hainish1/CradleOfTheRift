using System;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("When requirements are met")]
    public List<ItemData> addToPool = new();

    [Header("When requirements are met (block from spawning)")]
    public List<ItemData> removeFromPool = new();

    [Header("Actions (run-time effects)")]
    public List<InventoryRuleAction> actions = new();

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