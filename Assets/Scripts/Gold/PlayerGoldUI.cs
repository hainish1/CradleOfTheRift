using System;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerGoldUI : MonoBehaviour
{
    private Label goldLabel;
    [SerializeField]
    private PlayerGold playerGold;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        string labelName = "GoldLabel";
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        this.goldLabel = root.Q<Label>(name: labelName);

        // find the player
        if(playerGold == null)
        {
            playerGold = PlayerLocator.FindPlayerComponent<PlayerGold>();
        }


        // if its still null then something pupu going on
        if(playerGold == null)
        {
            Debug.Log("PLayerGold UI is not assigned, or player not found or sum");
            return;
        }

        this.playerGold.goldChanged += this.OnGoldChanged;
        OnGoldChanged(playerGold.Gold);
    }

    private void OnDisable()
    {
        if (playerGold != null)
        {
            playerGold.goldChanged -= OnGoldChanged;
        }
    }

    private void OnGoldChanged(int gold) {
        this.goldLabel.text = $"Gold: {gold.ToString()}";
    }
}
