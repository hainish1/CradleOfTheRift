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
    private bool subscribedToInventory;

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<PlayerInventory>();
        if (inventory == null) inventory = FindFirstObjectByType<PlayerInventory>();
        if (runLootState == null) runLootState = RunLootState.Instance;
    }

    private void OnEnable()
    {
        TrySubscribeToInventory();
    }

    private void OnDisable()
    {
        if (inventory == null || !subscribedToInventory) return;
        inventory.OnItemAdded -= OnInventoryChanged;
        inventory.OnItemRemoved -= OnInventoryChanged;
        inventory.OnItemStackChanged -= OnInventoryChanged;
        subscribedToInventory = false;
    }

    private void Update()
    {
        if (!subscribedToInventory)
            TrySubscribeToInventory();
    }

    private bool TrySubscribeToInventory()
    {
        if (subscribedToInventory) return true;

        if (inventory == null)
        {
            inventory = FindFirstObjectByType<PlayerInventory>();
            if (inventory == null) return false;
        }

        inventory.OnItemAdded += OnInventoryChanged;
        inventory.OnItemRemoved += OnInventoryChanged;
        inventory.OnItemStackChanged += OnInventoryChanged;
        subscribedToInventory = true;

        Debug.Log("[InventoryRuleRunner] Subscribed to PlayerInventory.");
        EvaluateAll();
        return true;
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


        bool allActionsSucceeded = true;

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
                    else allActionsSucceeded = false;
                    break;

                case InventoryRuleActionType.RemoveStacks:
                    if (action.item != null)
                        inventory.TryRemoveStacks(action.item, action.SafeAmount);
                    else allActionsSucceeded = false;
                    break;

                case InventoryRuleActionType.RemoveAllStacks:
                    if (action.item != null)
                        inventory.TryRemoveStacks(action.item, inventory.GetItemCount(action.item));
                    else allActionsSucceeded = false;
                    break;

                case InventoryRuleActionType.SetCount:
                    if (action.item != null)
                        inventory.SetItemCount(action.item, Mathf.Max(0, action.amount));
                    else allActionsSucceeded = false;
                    break;

                case InventoryRuleActionType.TransformItem:
                    if (action.item != null && action.otherItem != null)
                    {
                        int n = action.SafeAmount;
                        if (inventory.TryRemoveStacks(action.item, n))
                            for (int i = 0; i < n; i++) inventory.AddItem(action.otherItem);
                    }
                    else allActionsSucceeded = false;
                    break;

                case InventoryRuleActionType.UnlockLootItem:
                    if (runLootState == null) runLootState = RunLootState.Instance;
                    if (action.item != null && runLootState != null)
                        runLootState.Unlock(action.item);
                    else
                    {
                        Debug.LogWarning($"[InventoryRuleRunner] UnlockLootItem skipped (runLootState={(runLootState==null?"null":"ok")}, item={(action.item==null?"null":action.item.itemName)}). Will retry.");
                        allActionsSucceeded = false;
                    }
                    break;

                case InventoryRuleActionType.BlockLootItem:
                    if (runLootState == null) runLootState = RunLootState.Instance;
                    if (action.item != null && runLootState != null)
                        runLootState.Block(action.item);
                    else
                    {
                        Debug.LogWarning($"[InventoryRuleRunner] BlockLootItem skipped (runLootState={(runLootState==null?"null":"ok")}, item={(action.item==null?"null":action.item.itemName)}). Will retry.");
                        allActionsSucceeded = false;
                    }
                    break;

                case InventoryRuleActionType.AddToUpgradePool:
                    if (action.item != null && UpgradeLevelManager.Instance != null)
                        UpgradeLevelManager.Instance.UnlockForUpgrade(action.item);
                    else
                    {
                        Debug.LogWarning($"[InventoryRuleRunner] AddToUpgradePool skipped (UpgradeLevelManager.Instance={(UpgradeLevelManager.Instance==null?"null":"ok")}, item={(action.item==null?"null":action.item.itemName)}). Will retry.");
                        allActionsSucceeded = false;
                    }
                    break;

                case InventoryRuleActionType.RemoveFromUpgradePool:
                    if (action.item != null && UpgradeLevelManager.Instance != null)
                        UpgradeLevelManager.Instance.LockFromUpgrade(action.item);
                    else
                    {
                        Debug.LogWarning($"[InventoryRuleRunner] RemoveFromUpgradePool skipped (UpgradeLevelManager.Instance={(UpgradeLevelManager.Instance==null?"null":"ok")}, item={(action.item==null?"null":action.item.itemName)}). Will retry.");
                        allActionsSucceeded = false;
                    }
                    break;
            }
        }

        if (rule.oneWayUnlock && allActionsSucceeded)
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
