using System.Collections.Generic;
using UnityEngine;

public class InventoryRuleRunner : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private List<InventoryRule> rules = new();

    // tracks applied state during runtime
    private readonly HashSet<InventoryRule> applied = new();
    [SerializeField] private RunLootState runLootState;

    private bool isApplying;

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<PlayerInventory>();
        if (runLootState == null) runLootState = RunLootState.Instance;
    }
    private void OnEnable()
    {
        if (inventory == null) return;
        inventory.OnItemAdded += OnInventoryChanged;
        inventory.OnItemRemoved += OnInventoryChanged;
        inventory.OnItemStackChanged += OnInventoryChanged;
        EvaluateAll();
    }

    private void OnDisable()
    {
        if (inventory == null) return;
        inventory.OnItemAdded -= OnInventoryChanged;
        inventory.OnItemRemoved -= OnInventoryChanged;
        inventory.OnItemStackChanged -= OnInventoryChanged;
    }

    private void OnInventoryChanged(ItemData _, PlayerInventory.ItemStack __)
    {
        if (isApplying) return;
        EvaluateAll();
    }
    private void OnInventoryChanged(ItemData _)
    {
        if (isApplying) return;
        EvaluateAll();
    }

    private void EvaluateAll()
    {
        if (inventory == null) return;

        foreach (var rule in rules)
        {
            if (rule == null) continue;

            bool met = rule.IsMet(inventory);
            bool isApplied = applied.Contains(rule);

            // skip if one way rule already fired
            if (rule.oneWayUnlock && isApplied) continue;

            if (met)
            {
                ApplyActions(rule);
            }
        }
    }

    private void ApplyActions(InventoryRule rule)
    {
        if (rule.actions == null || rule.actions.Count == 0) return;

        isApplying = true;

        foreach (var action in rule.actions)
        {
            switch (action.type)
            {
                case InventoryRuleActionType.AddStacks:
                    if (action.item != null)
                    {
                        int n = action.SafeAmount;
                        for (int i = 0; i < n; i++) inventory.AddItem(action.item);
                    }
                    break;

                case InventoryRuleActionType.RemoveStacks:
                    if (action.item != null)
                        inventory.TryRemoveStacks(action.item, action.SafeAmount);
                    break;

                case InventoryRuleActionType.RemoveAllStacks:
                    if (action.item != null)
                        inventory.TryRemoveStacks(action.item, inventory.GetItemCount(action.item));
                    break;

                case InventoryRuleActionType.SetCount:
                    if (action.item != null)
                        inventory.SetItemCount(action.item, Mathf.Max(0, action.amount));
                    break;

                case InventoryRuleActionType.TransformItem:
                    if (action.item != null && action.otherItem != null)
                    {
                        int n = action.SafeAmount;
                        if (inventory.TryRemoveStacks(action.item, n))
                            for (int i = 0; i < n; i++) inventory.AddItem(action.otherItem);
                    }
                    break;

                case InventoryRuleActionType.UnlockLootItem:
                    if (action.item != null && runLootState != null)
                        runLootState.Unlock(action.item);
                    break;

                case InventoryRuleActionType.BlockLootItem:
                    if (action.item != null && runLootState != null)
                        runLootState.Block(action.item);
                    break;

                case InventoryRuleActionType.AddToUpgradePool:
                    if (action.item != null && UpgradeLevelManager.Instance != null)
                        UpgradeLevelManager.Instance.UnlockForUpgrade(action.item);
                    break;

                case InventoryRuleActionType.RemoveFromUpgradePool:
                    if (action.item != null && UpgradeLevelManager.Instance != null)
                        UpgradeLevelManager.Instance.LockFromUpgrade(action.item);
                    break;
            }
        }

        // mark as applied so one-way rules don't fire again
        if (rule.oneWayUnlock)
            applied.Add(rule);

        isApplying = false;
    }

    public void ResetForNewRun()
    {
        applied.Clear();
        isApplying = false;
        EvaluateAll();
    }

}