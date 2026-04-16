using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class InventoryStripUI : MonoBehaviour
{
    [SerializeField] private float maxSlotSize = 54f;
    [SerializeField] private float minSlotSize = 26f;
    [SerializeField] private float slotGap = 8f;

    private PlayerInventory inventory;
    private VisualElement stripRoot;
    private VisualElement content;

    private readonly Dictionary<ItemData, VisualElement> slotsByItem = new();
    private readonly Dictionary<ItemData, Label> countLabelsByItem = new();
    private readonly List<ItemData> displayOrder = new();

    private void Awake()
    {
        CacheUi();
    }

    private void OnEnable()
    {
        CacheUi();

        if (content != null)
            content.RegisterCallback<GeometryChangedEvent>(OnContentGeometryChanged);

        BindInventory();
        RefreshFromInventory();
    }

    private void Start()
    {
        BindInventory();
        RefreshFromInventory();
    }

    private void Update()
    {
        if (inventory != null)
            return;

        BindInventory();
        RefreshFromInventory();
    }

    private void OnDisable()
    {
        if (content != null)
            content.UnregisterCallback<GeometryChangedEvent>(OnContentGeometryChanged);

        if (inventory != null)
        {
            inventory.OnItemAdded -= OnItemAdded;
            inventory.OnItemStackChanged -= OnItemStackChanged;
            inventory.OnItemRemoved -= OnItemRemoved;
            inventory = null;
        }
    }

    private void CacheUi()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        stripRoot = root.Q<VisualElement>("PassiveInventoryStripRoot");
        content = root.Q<VisualElement>("PassiveInventoryContent");

        if (stripRoot == null || content == null)
        {
            Debug.LogWarning("[InventoryStripUI] Passive inventory strip elements were not found in the active HUD document.");
            return;
        }

        stripRoot.style.display = DisplayStyle.None;
    }

    private void BindInventory()
    {
        if (inventory != null)
            return;

        inventory = FindFirstObjectByType<PlayerInventory>();
        if (inventory == null)
            return;

        inventory.OnItemAdded += OnItemAdded;
        inventory.OnItemStackChanged += OnItemStackChanged;
        inventory.OnItemRemoved += OnItemRemoved;
    }

    private void RefreshFromInventory()
    {
        if (content == null || inventory == null)
            return;

        foreach (var slot in slotsByItem.Values)
            slot.RemoveFromHierarchy();

        slotsByItem.Clear();
        countLabelsByItem.Clear();
        displayOrder.Clear();

        foreach (var pair in inventory.Items)
            CreateOrUpdateSlot(pair.Key, pair.Value);

        RefreshLayout();
    }

    private void OnItemAdded(ItemData itemData, PlayerInventory.ItemStack stack)
    {
        CreateOrUpdateSlot(itemData, stack);
        RefreshLayout();
    }

    private void OnItemStackChanged(ItemData itemData, PlayerInventory.ItemStack stack)
    {
        CreateOrUpdateSlot(itemData, stack);
    }

    private void OnItemRemoved(ItemData itemData)
    {
        if (slotsByItem.TryGetValue(itemData, out var slot))
        {
            slot.RemoveFromHierarchy();
            slotsByItem.Remove(itemData);
        }

        countLabelsByItem.Remove(itemData);
        displayOrder.Remove(itemData);
        RefreshLayout();
    }

    private void CreateOrUpdateSlot(ItemData itemData, PlayerInventory.ItemStack stack)
    {
        if (content == null || itemData == null)
            return;

        if (!slotsByItem.TryGetValue(itemData, out var slot))
        {
            slot = BuildSlot(itemData);
            slotsByItem[itemData] = slot;
            displayOrder.Add(itemData);
            content.Add(slot);
        }

        UpdateStackCount(itemData, stack);
    }

    private VisualElement BuildSlot(ItemData itemData)
    {
        var slot = new VisualElement
        {
            name = $"PassiveInventorySlot_{SanitizeName(itemData.itemName)}",
            pickingMode = PickingMode.Ignore
        };
        slot.AddToClassList("passive-inventory-slot");

        var icon = new VisualElement
        {
            name = "PassiveInventoryIcon",
            pickingMode = PickingMode.Ignore
        };
        icon.AddToClassList("passive-inventory-icon");
        if (itemData.icon != null)
            icon.style.backgroundImage = new StyleBackground(itemData.icon);

        var countLabel = new Label
        {
            name = "PassiveInventoryCount",
            pickingMode = PickingMode.Ignore
        };
        countLabel.AddToClassList("passive-inventory-count");

        slot.Add(icon);
        slot.Add(countLabel);
        countLabelsByItem[itemData] = countLabel;
        return slot;
    }

    private void UpdateStackCount(ItemData itemData, PlayerInventory.ItemStack stack)
    {
        if (!countLabelsByItem.TryGetValue(itemData, out var countLabel))
            return;

        int count = stack != null ? stack.count : 0;
        countLabel.text = count > 1 ? count.ToString() : string.Empty;
        countLabel.style.display = count > 1 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void RefreshLayout()
    {
        if (stripRoot == null || content == null)
            return;

        int itemCount = displayOrder.Count;
        stripRoot.style.display = itemCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;

        if (itemCount == 0)
            return;

        float availableWidth = content.resolvedStyle.width;
        if (availableWidth <= 0f)
            return;

        float computedGap = itemCount > 1 ? slotGap : 0f;
        float slotSize = (availableWidth - (itemCount - 1) * computedGap) / itemCount;
        slotSize = Mathf.Clamp(slotSize, minSlotSize, maxSlotSize);

        if (slotSize <= minSlotSize && itemCount > 1)
        {
            computedGap = Mathf.Max(2f, (availableWidth - itemCount * slotSize) / (itemCount - 1));
        }

        for (int i = 0; i < displayOrder.Count; i++)
        {
            ItemData itemData = displayOrder[i];
            if (!slotsByItem.TryGetValue(itemData, out var slot))
                continue;

            slot.style.width = slotSize;
            slot.style.height = slotSize;
            slot.style.marginRight = i < displayOrder.Count - 1 ? computedGap : 0f;
        }
    }

    private void OnContentGeometryChanged(GeometryChangedEvent evt)
    {
        RefreshLayout();
    }

    private static string SanitizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Item";

        return value.Replace(" ", "_");
    }
}
