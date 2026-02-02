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
        if (inventory == null || runLootState == null) return;

        foreach (var rule in rules)
        {
            if (rule == null) continue;

            bool met = rule.IsMet(inventory);
            bool isApplied = applied.Contains(rule);

            if (met && !isApplied)
            {
                // first block spawns of items that are removed using rules
                foreach (var item in rule.removeFromPool)
                    runLootState.Block(item);
                foreach (var item in rule.addToPool)
                    runLootState.Unlock(item);

                if (rule.actions != null && rule.actions.Count > 0)
                {
                    isApplying = true;
                    foreach (var action in rule.actions)
                    {
                        if (action != null) action.Execute(inventory); // execute the actions here
                    }
                    isApplying = false;
                }

                applied.Add(rule);
            }
        }
    }
}