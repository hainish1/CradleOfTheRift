using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;


public class EndScreenUI : MonoBehaviour
{
    [SerializeField]
    private ExtractionZone extractionZone;
    [SerializeField]
    private PlayerHealth playerHealth;
    [SerializeField]
    private GameObject winScreen;
    [SerializeField]
    private GameObject loseScreen;
    private GameObject activeScreen;
    private TimerUI timerUI;


    void OnEnable()
    {
        if (this.timerUI == null)
            this.timerUI = GetComponent<TimerUI>();

        if (this.extractionZone != null)
            this.extractionZone.WinScreen += OnWinScreen;

        if (this.playerHealth != null)
            this.playerHealth.LoseScreen += OnLoseScreen;

        if (this.timerUI != null)
            this.timerUI.DisplayEndGame += OnLoseScreen;
    }

    void OnDisable()
    {
        if (this.extractionZone != null)
            this.extractionZone.WinScreen -= OnWinScreen;

        if (this.playerHealth != null)
            this.playerHealth.LoseScreen -= OnLoseScreen;

        if (this.timerUI != null)
            this.timerUI.DisplayEndGame -= OnLoseScreen;
    }

    private void OnWinScreen()
    {
        this.activeScreen = Instantiate(winScreen);
        HookEndScreenButtons(activeScreen);
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        Time.timeScale = 0f; // pause gameplay


        // go back to Start scene
        // StartCoroutine(LoadSceneAfterDelay("Jared", 5f)); // 5 second delay
    }

    private void OnLoseScreen()
    {
        this.activeScreen = Instantiate(loseScreen);
        HookEndScreenButtons(activeScreen);
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        Time.timeScale = 0f; // pause gameplay

        // go back to Start scene
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