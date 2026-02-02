using System.Collections.Generic;
using UnityEngine;

// this class helps map itemId to ItemData

[CreateAssetMenu(fileName = "Item Catalog", menuName = "Items/Item Catalog")]
public class ItemCatalog : ScriptableObject
{
    public List<ItemData> allItems = new();

    private Dictionary<string, ItemData> lookup;

    public ItemData GetById(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return null;
        EnsureLookup();
        return lookup.TryGetValue(itemId, out var item) ? item : null;
    }

    private void EnsureLookup()
    {
        if (lookup != null) return;
        lookup = new Dictionary<string, ItemData>();

        foreach (var item in allItems)
        {
            if (item == null) continue;
            if (string.IsNullOrWhiteSpace(item.itemId)) continue;
            if (!lookup.ContainsKey(item.itemId))
                lookup.Add(item.itemId, item);
        }
    }


}