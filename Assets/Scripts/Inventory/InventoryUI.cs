using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] Transform itemGridParent;
    [SerializeField] GameObject itemSlotPrefab;
    [SerializeField] int maxVisibleItems = 20;

    [Header("Rarity colors")]
    [SerializeField] Color commonColor = Color.white;
    [SerializeField] Color uncommonColor = Color.green;
    [SerializeField] Color rareColor = Color.red;
    [SerializeField] Color legendaryColor = Color.yellow;

    private PlayerInventory inventory;
    private Dictionary<ItemData, ItemSlot> itemSlots = new Dictionary<ItemData, ItemSlot>();

    void Start()
    {
        // Find the inventory first
        // inventory = FindObjectOfType<PlayerInventory>();
        inventory = FindFirstObjectByType<PlayerInventory>();
        if (inventory == null)
        {
            Debug.LogError("InventoryUI: No PlayerInventory found!");
            return;
        }

        // subscribe to my youtube channel
        inventory.OnItemAdded += OnItemAdded;
        inventory.OnItemRemoved += OnItemRemoved;
        inventory.OnItemStackChanged += OnItemStackChanged;
    }

    void OnDestroy()
    {
        // Unsubscribe 
        if (inventory != null)
        {
            inventory.OnItemAdded -= OnItemAdded;
            inventory.OnItemRemoved -= OnItemRemoved;
            inventory.OnItemStackChanged -= OnItemStackChanged;
        }
    }

    private void OnItemAdded(ItemData itemData, PlayerInventory.ItemStack stack)
    {
        CreateItemSlot(itemData, stack);
    }

    private void OnItemRemoved(ItemData itemData)
    {
        if (itemSlots.TryGetValue(itemData, out ItemSlot slot))
        {
            Destroy(slot.gameObject);
            itemSlots.Remove(itemData);
        }
    }

    private void OnItemStackChanged(ItemData itemData, PlayerInventory.ItemStack stack)
    {
        if (itemSlots.TryGetValue(itemData, out ItemSlot slot))
        {
            slot.UpdateStack(stack.count);
        }
    }

    private void CreateItemSlot(ItemData itemData, PlayerInventory.ItemStack stack)
    {
        if (itemSlots.Count >= maxVisibleItems)
        {
            Debug.LogWarning("Max items reached!");
            return;
        }

        GameObject slotObj = Instantiate(itemSlotPrefab, itemGridParent);
        ItemSlot itemSlot = slotObj.GetComponent<ItemSlot>();

        if (itemSlot != null)
        {
            Color rarityColor = GetRarityColor(itemData.rarity);
            itemSlot.Initialize(itemData, stack.count, rarityColor);
            itemSlots.Add(itemData, itemSlot);
        }
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
