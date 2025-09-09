using UnityEngine;

public class PlayerGold : MonoBehaviour
{

    public int Gold { get; private set; }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    public void AddGold(int amount)
    {
        this.Gold += amount;
    }

    public bool SpendGold(int cost)
    {
        if (this.Gold > cost)
        {
            this.Gold -= cost;

            return false;
        }

        return true;
    }
}
