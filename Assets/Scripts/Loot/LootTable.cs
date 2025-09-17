using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Handles a list of probable loot drops, gold, and dropping Items.
/// </summary>
public class LootTable : MonoBehaviour
{
    [Header("Gold Drops")] [SerializeField]
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
    public void DoDrop()
    {
        print("Dropping!");
        if (playerGold != null)
            GivePlayerGold();
        DropItem();
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
    private void DropItem()
    {
        // If there are no items to drop, don't even try!
        if (lootTable.Count < 1) return;
        GameObject rolledItem = RollItem();
        // Spawn in the Prefab.
        Instantiate(rolledItem, transform.position, Quaternion.identity);
    }

    /// <summary>
    /// Randomly selects a prefab from the Loot Table.
    /// </summary>
    /// <returns>The randomly selected prefab.</returns>
    private GameObject RollItem()
    {
        // Sometimes designers can't help themselves,
        // and they give us a loot table where the
        // percentages of every item doesn't add up to 100.
        // This is to avoid that moment.
        int totalOdds = 0;
        foreach (LootTableItem item in lootTable)
        {
            totalOdds += item.GetDropPercent();
        }
        int rolledNumber = Random.Range(0, totalOdds);
        
        // Now go through the odds of each item and return the one that is selected.
        // This is kind of like a probability bucket.
        // I'm not entirely sure how to explain that better...
        int countingBucket = 0;
        foreach (LootTableItem item in lootTable)
        {
            countingBucket += item.GetDropPercent();
            if (rolledNumber <= countingBucket)
                return item.GetLootItemPrefab();
        }
        // If we haven't returned anything at this point, something has gone pretty wrong!
        // Just return the first LootTableItem!
        return lootTable[0].GetLootItemPrefab();
    }
}
