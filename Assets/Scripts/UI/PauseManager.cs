using UnityEngine;

public class PauseManager : MonoBehaviour
{
    private bool isPaused = false;
    PauseAction action;
    [SerializeField] 
    private GameObject pauseMenuUI;


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
        // Unlock and show cursor for menu interaction
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        Debug.Log("Game Paused: Cursor unlocked and visible");
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1;
        isPaused = false;
        pauseMenuUI.SetActive(true);
        // Lock and hide cursor for gameplay
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        Debug.Log("Game Resumed: Cursor locked and hidden");
       
    }
}
