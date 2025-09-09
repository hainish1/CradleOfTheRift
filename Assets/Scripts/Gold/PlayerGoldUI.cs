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

        this.playerGold.goldChanged += this.OnGoldChanged;
    }

    private void OnGoldChanged(int gold) {
        this.goldLabel.text = gold.ToString();
    }
}
