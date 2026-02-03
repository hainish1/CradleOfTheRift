using System;
using UnityEngine;
/// <summary>
/// A storage class for associating a prefab Item with a percentage chance.
/// </summary>
[Serializable]
public class LootTableItem
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private GameObject lootItemPrefab;
    [SerializeField] private int dropPercent;

    [SerializeField] private bool requiresUnlock;
    public bool RequiresUnlock => requiresUnlock;
    public ItemData ItemData => itemData;

    public int GetDropPercent()
    {
        return dropPercent;
    }

    public GameObject GetLootItemPrefab()
    {
        return lootItemPrefab;
    }

}
