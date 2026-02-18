using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UpgradePanelUI : MonoBehaviour
{
    private UIDocument document;
    private VisualElement overlay;
    private VisualElement choicesContainer;

    void Awake()
    {
        document = GetComponent<UIDocument>();
        var root = document.rootVisualElement;

        overlay = root.Q<VisualElement>("UpgradeOverlay");
        choicesContainer = root.Q<VisualElement>("ChoicesContainer");

        Hide();
    }

    void Start()
    {
        // upgrade level manager runs first
        if (UpgradeLevelManager.Instance != null)
            UpgradeLevelManager.Instance.UpgradePanelOpened += Show;
        else
            Debug.LogWarning("UpgradeLevelManager.Instance is null");
    }

    void OnDestroy()
    {
        if (UpgradeLevelManager.Instance != null)
            UpgradeLevelManager.Instance.UpgradePanelOpened -= Show;
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

        // effects list, shows name and its own description
        if (item.effects != null && item.effects.Count > 0)
        {
            foreach (var effect in item.effects)
            {
                if (effect.kind == ItemEffectKind.None) continue;

                // effect name
                var effectName = new Label(FormatEffectName(effect.kind));
                effectName.AddToClassList("upgrade-effect-name");
                effectName.style.color = rarityCol; // smae color as rarity
                card.Add(effectName);

                // effect description
                if (!string.IsNullOrEmpty(effect.description))
                {
                    var effectDesc = new Label(effect.description);
                    effectDesc.AddToClassList("upgrade-effect-desc");
                    card.Add(effectDesc);
                }
            }
        }

        // stat mods list
        if (item.statMods != null && item.statMods.Count > 0)
        {
            foreach (var mod in item.statMods)
            {
                string sign = mod.operatorType == OperatorType.Add ? "+" : "×";
                string value = mod.operatorType == OperatorType.Percentage
                    ? $"{mod.value * 100f:F0}%"
                    : $"{sign}{mod.value:F1}";
                var modLabel = new Label($"• {mod.statType} {value}");
                modLabel.AddToClassList("upgrade-effect-desc");
                card.Add(modLabel);
            }
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

    private string FormatEffectName(ItemEffectKind kind)
    {
        string nameStr = kind.ToString();
        var nameStringBuilder = new System.Text.StringBuilder(nameStr.Length + 8);
        for (int i = 0; i < nameStr.Length; i++)
        {
            if (i > 0 && char.IsUpper(nameStr[i]))
                nameStringBuilder.Append(' ');
            nameStringBuilder.Append(nameStr[i]);
        }
        return nameStringBuilder.ToString();
    }
}