using System;
using UnityEngine;

public class PlayerGold : MonoBehaviour
{
    public static PlayerGold Instance { get; private set; }
    public event Action<int> goldChanged;
    public int gold = 30;
    public int Gold
    {
        get => this.gold;
        private set
        {
            this.gold = value;
            this.goldChanged?.Invoke(gold);
        }
    }
    public void AddGold(int amount)
    {
        this.Gold += amount;
        Debug.Log("Added " + amount + " gold. Total: " + this.Gold);
    }

    public bool SpendGold(int cost)
    {
        if (this.Gold >= cost)
        {
            this.Gold -= cost;

            return true;
        }

        return false;
    }

    void Awake()
    {
        Instance = this;
    }
}
