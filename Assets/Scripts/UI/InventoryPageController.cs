using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class InventoryPageController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    
    [Header("Settings")]
    [Tooltip("If true, multiplies stats by stack count in description.")]
    [SerializeField] private bool useDynamicDescriptions = true; 

    // Internal Variables
    private VisualElement root;
    private Label descriptionLabel;
    private List<VisualElement> itemSlots = new List<VisualElement>();
    private ItemData currentSelectedItem;

    public void Initialize(VisualElement pageRoot)
    {
        root = pageRoot;

        // Locate UI elements by their names/classes defined in UXML
        descriptionLabel = root.Q<Label>("DescriptionLabel");
        
        var itemsContainer = root.Q<VisualElement>("ItemsVisual");
        if (itemsContainer != null)
        {
            // Gather all visual slots tagged with the "item" class
            itemSlots = itemsContainer.Query<VisualElement>(className: "item").ToList();
        }

        // Link click events to every slot in the grid
        for (int i = 0; i < itemSlots.Count; i++)
        {
            VisualElement slot = itemSlots[i];
            slot.RegisterCallback<ClickEvent>(evt => OnSlotClicked(slot));
        }

        // Initial Refresh
        RefreshInventoryDisplay();

        // Subscribe to events
        if (playerInventory != null)
        {
            playerInventory.OnItemAdded += HandleInventoryChanged;
            playerInventory.OnItemStackChanged += HandleInventoryChanged;
            playerInventory.OnItemRemoved += HandleItemRemoved;
        }
    }

    private void OnDisable()
    {
        if (playerInventory != null)
        {
            playerInventory.OnItemAdded -= HandleInventoryChanged;
            playerInventory.OnItemStackChanged -= HandleInventoryChanged;
            playerInventory.OnItemRemoved -= HandleItemRemoved;
        }
    }

    // Refresh UI automatically when the inventory contents change
    private void HandleInventoryChanged(ItemData data, PlayerInventory.ItemStack stack)
    {
        RefreshInventoryDisplay();
        if (currentSelectedItem == data) UpdateDescription(data);
    }

    // Clear selection if the currently viewed item is dropped/removed
    private void HandleItemRemoved(ItemData data)
    {
        RefreshInventoryDisplay();
        if (currentSelectedItem == data)
        {
            currentSelectedItem = null;
            if(descriptionLabel != null) descriptionLabel.text = "Select an item to view details.";
        }
    }

    public void RefreshInventoryDisplay()
    {
        // Clear icons and data from all UI slots
        foreach(var slot in itemSlots)
        {
            slot.style.backgroundImage = null; 
            slot.userData = null;
        }

        // Map items from the logic dictionary to the visual slot grid
        int index = 0;
        foreach(var kvp in playerInventory.Items)
        {
            if(index >= itemSlots.Count) break;
            ItemData data = kvp.Key;
            
            VisualElement slot = itemSlots[index];
            if (data.icon != null) slot.style.backgroundImage = new StyleBackground(data.icon);
            slot.userData = data; // Attach data to the UI element for retrieval on click
            index++;
        }
    }

    private void OnSlotClicked(VisualElement slot)
    {
        // Retrieve the data we stored in userData during Refresh
        if (slot.userData is ItemData data)
        {
            currentSelectedItem = data;
            UpdateDescription(data);
        }
    }

    private void UpdateDescription(ItemData data)
    {
        if (data == null) return;

        // Get the current stack count from the inventory logic

        int currentStacks = playerInventory.GetItemCount(data);
        string finalDescription;

        if (useDynamicDescriptions)
            // Use the math-heavy version from ItemData
            finalDescription = data.GetFormattedDescription(currentStacks, true);
        else
            // Use the raw description from ItemData (no math/formatting applied)
            finalDescription = data.description;

        // Append the stack footer regardless of the dynamic description toggle
        // We only show it if the player actually has more than 1 of the item
        if (currentStacks > 1)
        {
            finalDescription += $"\n\n<b>Stack Count: {currentStacks}</b>";
        }

        descriptionLabel.text = finalDescription;
    }
}