using System;
using UnityEngine;
/// <summary>
/// A storage class for associating a prefab Item with a percentage chance.
/// </summary>
[Serializable]
public class LootTableItem
{
    [SerializeField]
    private GameObject lootItemPrefab;
    [SerializeField]
    private int dropPercent;

    public int GetDropPercent()
    {
        return dropPercent;
    }

    public GameObject GetLootItemPrefab()
    {
        return lootItemPrefab;
    }
    
}
