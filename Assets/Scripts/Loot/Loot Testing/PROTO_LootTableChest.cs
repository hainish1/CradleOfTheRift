using System;
using UnityEngine;

[RequireComponent(typeof(LootTable))]
public class PROTO_LootTableChest : MonoBehaviour
{
    LootTable lootTable;
    private void OnCollisionEnter(Collision other)
    {
        print("Some collision!");
        if (other.gameObject.CompareTag("Player"))
        {
            print("Colliding!");
            lootTable.DoDrop();
            this.gameObject.SetActive(false);
        }
    }
}
