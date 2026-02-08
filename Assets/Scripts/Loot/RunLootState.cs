using System.Collections.Generic;
using UnityEngine;

public class RunLootState : MonoBehaviour
{
    public static RunLootState Instance { get; private set; }

    private readonly HashSet<string> unlockedItemIds = new();

    private readonly HashSet<string> blockedItemIds = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ResetForNewRun()
    {
        unlockedItemIds.Clear();
        blockedItemIds.Clear();
    }

    public void Unlock(ItemData item)
    {
        if (item == null) return;
        if (string.IsNullOrWhiteSpace(item.itemId)) return;
        unlockedItemIds.Add(item.itemId);
    }

    public bool IsUnlocked(ItemData item)
    {
        if (item == null) return false;
        if (string.IsNullOrWhiteSpace(item.itemId)) return false;
        return unlockedItemIds.Contains(item.itemId);
    }

    public void Block(ItemData item)
    {
        if(item == null) return;
        if(string.IsNullOrWhiteSpace(item.itemId)) return;
        blockedItemIds.Add(item.itemId);
    }

    public bool IsBlocked(ItemData item)
    {
        if(item == null) return false;
        if(string.IsNullOrWhiteSpace(item.itemId)) return false;
        return blockedItemIds.Contains(item.itemId);
    }
}