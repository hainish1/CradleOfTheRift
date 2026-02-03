using System;
using System.Collections.Generic;


[Serializable]
public class InventorySaveData
{
    public List<InventoryItemEntry> entries = new();
}

[Serializable]
public class InventoryItemEntry
{
    public string itemId;
    public int count;
}