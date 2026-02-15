using UnityEngine;
using UnityEngine.UIElements;

public class MenuController : MonoBehaviour
{
    [Header("Page Controllers")]
    [SerializeField] private InventoryPageController inventoryController;
    [SerializeField] private AbilityPageController abilityController;

    private VisualElement root;
    private VisualElement inventoryPage;
    private VisualElement abilitiesPage;

    private void OnEnable()
    {
        // 1. Hook into the UI Document
        root = GetComponent<UIDocument>().rootVisualElement;

        // 2. Find the Page Containers (defined in your Shell XML)
        inventoryPage = root.Q<VisualElement>("InventoryPage");
        abilitiesPage = root.Q<VisualElement>("AbilitiesPage");

        // 3. Initialize the Sub-Controllers
        // We pass them their specific "slice" of the UI so they don't touch each other
        inventoryController.Initialize(inventoryPage);
        abilityController.Initialize(abilitiesPage);

        // 4. Setup Tab Buttons
        root.Q<Button>("TabInvBtn").RegisterCallback<ClickEvent>(evt => SwitchToInventory());
        root.Q<Button>("TabAbilityBtn").RegisterCallback<ClickEvent>(evt => SwitchToAbilities());

        // Default to Inventory when first opened
        SwitchToInventory();
    }

    public void SwitchToInventory()
    {
        // Visual Toggle
        inventoryPage.style.display = DisplayStyle.Flex;
        abilitiesPage.style.display = DisplayStyle.None;

        // Logic Toggle
        // (Optional: You can add .OnShow() methods to your controllers if they need to refresh data)
    }

    public void SwitchToAbilities()
    {
        inventoryPage.style.display = DisplayStyle.None;
        abilitiesPage.style.display = DisplayStyle.Flex;
        
        // This ensures the video player wakes up correctly when we switch tabs
        abilityController.OnShow(); 
    }
}