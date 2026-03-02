using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;


public class EndScreenUI : MonoBehaviour
{
    [SerializeField]
    private PlayerHealth playerHealth;
    [SerializeField]
    private GameObject winScreen;
    [SerializeField]
    private GameObject loseScreen;
    private GameObject activeScreen;
    [SerializeField]
    private PauseManager ManagePause;
    private bool IsScreenShowing = false;

    void OnEnable()
    {
        if(playerHealth == null)
        {
            playerHealth = PlayerLocator.FindPlayerComponent<PlayerHealth>();
        }

        if (ExtractionManager.Instance != null)
            ExtractionManager.Instance.OnGameWon += OnWinScreen;

        if(playerHealth == null)
        {
            Debug.Log("Player health missing on player, or player missing");
            return;
        }
        
        if (this.playerHealth != null)
            this.playerHealth.LoseScreen += OnLoseScreen;
    }

    void OnDisable()
    {
        if (ExtractionManager.Instance != null)
            ExtractionManager.Instance.OnGameWon -= OnWinScreen;

        if (this.playerHealth != null)
            this.playerHealth.LoseScreen -= OnLoseScreen;
    }

    private void OnWinScreen()
    {
        if (IsScreenShowing) return; // prevent multiple screens if somehow triggered multiple times
        IsScreenShowing = true;

        this.activeScreen = Instantiate(winScreen);
        
        FinalizeEndGame();

        // go back to Start scene
        // StartCoroutine(LoadSceneAfterDelay("Jared", 5f)); // 5 second delay
    }

    private void OnLoseScreen()
    {
        if (IsScreenShowing) return; // prevent multiple screens if somehow triggered multiple times
        IsScreenShowing = true;

        this.activeScreen = Instantiate(loseScreen);

        FinalizeEndGame();

        // StartCoroutine(LoadSceneAfterDelay("Jared", 5f)); // 5 second delay
    }

    private void FinalizeEndGame()
    {
        HookEndScreenButtons(activeScreen);

        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        PlayerHealth.GameIsOver = true;
        ManagePause.PauseForEndGame();
    }

    private void HookEndScreenButtons(GameObject screen)
    {
        // get UI Document on the end screen object
        var document = screen.GetComponent<UIDocument>();
        if (document == null)
        {
            Debug.LogWarning("No UIDocument found on end screen prefab!");
            return;
        }

        var root = document.rootVisualElement;
        var playAgainButton = root.Q<Button>("playAgainButton");
        var quitButton = root.Q<Button>("quitButton");
        var mainMenuButton = root.Q<Button>("mainMenuButton");

        // Play Again → restart current level
        if (playAgainButton != null)
        {
            playAgainButton.RegisterCallback<ClickEvent>(evt =>
            {
                Debug.Log("Play Again clicked!");
                Time.timeScale = 1f; // unpause
                PauseManager.GameIsPaused = false;
                PauseManager.CurrentPauseState = PauseManager.PauseState.None;
                PlayerHealth.GameIsOver = false;

                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;

                SceneManager.LoadScene("Design 1"); // or your current game scene name
            });
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.RegisterCallback<ClickEvent>(evt =>
            {
            Time.timeScale = 1f;
            PauseManager.GameIsPaused = false;
            PauseManager.CurrentPauseState = PauseManager.PauseState.None;
            PlayerHealth.GameIsOver = false;
            SceneManager.LoadScene("MainMenu");
            });
        }

        // Quit → exit to desktop
        if (quitButton != null)
        {
            quitButton.RegisterCallback<ClickEvent>(evt =>
            {
                Debug.Log("Quit clicked!");
                Application.Quit();

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            });
        }
    }



    private IEnumerator LoadSceneAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }
}