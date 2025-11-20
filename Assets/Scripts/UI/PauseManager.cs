using UnityEngine;

public class PauseManager : MonoBehaviour
{
    private bool isPaused = false;
    PauseAction action;
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private PlayerAimController playerAim;
    [SerializeField] private GameObject inventoryUI;


    void Awake()
    {
        action = new PauseAction();
    }

    void Start()
    {
        action.Pause.PauseGame.performed += _ => DeterminedPause();
    }


    private void DeterminedPause()
    {
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
        Time.timeScale = 0f;

        playerAim.IsPaused = true;
        playerAim.SetLookEnabled(false);

        pauseMenuUI.SetActive(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        playerAim.IsPaused = false;
        playerAim.SetLookEnabled(true);

        pauseMenuUI.SetActive(false);
        //inventoryUI?.SetActive(false);
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
