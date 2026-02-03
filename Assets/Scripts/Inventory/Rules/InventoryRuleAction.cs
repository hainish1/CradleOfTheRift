using UnityEngine;

public abstract class InventoryRuleAction : ScriptableObject
{
    public abstract void Execute(PlayerInventory inventory);
}
