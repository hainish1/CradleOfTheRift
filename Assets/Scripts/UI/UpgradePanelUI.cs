using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UpgradePanelUI : MonoBehaviour
{
    private UIDocument document;
    private VisualElement overlay;
    private VisualElement choicesContainer;
    private Button rerollButton;

    void Awake()
    {
        document = GetComponent<UIDocument>();
        var root = document.rootVisualElement;

        overlay = root.Q<VisualElement>("UpgradeOverlay");
        choicesContainer = root.Q<VisualElement>("ChoicesContainer");
        rerollButton = root.Q<Button>("RerollButton");

        var closeButton = root.Q<Button>("CloseButton");
        if (closeButton != null)
            closeButton.RegisterCallback<ClickEvent>(OnCloseClicked);

        if (rerollButton != null)
            rerollButton.RegisterCallback<ClickEvent>(OnRerollClicked);

        Hide();
    }

    void Start()
    {
        // upgrade level manager runs first
        if (UpgradeLevelManager.Instance != null)
        {
            UpgradeLevelManager.Instance.UpgradePanelOpened += Show;
            UpgradeLevelManager.Instance.UpgradePanelClosed += Hide;
        }
        else
            Debug.LogWarning("UpgradeLevelManager.Instance is null");
    }

    void OnDestroy()
    {
        if (UpgradeLevelManager.Instance != null)
        {
            UpgradeLevelManager.Instance.UpgradePanelOpened -= Show;
            UpgradeLevelManager.Instance.UpgradePanelClosed -= Hide;
        }
    }

    public void Show(List<ItemData> choices)
    {
        // clear old cards
        choicesContainer.Clear();

        // make a card for each choice
        for (int i = 0; i < choices.Count; i++)
        {
            int index = i; 
            ItemData item = choices[i];
            VisualElement card = BuildCard(item, index);
            choicesContainer.Add(card);
        }

        UpdateRerollButtonText();
        overlay.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        overlay.style.display = DisplayStyle.None;
    }

    private VisualElement BuildCard(ItemData item, int choiceIndex)
    {
        var card = new VisualElement();
        card.AddToClassList("upgrade-card");

        // border color
        Color rarityCol = item.rarityColor;
        card.style.borderTopColor = rarityCol;
        card.style.borderBottomColor = rarityCol;
        card.style.borderLeftColor = rarityCol;
        card.style.borderRightColor = rarityCol;

        // icon
        if (item.icon != null)
        {
            var icon = new VisualElement();
            icon.AddToClassList("upgrade-icon");
            icon.style.backgroundImage = new StyleBackground(item.icon);
            card.Add(icon);
        }

        // item name
        var nameLabel = new Label(item.itemName);
        nameLabel.AddToClassList("upgrade-name");
        nameLabel.style.color = rarityCol;
        card.Add(nameLabel);

        // rarity 
        var rarityLabel = new Label(item.rarity.ToString());
        rarityLabel.AddToClassList("upgrade-rarity");
        rarityLabel.style.color = rarityCol;
        card.Add(rarityLabel);

        var divider = new VisualElement();
        divider.AddToClassList("upgrade-divider");
        divider.style.backgroundColor = rarityCol;
        card.Add(divider);

        // Description
        if (!string.IsNullOrEmpty(item.description))
        {
            var desc = new Label(item.GetFormattedDescription(1, false));
            desc.AddToClassList("upgrade-description");
            card.Add(desc);
        }

        // click handle
        card.RegisterCallback<ClickEvent>(evt =>
        {
            Hide();
            if (UpgradeLevelManager.Instance != null)
                UpgradeLevelManager.Instance.SelectUpgrade(choiceIndex);
        });

        return card;
    }

    private void OnRerollClicked(ClickEvent evt)
    {
        UpgradeLevelManager.Instance.RerollChoices();
        UpdateRerollButtonText();
    }

    private void UpdateRerollButtonText()
    {
        if (rerollButton == null || UpgradeLevelManager.Instance == null) return;
        int n = UpgradeLevelManager.Instance.RerollCountRemaining;
        rerollButton.text = $"Reroll ({n})";
        rerollButton.SetEnabled(n > 0);
    }

    private void OnCloseClicked(ClickEvent evt)
    {
        // idk man ill have jared do that
        UpgradeLevelManager.Instance.CloseUpgradePanel();
    }

    // private string FormatEffectName(ItemEffectKind kind)
    // {
    //     string nameStr = kind.ToString();
    //     var nameStringBuilder = new System.Text.StringBuilder(nameStr.Length + 8);
    //     for (int i = 0; i < nameStr.Length; i++)
    //     {
    //         if (i > 0 && char.IsUpper(nameStr[i]))
    //             nameStringBuilder.Append(' ');
    //         nameStringBuilder.Append(nameStr[i]);
    //     }
    //     return nameStringBuilder.ToString();
    // }
}