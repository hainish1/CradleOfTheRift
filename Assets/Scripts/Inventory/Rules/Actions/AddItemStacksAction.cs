using UnityEngine;

[CreateAssetMenu(menuName = "Items/Inventory Rule Actions/Add Item Stacks", fileName = "AddItemStacksAction")]

public class AddItemStacksAction : InventoryRuleAction
{
    [SerializeField] private ItemData item;
    [Min(1)][SerializeField] private int stacksToAdd = 1;

    public override void Execute(PlayerInventory inventory)
    {
        if (inventory == null || item == null) return;
        for (int i = 0; i < stacksToAdd; i++) inventory.AddItem(item);
    }
}
