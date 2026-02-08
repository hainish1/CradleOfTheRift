using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Handles a list of probable loot drops, gold, and dropping Items.
/// </summary>
public class LootTable : MonoBehaviour
{
    [Header("Gold Drops")]
    [SerializeField]
    private int minimumGold;
    [SerializeField]
    private int maximumGold;
    [Header("Item Drops")]
    [SerializeField]
    private List<LootTableItem> lootTable;
    [Header("Player Reference")]
    [SerializeField]
    private PlayerGold playerGold;

    /// <summary>
    /// Gives the player gold associated with the loot table
    /// And instantiates a randomly selected prefab from the loot table.
    /// </summary>
    public void DoDrop(PlayerInventory inv)
    {
        Debug.Log("Dropping");
        if (playerGold != null)
            GivePlayerGold();
        DropItem(inv);
    }

    /// <summary>
    /// Gives the player gold somewhere between the range of minimumGold and maximumGold.
    /// </summary>
    private void GivePlayerGold()
    {
        int goldAmount = Random.Range(minimumGold, maximumGold);
        playerGold.AddGold(goldAmount);
    }

    /// <summary>
    /// Randomly selects an item from the table and instantiates its prefab.
    /// </summary>
    private void DropItem(PlayerInventory inv)
    {
        // If there are no items to drop, don't even try!
        if (lootTable.Count < 1) return;
        GameObject rolledItem = RollItem(inv);
        if(rolledItem == null) return; // no need to drop that shits
        // Spawn in the Prefab.
        // Instantiate(rolledItem, transform.position, Quaternion.identity);

        // Define how far away from the chest the item should spawn
        float spawnRadius = 1.0f; // Adjust this value as needed

        // Get a random direction vector on the XZ plane
        Vector3 randomDirection = Random.insideUnitCircle.normalized;
        Vector3 spawnOffset = new Vector3(randomDirection.x, 0, randomDirection.y) * spawnRadius;

        Vector3 spawnPosition = transform.position + spawnOffset;
        spawnPosition.y += 2f; // a hardcode fix for now

        Instantiate(rolledItem, spawnPosition, Quaternion.identity);

    }

    /// <summary>
    /// Randomly selects a prefab from the Loot Table.
    /// </summary>
    /// <returns>The randomly selected prefab.</returns>
    private GameObject RollItem(PlayerInventory inv)
    {
        // Sometimes designers can't help themselves,
        // and they give us a loot table where the
        // percentages of every item doesn't add up to 100.
        // This is to avoid that moment.
        int totalOdds = 0;
        var runLoot = RunLootState.Instance;
        var eligible = new List<LootTableItem>();

        foreach (var entry in lootTable)
        {
            if (entry == null) continue;

            // Find the ItemData from the prefab 
            // ItemData data = null;
            ItemData data = entry.ItemData;

            // infer from prefab is data not set
            if (data == null)
            {
                var pickup = entry.GetLootItemPrefab() != null
                    ? entry.GetLootItemPrefab().GetComponent<ItemPickup>()
                    : null;
                if (pickup != null)
                {
                    data = pickup.ItemData;
                }
            }

            // if items cannot spawn check that first
            if (runLoot != null && data != null && runLoot.IsBlocked(data))
                continue;

            // Gate “locked” items behind global unlocks
            if (entry.RequiresUnlock)
            {
                if (runLoot == null || data == null || !runLoot.IsUnlocked(data))
                    continue;
            }

            // RoR2 stacking: duplicates allowed, but avoid rolling past maxStacks
            if (inv != null && data != null && data.canStack && inv.GetItemCount(data) >= data.maxStacks)
                continue;

            eligible.Add(entry);
            totalOdds += entry.GetDropPercent();
        }

        // foreach (LootTableItem item in lootTable)
        // {
        //     totalOdds += item.GetDropPercent();
        // }
        // int rolledNumber = Random.Range(0, totalOdds);

        // // Now go through the odds of each item and return the one that is selected.
        // // This is kind of like a probability bucket.
        // // I'm not entirely sure how to explain that better...
        // int countingBucket = 0;
        // foreach (LootTableItem item in lootTable)
        // {
        //     countingBucket += item.GetDropPercent();
        //     if (rolledNumber <= countingBucket)
        //         return item.GetLootItemPrefab();
        // }

        if (eligible.Count == 0) return null; // fallback
        int roll = Random.Range(0, totalOdds);
        int bucket = 0;

        foreach (var entry in eligible)
        {
            bucket += entry.GetDropPercent();
            if (roll <= bucket)
            {
                return entry.GetLootItemPrefab();
            }
        }
        // If we haven't returned anything at this point, something has gone pretty wrong!
        // Just return the first LootTableItem!
        return lootTable[0].GetLootItemPrefab();
    }
}
