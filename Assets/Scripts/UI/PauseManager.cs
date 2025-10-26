using UnityEngine;

public class PauseManager : MonoBehaviour
{
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
}
