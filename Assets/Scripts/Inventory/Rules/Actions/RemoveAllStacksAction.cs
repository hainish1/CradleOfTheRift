using UnityEngine;

[CreateAssetMenu(menuName = "Items/Inventory Rule Actions/Remove All Stacks", fileName = "RemoveAllStacksAction")]
public class RemoveAllStacksAction : InventoryRuleAction
{
    [SerializeField] private ItemData item;

    public override void Execute(PlayerInventory inventory)
    {
        if(inventory == null || item == null) return;
        int have = inventory.GetItemCount(item);
        if(have > 0) inventory.TryRemoveStacks(item, have);
    }
    
}
