using UnityEngine;

public class InventoryPersistence : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private ItemCatalog itemCatalog;

    [Header("Storage")]
    [SerializeField] private string playerPrefsKey = "player_inventory_v1";
    [SerializeField] private bool loadOnAwake = true;
    [SerializeField] private bool saveOnQuit = true;

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<PlayerInventory>();
        if (loadOnAwake) Load();
    }

    private void OnApplicationQuit()
    {
        if (saveOnQuit) Save();
    }

    public void Save()
    {
        if (inventory == null) return;

        var data = new InventorySaveData();
        foreach (var pair in inventory.Items)
        {
            var item = pair.Key;
            var stack = pair.Value;
            if (item == null) continue;
            if (string.IsNullOrWhiteSpace(item.itemId)) continue;

            data.entries.Add(new InventoryItemEntry
            {
                itemId = item.itemId,
                count = stack.count
            });
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(playerPrefsKey, json);
        PlayerPrefs.Save();
    }

    public void Load()
    {
        if (inventory == null || itemCatalog == null) return;
        if (!PlayerPrefs.HasKey(playerPrefsKey)) return;

        string json = PlayerPrefs.GetString(playerPrefsKey, "");
        if (string.IsNullOrWhiteSpace(json)) return;

        var data = JsonUtility.FromJson<InventorySaveData>(json);
        if (data?.entries == null) return;

        // Apply counts (uses your runtime logic to rebuild stats/effects)
        foreach (var entry in data.entries)
        {
            var item = itemCatalog.GetById(entry.itemId);
            if (item == null) continue;
            inventory.SetItemCount(item, entry.count);
        }
    }

    public void WipeSave()
    {
        PlayerPrefs.DeleteKey(playerPrefsKey);
    }
}