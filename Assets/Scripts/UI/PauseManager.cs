using UnityEngine;

public class PauseManager : MonoBehaviour
{
    // [SerializeField] private GameObject pauseMenu;
    private bool isPaused = false;
    PauseAction action;

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
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
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
        Time.timeScale = 0;
        isPaused = true;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1;
        isPaused = false;
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        // pauseMenu.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
    }
}
