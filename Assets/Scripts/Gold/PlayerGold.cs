using System;
using UnityEngine;

public class PlayerGold : MonoBehaviour
{
    public event Action<int> goldChanged;
    private int gold = 0;
    public int Gold
    {
        get => this.gold;
        private set
        {
            this.gold = value;
            this.goldChanged.Invoke(gold);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            this.AddGold(10);
        }

        if (Input.GetKeyDown(KeyCode.V)) {
            this.SpendGold(30);
        }
    }

    public void AddGold(int amount)
    {
        this.Gold += amount;
    }

    public bool SpendGold(int cost)
    {
        if (this.Gold >= cost)
        {
            this.Gold -= cost;

            return false;
        }

        return true;
    }
}
