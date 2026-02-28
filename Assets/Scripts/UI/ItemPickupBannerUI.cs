using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

// UI that slides up from bottom when player picks up an item, stays there a while, and tehn goes does again
public class ItemPickupBannerUI : MonoBehaviour
{
    public static ItemPickupBannerUI Instance { get; private set; }

    [Header("Timing")]
    [Tooltip("how long the UI stays visible before going out")]
    [SerializeField] private float displayDuration = 3f;

    [Tooltip("how long transition takes")]
    [SerializeField] private float fadeOutDuration = 0.35f;

    // UI refs
    private UIDocument document;
    private VisualElement bannerBox;
    private VisualElement bannerIcon;
    private Label rarityLabel;
    private Label itemNameLabel;
    private Label descriptionLabel;
    private Coroutine hideRoutine;

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

        bannerBox = root.Q<VisualElement>("BannerBox");
        bannerIcon = root.Q<VisualElement>("BannerIcon");
        rarityLabel = root.Q<Label>("BannerRarity");
        itemNameLabel = root.Q<Label>("BannerItemName");
        descriptionLabel = root.Q<Label>("BannerDescription");

        // start hidden
        HideAtStart();
    }

    // show the UI for that Item
    public void Show(ItemData itemData)
    {
        if (itemData == null) return;

        // cancel any pending hide
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        // icon
        if (itemData.icon != null)
        {
            bannerIcon.style.backgroundImage = new StyleBackground(itemData.icon);
            bannerIcon.style.display = DisplayStyle.Flex;
        }
        else
        {
            bannerIcon.style.display = DisplayStyle.None;
        }
        // rarity
        rarityLabel.text = itemData.rarity.ToString().ToUpper();
        rarityLabel.style.color = itemData.rarityColor;
        // name
        itemNameLabel.text = itemData.itemName;

        // description
        descriptionLabel.text = itemData.description;

        // rarity color
        bannerBox.style.borderLeftColor = itemData.rarityColor;
        bannerBox.style.borderLeftWidth = 4;
        bannerBox.style.borderRightColor = itemData.rarityColor;
        bannerBox.style.borderRightWidth = 4;
        bannerBox.style.borderTopColor = itemData.rarityColor;
        bannerBox.style.borderTopWidth = 4;
        bannerBox.style.borderBottomColor = itemData.rarityColor;
        bannerBox.style.borderBottomWidth = 4;

        // reveal
        bannerBox.style.display = DisplayStyle.Flex;
        bannerBox.RemoveFromClassList("banner-box--hidden");

        // auto hide after time ends
        hideRoutine = StartCoroutine(AutoHideRoutine());
    }

    private IEnumerator AutoHideRoutine()
    {
        yield return new WaitForSecondsRealtime(displayDuration);

        // trigger fade out transition
        bannerBox.AddToClassList("banner-box--hidden");

        // wait for transition then fully hide
        yield return new WaitForSecondsRealtime(fadeOutDuration);

        HideAtStart();
        hideRoutine = null;
    }


    private void HideAtStart()
    {
        if (bannerBox == null) return;
        bannerBox.AddToClassList("banner-box--hidden");
        bannerBox.style.display = DisplayStyle.None;
    }
}
