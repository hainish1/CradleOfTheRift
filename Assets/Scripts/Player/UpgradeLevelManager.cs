using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// This class checks if Level up is available, and shows an upgrade selection panel when the player presses 
/// the upgrade keybind. Pause the game, and resumes after somethign is picked.
/// </summary>
public class UpgradeLevelManager : MonoBehaviour
{

    public static UpgradeLevelManager Instance { get; private set; }

    [Header("Upgrade Pool")]
    [Tooltip("Base upgrade pool, always available from the start.")]
    [SerializeField] private List<ItemData> upgradePool = new();

    // items unlocked at runtime via InventoryRule (AddToUpgradePool )
    private readonly List<ItemData> runtimePool = new();

    // items blocked at runtime via InventoryRule (RemoveFromUpgradePool)
    private readonly HashSet<ItemData> blockedFromPool = new();

    [Header("Input")]
    [SerializeField] private Key activateKey = Key.U;

    [Header("Upgrade Options")]
    [Tooltip("Max number of upgrade choices shown each time panel open")]
    [SerializeField] private int maxChoices = 3;

    private bool levelUpPending;
    private PlayerXP playerXP;

    // track which upgrades have already been chosen 
    private readonly HashSet<ItemData> chosenUpgrades = new();

    // the choices offered to the player currently
    private List<ItemData> currentChoices = new();

    // Events
    public event Action<List<ItemData>> UpgradePanelOpened;   // pass the choices to UI
    public event Action<int> UpgradeSelected;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        playerXP = PlayerXP.Instance;

        if (playerXP != null)
            playerXP.LevelUpAvailable += OnLevelUpAvailable;
        else
            Debug.LogWarning("PlayerXP not found.");
    }

    void OnDestroy()
    {
        if (playerXP != null)
            playerXP.LevelUpAvailable -= OnLevelUpAvailable;
    }

    void Update()
    {
        if (!levelUpPending) return;

        if (Keyboard.current != null && Keyboard.current[activateKey].wasPressedThisFrame)
        {
            OpenUpgradePanel();
        }
    }

    private void OnLevelUpAvailable()
    {
        levelUpPending = true;
        Debug.Log($"Level Up available. Press '{activateKey}'");
    }

    private void OpenUpgradePanel()
    {
        //random choices from remaining pool
        currentChoices = PickRandomUpgrades();

        if (currentChoices.Count == 0)
        {
            // auto consume level up, no upgrades since no choices
            if (PlayerXP.Instance != null)
                PlayerXP.Instance.ConsumeLevelUp();
            levelUpPending = false;
            return;
        }

        levelUpPending = false;

        Time.timeScale = 0f;
        PauseManager.GameIsPaused = true;
        PauseManager.CurrentPauseState = PauseManager.PauseState.Inventory; // im gonna reuse this for a bit

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        UpgradePanelOpened?.Invoke(currentChoices);
        Debug.Log($"Upgrade panel opened with {currentChoices.Count} choices.");
    }


    // Called by the UI when the player clicks a choice button.
    public void SelectUpgrade(int choiceIndex)
    {
        if (choiceIndex < 0 || choiceIndex >= currentChoices.Count)
        {
            return;
        }

        ItemData chosen = currentChoices[choiceIndex];
        Debug.Log($"Player selected upgrade: {chosen.itemName}");

        // mark as chosen so it does not appear again
        chosenUpgrades.Add(chosen);

        // apply the item to the player's inventory effects ans stats
        PlayerInventory inventory = PlayerLocator.FindPlayerComponent<PlayerInventory>();
        if (inventory != null)
        {
            inventory.AddItem(chosen);
            Debug.Log($"Applied upgrade '{chosen.itemName}' to inventory.");
        }
        else
        {
            Debug.LogWarning("PlayerInventory not found");
        }

        UpgradeSelected?.Invoke(choiceIndex);

        // Consume the level up
        if (PlayerXP.Instance != null)
            PlayerXP.Instance.ConsumeLevelUp();

        // Resume game
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        PauseManager.CurrentPauseState = PauseManager.PauseState.None;
        PauseManager.GameIsPaused = false;
        Time.timeScale = 1f;
    }

    // added weight per rarity tier 
    private static int RarityWeight(ItemRarity r) => r switch
    {
        ItemRarity.Common    => 60,
        ItemRarity.Uncommon  => 25,
        ItemRarity.Rare      => 12,
        ItemRarity.Legendary =>  3,
        _                    =>  1,
    };

    // picks weighted-random choices from pool that have not been chosen yet
    private List<ItemData> PickRandomUpgrades()
    {
        var available = new List<ItemData>();
        int totalWeight = 0;

        // combine pool(do we need this?)
        var combined = new List<ItemData>(upgradePool);
        foreach (var item in runtimePool)
            if (!combined.Contains(item)) combined.Add(item);

        foreach (var item in combined)
        {
            if (item != null && !chosenUpgrades.Contains(item) && !blockedFromPool.Contains(item))
            {
                available.Add(item);
                totalWeight += RarityWeight(item.rarity);
            }
        }

        var result = new List<ItemData>();
        int count = Mathf.Min(maxChoices, available.Count);

        while (result.Count < count && available.Count > 0)
        {
            int roll = UnityEngine.Random.Range(0, totalWeight);
            int bucket = 0;
            for (int i = 0; i < available.Count; i++)
            {
                bucket += RarityWeight(available[i].rarity);
                if (roll < bucket)
                {
                    result.Add(available[i]);
                    totalWeight -= RarityWeight(available[i].rarity);
                    available.RemoveAt(i);
                    break;
                }
            }
        }

        return result;
    }



    public void UnlockForUpgrade(ItemData item)
    {
        if (item == null) return;
        if (!upgradePool.Contains(item) && !runtimePool.Contains(item))
        {
            runtimePool.Add(item);
            Debug.Log($"[UpgradeLevelManager] '{item.itemName}' unlocked into upgrade pool.");
        }
    }

    public void LockFromUpgrade(ItemData item)
    {
        if (item == null) return;
        runtimePool.Remove(item);
        blockedFromPool.Add(item);
        Debug.Log($"[UpgradeLevelManager] '{item.itemName}' blocked from upgrade pool.");
    }

    public void ResetForNewRun()
    {
        chosenUpgrades.Clear();
        currentChoices.Clear();
        runtimePool.Clear();
        blockedFromPool.Clear();
        levelUpPending = false;
    }
}
