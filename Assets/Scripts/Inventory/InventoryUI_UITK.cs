using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InventoryUI_UITK : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VisualTreeAsset itemSlotTemplate;  // ItemSlot UXML

    [Header("Rarity Colors")]
    [SerializeField] private Color commonColor = Color.white;
    [SerializeField] private Color uncommonColor = Color.green;
    [SerializeField] private Color rareColor = Color.red;
    [SerializeField] private Color legendaryColor = Color.yellow;

    [Header("Settings")]
    [SerializeField] private int maxVisibleItems = 50;

    private PlayerInventory inventory;
    private VisualElement grid;
    private Dictionary<ItemData, VisualElement> itemSlots = new();

    private void Awake()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        grid = root.Q<VisualElement>("item-grid");

        if (grid == null)
            Debug.LogError("Item grid not found in UIDocument!");
    }

    private void Start()
    {
        inventory = FindFirstObjectByType<PlayerInventory>();
        if (inventory == null)
        {
            Debug.LogError("InventoryUI: No PlayerInventory found!");
            return;
        }

        inventory.OnItemAdded += OnItemAdded;
        inventory.OnItemRemoved += OnItemRemoved;
        inventory.OnItemStackChanged += OnItemStackChanged;

        // Populate existing items
        foreach (var pair in inventory.Items)
        {
            AddItemSlot(pair.Key, pair.Value);
        }
    }

    private void OnDestroy()
    {
        if (inventory == null) return;

        inventory.OnItemAdded -= OnItemAdded;
        inventory.OnItemRemoved -= OnItemRemoved;
        inventory.OnItemStackChanged -= OnItemStackChanged;
    }

    private void OnItemAdded(ItemData itemData, PlayerInventory.ItemStack stack)
    {
        AddItemSlot(itemData, stack);
    }

    private void OnItemRemoved(ItemData itemData)
    {
        if (itemSlots.TryGetValue(itemData, out var slot))
        {
            slot.RemoveFromHierarchy();
            itemSlots.Remove(itemData);
        }
    }

    private void OnItemStackChanged(ItemData itemData, PlayerInventory.ItemStack stack)
    {
        if (itemSlots.TryGetValue(itemData, out var slot))
        {
            var countLabel = slot.Q<Label>("stack-count");
            if (countLabel != null)
                countLabel.text = stack.count > 1 ? stack.count.ToString() : "";
        }
    }

    private void AddItemSlot(ItemData itemData, PlayerInventory.ItemStack stack)
    {
        if (itemSlots.ContainsKey(itemData)) return;
        if (itemSlots.Count >= maxVisibleItems)
        {
            Debug.LogWarning("Max items reached!");
            return;
        }

        var slotElement = itemSlotTemplate.Instantiate();

        var icon = slotElement.Q<VisualElement>("icon");
        var countLabel = slotElement.Q<Label>("stack-count");

        if (icon != null && itemData.icon != null)
        {
            icon.style.backgroundImage = new StyleBackground(itemData.icon);
            icon.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
        }

        if (countLabel != null)
            countLabel.text = stack.count > 1 ? stack.count.ToString() : "";

        // Rarity background
        slotElement.style.backgroundColor = GetRarityColor(itemData.rarity);

        // Tooltip
        slotElement.RegisterCallback<ClickEvent>(_ => ShowTooltip(itemData));

        // Add to grid
        if (grid != null)
            grid.Add(slotElement);

        itemSlots[itemData] = slotElement;
    }

    private void ShowTooltip(ItemData item)
    {
        Debug.Log($"{item.itemName}: {item.description}");
        // Replace with actual tooltip UI if needed
    }

    private Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => commonColor,
            ItemRarity.Uncommon => uncommonColor,
            ItemRarity.Rare => rareColor,
            ItemRarity.Legendary => legendaryColor,
            _ => Color.white
        };
    }
}
