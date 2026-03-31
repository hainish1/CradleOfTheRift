using System;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerGoldUI : MonoBehaviour
{
    private Label goldLabel;
    [SerializeField]
    private PlayerGold playerGold;

    private void OnEnable()
    {
        // Setup UI References
        if (goldLabel == null)
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            goldLabel = root.Q<Label>(name: "GoldLabel");
        }

        // Find the player if not assigned
        if (playerGold == null)
        {
            playerGold = PlayerLocator.FindPlayerComponent<PlayerGold>();
        }

        // Subscribe and initialize
        if (playerGold != null)
        {
            playerGold.goldChanged += OnGoldChanged;
            OnGoldChanged(playerGold.Gold); // Set initial value
        }
        else
        {
            Debug.LogWarning("PLayerGold UI is not assigned, or player not found or sum");
        }
    }

    private void OnDisable()
    {
        if (playerGold != null)
        {
            playerGold.goldChanged -= OnGoldChanged;
        }
    }

    private void OnGoldChanged(int gold) {
        this.goldLabel.text = gold.ToString();
    }
}
