using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class InventoryController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private UIDocument uiDocument;

    [Header("Settings")]
    [Tooltip("Key to open/close inventory")]
    [SerializeField] private KeyCode toggleKey = KeyCode.I; 
    [Tooltip("If true, multiplies stats by stack count in description.")]
    [SerializeField] private bool useDynamicDescriptions = true; 

    private VisualElement root;
    private Label descriptionLabel;
    private List<VisualElement> itemSlots = new List<VisualElement>();
    private ItemData currentSelectedItem;

    private bool isInventoryOpen = false;

    private void OnEnable()
    {
        root = uiDocument.rootVisualElement;
        
        // HIDE INVENTORY ON START
        root.style.display = DisplayStyle.None;
        isInventoryOpen = false;

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

        // Subscribe to inventory logic events to trigger UI refreshes
        if (playerInventory != null)
        {
            playerInventory.OnItemAdded += HandleInventoryChanged;
            playerInventory.OnItemStackChanged += HandleInventoryChanged;
            playerInventory.OnItemRemoved += HandleItemRemoved;
        }
    }

    private void Update()
    {
        // Listen for input to open or close the menu
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory(!isInventoryOpen);
        }
    }

    private void ToggleInventory(bool isOpen)
    {
        isInventoryOpen = isOpen;

        if (isInventoryOpen)
        {
            // Open Inventory
            root.style.display = DisplayStyle.Flex; // Show UI
            
            // Unlock Cursor so you can click items
            UnityEngine.Cursor.visible = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            
            RefreshInventoryDisplay();
        }
        else
        {
            // Close Inventory
            root.style.display = DisplayStyle.None; // Hide UI
            
            // Lock Cursor 
            UnityEngine.Cursor.visible = false;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
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
        if (isInventoryOpen) RefreshInventoryDisplay();
        if (currentSelectedItem == data) UpdateDescription(data);
    }

    // Clear selection if the currently viewed item is dropped/removed
    private void HandleItemRemoved(ItemData data)
    {
        if (isInventoryOpen) RefreshInventoryDisplay();
        if (currentSelectedItem == data)
        {
            currentSelectedItem = null;
            descriptionLabel.text = "Select an item to view details.";
        }
    }

    private void RefreshInventoryDisplay()
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

        // Query current count from logic to handle dynamic math
        int currentStacks = playerInventory.GetItemCount(data);

        // Request the formatted text from the ItemData script
        string finalDescription = data.GetFormattedDescription(currentStacks, useDynamicDescriptions);
        
        // Append footer info if dynamic scaling is active
        if (useDynamicDescriptions && currentStacks > 1)
             finalDescription += $"\n(Total for {currentStacks} stacks)";

        descriptionLabel.text = finalDescription;
    }
}