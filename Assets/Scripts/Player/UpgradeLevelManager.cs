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
    [Tooltip("All possible upgrade ItemData")]
    [SerializeField] private List<ItemData> upgradePool = new();

    // items unlocked at runtime from InventoryRule
    private readonly List<ItemData> runtimePool = new();

    // items blocked at runtime from InventoryRule
    private readonly HashSet<ItemData> blockedFromPool = new();

    [Header("AutoOpen?")]
    [Tooltip("If true, the upgrade panel opens automatically when a level up is available")]
    [SerializeField] private bool autoOpenOnLevelUp = false;

    [Header("Upgrade Options")]
    [Tooltip("Max number of upgrade choices shown each time panel open")]
    [SerializeField] private int maxChoices = 3;

    [Header("Reroll Cost")]
    [Tooltip("Base cost for the first reroll, then rach reroll adds this amount.")]
    [SerializeField] private int baseRerollGoldCost = 5;
    private int _rerollGoldCost;
    public int CurrentRerollGoldCost => _rerollGoldCost;

    private bool levelUpPending;
    private bool panelIsOpen;
    private bool subscribedToXP; // track whether we subscribed to PlayerXP events

    // Input System
    private InputSystem_Actions _inputActions;
    private InputAction _upgradeAction;

    // track which upgrades have already been chosen 
    private readonly HashSet<ItemData> chosenUpgrades = new();

    // the choices offered to the player currently
    private List<ItemData> currentChoices = new();

    // Events
    public event Action<List<ItemData>> UpgradePanelOpened;   // pass the choices to UI
    public event Action<int> UpgradeSelected;
    public event Action UpgradePanelClosed; // call when you wanna clas the panel or sum

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _inputActions = new InputSystem_Actions();
        _upgradeAction = _inputActions.UI.Upgrade;
        _rerollGoldCost = baseRerollGoldCost;
    }

    void OnEnable()
    {
        if (_upgradeAction == null) return;
        _upgradeAction.Enable();
        _upgradeAction.performed += OnUpgradePressed;
    }

    void OnDisable()
    {
        if (_upgradeAction == null) return;
        _upgradeAction.performed -= OnUpgradePressed;
        _upgradeAction.Disable();
    }

    void Start()
    {
        TrySubscribeToXP();
    }

    void OnDestroy()
    {
        // Unsubscribe from any live PlayerXP instance
        if (subscribedToXP && PlayerXP.Instance != null)
        {
            PlayerXP.Instance.LevelUpAvailable -= OnLevelUpAvailable;
            PlayerXP.Instance.LeveledUp -= OnLeveledUp;
        }

        if (Instance == this)
            Instance = null;
    }

    private bool TrySubscribeToXP()
    {
        if (subscribedToXP) return true;

        var xp = PlayerXP.Instance;
        if (xp == null)
        {
            Debug.LogWarning("[UpgradeLevelManager] PlayerXP.Instance not ready yet, retry");
            return false;
        }

        xp.LevelUpAvailable += OnLevelUpAvailable;
        xp.LeveledUp += OnLeveledUp;
        subscribedToXP = true;
        Debug.Log("[UpgradeLevelManager] Subscribed to PlayerXP.LevelUpAvailable.");
        return true;
    }

    private void OnLeveledUp(int newLevel)
    {
    }

    void Update()
    {
        if (!subscribedToXP)
            TrySubscribeToXP();

        // auto-open path (no key press needed)
        if (!panelIsOpen && levelUpPending && !PauseManager.GameIsPaused && autoOpenOnLevelUp)
        {
            OpenUpgradePanel();
        }

        // ensure upgrade action stays enabled while this component is active
        // Other systems disabling shared InputActionAssets (other UI i guess) can disable our action
        if (_upgradeAction != null && !_upgradeAction.enabled)
        {
            Debug.LogWarning("[UpgradeLevelManager] Upgrade action was disabled by sum else, re enabling.");
            _upgradeAction.Enable();
        }
    }

    /// <summary>Called by InputSystem Upgrade action (Q by default).</summary>
    private void OnUpgradePressed(InputAction.CallbackContext ctx)
    {
        // if panel is open, Q closes it
        if (panelIsOpen)
        {
            CloseUpgradePanel();
            return;
        }

        // block when any other panel/state is active like inventory, pause menu, end game
        var ps = PauseManager.CurrentPauseState;
        if (ps != PauseManager.PauseState.None)
        {
            // blocked by pause state
            return;
        }

        if (!levelUpPending)
        {
            var xp = PlayerXP.Instance;
            if (xp != null && xp.IsLevelUpReady)
            {
                levelUpPending = true;
            }
        }

        if (!levelUpPending)
        {
            return;
        }

        OpenUpgradePanel();
    }

    private void OnLevelUpAvailable()
    {
        levelUpPending = true;
        Debug.Log("Level Up available. Press the Upgrade key.");
    }

    private void OpenUpgradePanel()
    {
        if (panelIsOpen) return;

        if (currentChoices == null || currentChoices.Count == 0)
        {
            currentChoices = PickRandomUpgrades();
        }

        // pool is finished, so don't open the Panel
        if (currentChoices == null || currentChoices.Count == 0)
        {
            Debug.Log("[UpgradeLevelManager] Upgrade pool empty, panel not opened.");
            return;
        }

        levelUpPending = false;
        panelIsOpen = true;

        Time.timeScale = 0f;
        PauseManager.GameIsPaused = true;
        PauseManager.CurrentPauseState = PauseManager.PauseState.Upgrade;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        UpgradePanelOpened?.Invoke(currentChoices);
        Debug.Log($"Upgrade panel opened with {currentChoices.Count} choices.");
    }
    // Reroll
    public void RerollChoices()
    {
        if (!panelIsOpen) return;
        if (_rerollGoldCost <= 0)
            _rerollGoldCost = baseRerollGoldCost;

        if (PlayerGold.Instance == null)
        {
            return;
        }

        if (!PlayerGold.Instance.SpendGold(_rerollGoldCost))
        {
            return;
        }

        // Increase cost for the next reroll
        _rerollGoldCost += baseRerollGoldCost;

        currentChoices = PickRandomUpgrades();
        if (currentChoices.Count == 0) return;
        UpgradePanelOpened?.Invoke(currentChoices);
    }
    // close the upgrade panel
    public void CloseUpgradePanel()
    {
        if (!panelIsOpen) return;

        panelIsOpen = false;
        levelUpPending = true; // keep the level up, but no rerun

        UpgradePanelClosed?.Invoke();

        // resume
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        PauseManager.CurrentPauseState = PauseManager.PauseState.None;
        PauseManager.GameIsPaused = false;
        Time.timeScale = 1f;
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

        if (!chosen.canStack)
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

        currentChoices.Clear();
        // Close panel
        panelIsOpen = false;
        UpgradePanelClosed?.Invoke();

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

    private List<ItemData> PickRandomUpgrades()
    {
        var available = new List<ItemData>();
        int totalWeight = 0;

        var combined = new List<ItemData>(upgradePool);
        foreach (var item in runtimePool)
            if (!combined.Contains(item)) combined.Add(item);

        var inventory = PlayerLocator.FindPlayerComponent<PlayerInventory>();

        foreach (var item in combined)
        {
            if (item == null || blockedFromPool.Contains(item)) continue;

            if (item.canStack)
            {
                int current = inventory != null ? inventory.GetItemCount(item) : 0;
                if (current >= item.maxStacks) continue;
            }
            else
            {
                if (chosenUpgrades.Contains(item)) continue;
            }

            available.Add(item);
            totalWeight += RarityWeight(item.rarity);
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
        panelIsOpen = false;
        _rerollGoldCost = baseRerollGoldCost;
    }
}
