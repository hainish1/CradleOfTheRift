using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// This class checks if Level up is available, and shows an upgrade selection panel when the player presses 
/// the upgrade keybind. Pause the game, and resumes after somethign is picked.
/// </summary>
public class UpgradeLevelManager : MonoBehaviour
{

    public static UpgradeLevelManager Instance { get; private set; }

    // [Header("UI")]
    // [Tooltip("upgrade selection panel placeholder")]
    // [SerializeField] private GameObject upgradePanelUI;

    [Header("Input")]
    [SerializeField] private Key activateKey = Key.U;

    [Header("Upgrade options")]
    [Tooltip("Number of upgrade choices shown to the player")]
    [SerializeField] private int choiceCount = 3;

    private bool levelUpPending;
    private PlayerXP playerXP;

    // UI events

    // fires when upgrade panel is opened
    public event Action UpgradePanelOpened;

    // fires when player selects an upgrade, 0 - 1st choice, 1 - 2nd choice, 2 - 3rd choice
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
        // if (upgradePanelUI != null)
        // {
        //     upgradePanelUI.SetActive(false);
        // }
        playerXP = PlayerXP.Instance;

        if (playerXP != null)
        {
            playerXP.LevelUpAvailable += OnLevelUpAvailable;
        }
        else
        {
            Debug.Log("Level up thing not founds");
        }
    }

    void OnDestroy()
    {
        if (playerXP != null)
        {
            playerXP.LevelUpAvailable -= OnLevelUpAvailable;
        }
    }

    void Update()
    {
        if (!levelUpPending) return;

        // Wait for the player to press the keybind
        if (Keyboard.current != null && Keyboard.current[activateKey].wasPressedThisFrame)
        {
            OpenUpgradePanel();
        }
    }

    // when threshold is reached
    private void OnLevelUpAvailable()
    {
        levelUpPending = true;
        Debug.Log($"Level Up available! Press '{activateKey}' to choose an upgrade");
    }

    // opens the panel and pause 
    private void OpenUpgradePanel()
    {
        levelUpPending = false;

        // Pause logic, ehhh Simple 
        Time.timeScale = 0f;
        PauseManager.GameIsPaused = true;
        PauseManager.CurrentPauseState = PauseManager.PauseState.Inventory; // im gonna reuse this for a bit

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // if(upgradePanelUI != null)
        // {
        //     upgradePanelUI.SetActive(true);
        // }

        UpgradePanelOpened?.Invoke();
        Debug.Log("Upgrade Panel was opened");
    }

    /// <summary>
    /// Called when player presses a UI button to select an upgrade, 0,1,2,3....
    /// </summary>
    public void SelectUpgrade(int indexChoice)
    {
        Debug.Log($"Player selected upgrade #{indexChoice}.");

        // TOOD: Apply the actual upgrade effect here, prolly gonna do this later
        UpgradeSelected?.Invoke(indexChoice);

        // consume the level up
        if(PlayerXP.Instance != null)
        {
            PlayerXP.Instance.ConsumeLevelUp();
        }

        // resume
        // if(upgradePanelUI != null)
        // {
        //     upgradePanelUI.SetActive(false);
        // }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        PauseManager.CurrentPauseState = PauseManager.PauseState.None;
        PauseManager.GameIsPaused = false;
        Time.timeScale = 1f;

        Debug.Log(" Upgrade selected, game resumed");
    }
}
