using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static bool GameIsPaused; // Global Pause Var

    private bool isPaused = false;
    PauseAction action;
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private PlayerAimController playerAim;
    [SerializeField] private GameObject inventoryUI;


    void Awake()
    {
        action = new PauseAction();
        GameIsPaused = false; // Reset static flag on scene start
        isPaused = false;     // Reset instance flag

    }

    void Start()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;

        action.Pause.PauseGame.performed += _ => DeterminedPause();
    }


    private void DeterminedPause()
    {
        if (PlayerHealth.GameIsOver) return; // cannot pause if game ended

        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }
    void OnEnable()
    {
        action.Enable();
    }

    void OnDisable()
    {
        action.Disable();
    }

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

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        GameIsPaused = false;

        Time.timeScale = 1f;

        if (playerAim != null)
        {
            playerAim.SetLookEnabled(true);
            playerAim.IsPaused = false;
        }

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);
    }
    public void OpenInventory()
    {
        // pauseMenuUI.SetActive(false);
        // inventoryUI.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

}
