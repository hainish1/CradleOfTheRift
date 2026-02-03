using UnityEngine;

[CreateAssetMenu(menuName = "Items/Inventory Rule Actions/Transform Item Stacks", fileName = "TransformItemStacksAction")]

public class TransformItemStacksAction : InventoryRuleAction
{
    [SerializeField] private ItemData removeItem;
    [SerializeField] private ItemData addItem;
    [Min(1)][SerializeField] private int stacks = 1;

    public override void Execute(PlayerInventory inventory)
    {
        if (inventory == null || removeItem == null || addItem == null) return;
        if (inventory.TryRemoveStacks(removeItem, stacks))
        {
            for (int i = 0; i < stacks; i++) inventory.AddItem(addItem);
        }
    }
}
