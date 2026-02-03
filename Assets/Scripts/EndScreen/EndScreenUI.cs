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

    void OnEnable()
    {
        if (ExtractionManager.Instance != null)
            ExtractionManager.Instance.OnGameWon += OnWinScreen;

        if (this.playerHealth != null)
            this.playerHealth.LoseScreen += OnLoseScreen;
    }

    void Start()
    {
        if (ExtractionManager.Instance != null)
            ExtractionManager.Instance.OnGameWon += OnWinScreen;

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
        this.activeScreen = Instantiate(winScreen);
        HookEndScreenButtons(activeScreen);

        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        PlayerHealth.GameIsOver = true;
        ManagePause.PauseForEndGame();


        // go back to Start scene
        // StartCoroutine(LoadSceneAfterDelay("Jared", 5f)); // 5 second delay
    }

    private void OnLoseScreen()
    {
        this.activeScreen = Instantiate(loseScreen);
        HookEndScreenButtons(activeScreen);

        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        PlayerHealth.GameIsOver = true;
        ManagePause.PauseForEndGame();
        // StartCoroutine(LoadSceneAfterDelay("Jared", 5f)); // 5 second delay
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

                SceneManager.LoadScene("Design"); // or your current game scene name
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