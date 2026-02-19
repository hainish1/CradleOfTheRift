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

    // picks random choices from pool that have not been chosen yet
    private List<ItemData> PickRandomUpgrades()
    {
        List<ItemData> available = new();
        foreach (var item in upgradePool)
        {
            if (item != null && !chosenUpgrades.Contains(item))
                available.Add(item);
        }

        // Shuffle
        for (int i = available.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (available[i], available[j]) = (available[j], available[i]);
        }

        int count = Mathf.Min(maxChoices, available.Count);
        return available.GetRange(0, count);
    }



    public void ResetForNewRun()
    {
        chosenUpgrades.Clear();
        currentChoices.Clear();
        levelUpPending = false;
    }
}
