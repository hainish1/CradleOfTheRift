using UnityEngine;
using UnityEngine.UIElements;

// assign to UIDocument
public class ItemPickupTooltipUI : MonoBehaviour
{
    public static ItemPickupTooltipUI Instance { get; private set; }

    private UIDocument document;
    private VisualElement tooltipRoot;
    private VisualElement tooltipBox;
    private VisualElement tooltipBody;
    private Label pickupHint;
    private Label pickupAction;
    private Label rarityLabel;
    private Label itemNameLabel;
    private Label itemDescription;

    private Transform ourItem;
    private Camera mainCam;

    [Header("Settings")]
    [Tooltip("Position above the item")]
    [SerializeField] private float yOffset = 1.5f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        document = GetComponent<UIDocument>();
        var root = document.rootVisualElement;

        tooltipRoot = root.Q<VisualElement>("TooltipRoot");
        tooltipBox = root.Q<VisualElement>("TooltipBox");
        tooltipBody = root.Q<VisualElement>("TooltipBody");
        pickupHint = root.Q<Label>("PickupHint");
        pickupAction = root.Q<Label>("PickupAction");
        rarityLabel = root.Q<Label>("RarityLabel");
        itemNameLabel = root.Q<Label>("ItemName");
        itemDescription = root.Q<Label>("ItemDescription");

        Hide(); // hide default
    }

    void Start()
    {
        mainCam = Camera.main;
    }

    void LateUpdate()
    {
        if (ourItem == null || mainCam == null)
        {
            Hide();
            return;
        }

        // item world position to screen
        Vector3 worldPos = ourItem.position + Vector3.up * yOffset;
        Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);

        if (screenPos.z < 0)
        {
            tooltipBox.style.display = DisplayStyle.None;
            return;
        }

        tooltipBox.style.display = DisplayStyle.Flex;

        float uiX = screenPos.x;
        float uiY = Screen.height - screenPos.y;

        // center tooltip horizontally on the item
        tooltipBox.style.left = uiX;
        tooltipBox.style.top = uiY;
        tooltipBox.style.translate = new Translate(Length.Percent(-50), Length.Percent(-100));
    }

    public void Show(ItemData itemData, Transform itemTransform)
    {
        if (itemData == null) return;

        ourItem = itemTransform;

        // rarity
        rarityLabel.text = itemData.rarity.ToString();
        rarityLabel.style.color = itemData.rarityColor;

        // item name
        itemNameLabel.text = itemData.itemName.ToUpper();

        // description
        itemDescription.text = itemData.description;

        // border color -rarity
        tooltipBody.style.borderTopColor = itemData.rarityColor;
        tooltipBody.style.borderBottomColor = itemData.rarityColor;
        tooltipBody.style.borderLeftColor = itemData.rarityColor;
        tooltipBody.style.borderRightColor = itemData.rarityColor;

        tooltipBox.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        if (tooltipBox != null)
            tooltipBox.style.display = DisplayStyle.None;
        ourItem = null;
    }
}
