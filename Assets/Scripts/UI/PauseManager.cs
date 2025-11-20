using UnityEngine;

public class PauseManager : MonoBehaviour
{
    private bool isPaused = false;
    PauseAction action;
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private PlayerAimController playerAim;


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

        playerAim.SetLookEnabled(false);
        playerAim.IsPaused = true;

        pauseMenuUI.SetActive(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        playerAim.SetLookEnabled(true);
        playerAim.IsPaused = false;

        pauseMenuUI.SetActive(false);       
    }
}
