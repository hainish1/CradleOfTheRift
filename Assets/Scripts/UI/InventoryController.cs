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
        
        // --- 1. HIDE INVENTORY ON START ---
        // We set the display to None immediately so it doesn't block the screen on start
        root.style.display = DisplayStyle.None;
        isInventoryOpen = false;

        descriptionLabel = root.Q<Label>("DescriptionLabel"); // [cite: 20]
        
        var itemsContainer = root.Q<VisualElement>("ItemsVisual"); // [cite: 2]
        if (itemsContainer != null)
        {
            itemSlots = itemsContainer.Query<VisualElement>(className: "item").ToList(); // [cite: 4]
        }

        // Register Clicks
        for (int i = 0; i < itemSlots.Count; i++)
        {
            VisualElement slot = itemSlots[i];
            slot.RegisterCallback<ClickEvent>(evt => OnSlotClicked(slot));
        }

        if (playerInventory != null)
        {
            playerInventory.OnItemAdded += HandleInventoryChanged;
            playerInventory.OnItemStackChanged += HandleInventoryChanged;
            playerInventory.OnItemRemoved += HandleItemRemoved;
        }
    }

    private void Update()
    {
        // --- 2. LISTEN FOR INPUT ---
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
            
            // Lock Cursor (assuming First Person / Third Person game)
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

    private void HandleInventoryChanged(ItemData data, PlayerInventory.ItemStack stack)
    {
        if (isInventoryOpen) RefreshInventoryDisplay();
        if (currentSelectedItem == data) UpdateDescription(data);
    }

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
        // Clear slots
        foreach(var slot in itemSlots)
        {
            slot.style.backgroundImage = null; 
            slot.userData = null;
        }

        // Fill slots
        int index = 0;
        foreach(var kvp in playerInventory.Items)
        {
            if(index >= itemSlots.Count) break;
            ItemData data = kvp.Key;
            
            VisualElement slot = itemSlots[index];
            if (data.icon != null) slot.style.backgroundImage = new StyleBackground(data.icon);
            slot.userData = data; 
            index++;
        }
    }

    private void OnSlotClicked(VisualElement slot)
    {
        if (slot.userData is ItemData data)
        {
            currentSelectedItem = data;
            UpdateDescription(data);
        }
    }

    private void UpdateDescription(ItemData data)
    {
        if (data == null) return;
        int currentStacks = playerInventory.GetItemCount(data);
        string finalDescription = data.GetFormattedDescription(currentStacks, useDynamicDescriptions);
        
        if (useDynamicDescriptions && currentStacks > 1)
             finalDescription += $"\n(Total for {currentStacks} stacks)";

        descriptionLabel.text = finalDescription;
    }
}