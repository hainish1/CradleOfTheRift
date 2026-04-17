using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public enum PauseState
    {
        None,
        PauseMenu,
        Inventory,
        EndGame,
        Upgrade,
        Settings
    }
    public static bool GameIsPaused; 
    public static PauseState CurrentPauseState = PauseState.None;
    [SerializeField] private bool isMainMenu = false;

    // Reset statics when entering play mode 
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        GameIsPaused = false;
        CurrentPauseState = PauseState.None;
    }

    private bool isPaused = false;
    private PauseAction action;
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private GameObject inventoryMenuUI;
    [SerializeField] private PlayerAimController playerAim;

    void Awake()
    {
        action = new PauseAction();
        ResumeInternal();
    }

    void Start()
    {
        pauseMenuUI.SetActive(false);
        inventoryMenuUI.SetActive(false);

        action.Pause.PauseGame.performed += _ => TogglePauseMenu();
        action.Inventory.Inventory.performed += _ => ToggleInventory();
    }
    void OnEnable() { action.Enable(); }

    void OnDisable() { action.Disable(); }

    public void PauseGame()
    {
        isPaused = true;
        GameIsPaused = true;
        Time.timeScale = 0f;

        if (playerAim != null)
        {
            playerAim.SetLookEnabled(false);
            playerAim.IsPaused = true;
        }

        if (PlayerHealth.GameIsOver) return;

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(true);
    }

    public void ResumeGame()
    {
        ResumeInternal();
    }

    private void ResumeInternal()
    {
        GameIsPaused = false;
        isPaused = false;

        CurrentPauseState = PauseState.None;
        Time.timeScale = 1f;

        if (playerAim != null)
        {
            playerAim.SetLookEnabled(true);
            playerAim.IsPaused = false;
        }

        pauseMenuUI.SetActive(false);
        inventoryMenuUI.SetActive(false);
    }
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void ApplyPause()
    {
        GameIsPaused = true;
        Time.timeScale = 0f;

        if (playerAim != null)
        {
            playerAim.SetLookEnabled(false);
            playerAim.IsPaused = true;
        }
    }
    private void TogglePauseMenu()
    {
        if (this.isMainMenu)
        {
            return;
        }

        if (PlayerHealth.GameIsOver) return;
        if (CurrentPauseState == PauseState.EndGame) return;
        if (CurrentPauseState == PauseState.Upgrade) return; // don't interfere with upgrade panel
        if (CurrentPauseState == PauseState.Settings) return;


        if (CurrentPauseState == PauseState.PauseMenu)
        {
            ResumeInternal();
            return;
        }

        if (CurrentPauseState != PauseState.None)
            return; 

        CurrentPauseState = PauseState.PauseMenu;
        ApplyPause();
        pauseMenuUI.SetActive(true);
    }
    private void ToggleInventory()
    {
        if (PlayerHealth.GameIsOver) return;
        if (CurrentPauseState == PauseState.EndGame) return;
        if (CurrentPauseState == PauseState.Upgrade) return; // don't interfere with upgrade panel

        if (CurrentPauseState == PauseState.Inventory)
        {
            ResumeInternal();
            return;
        }

        if (CurrentPauseState != PauseState.None)
            return; 

        CurrentPauseState = PauseState.Inventory;
        ApplyPause();
        inventoryMenuUI.SetActive(true);
    }
    public void PauseForEndGame()
    {
        CurrentPauseState = PauseState.EndGame;
        GameIsPaused = true;
        Time.timeScale = 0f;

        if (playerAim != null)
        {
            playerAim.SetLookEnabled(false);
            playerAim.IsPaused = true;
        }

        
        pauseMenuUI.SetActive(false);
        inventoryMenuUI.SetActive(false);
    }

}
