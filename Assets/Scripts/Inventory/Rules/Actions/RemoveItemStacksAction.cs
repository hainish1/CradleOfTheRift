using UnityEngine;

[CreateAssetMenu(menuName = "Items/Inventory Rule Actions/Remove Item Stacks", fileName = "RemoveItemStacksAction")]
public class RemoveItemStacksAction : InventoryRuleAction
{
    [SerializeField] private ItemData item;
    [Min(1)][SerializeField] private int stacksToRemoveFromInventory = 1;

    public override void Execute(PlayerInventory inventory)
    {
        if(inventory == null || item == null) return;
        inventory.TryRemoveStacks(item, stacksToRemoveFromInventory);
    }
    
}


