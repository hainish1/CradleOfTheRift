using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    private UIDocument document;
    private Button startButton;
    private Button continueButton;
    private Button mainMenuButton;
    private Button creditsButton;
    private Button backButton;
    private Button quitButton;
    public InputActionAsset InputActions;
    public PauseManager pauseManager;
    [SerializeField]private GameObject creditsUI;
    [SerializeField]private GameObject mainMenuUI;

    private void Awake()
    {
        document = GetComponent<UIDocument>();
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        startButton = root.Q<Button>("ButtonStartGame");
        continueButton = root.Q<Button>("ButtonContinue");
        mainMenuButton = root.Q<Button>("ButtonMainMenu");
        creditsButton = root.Q<Button>("ButtonCredits");
        backButton = root.Q<Button>("ButtonBack");
        quitButton = root.Q<Button>("ButtonQuitGame");
    }

    private void OnEnable()
    {
        document = GetComponent<UIDocument>();
        if (document == null)
        {
            Debug.LogError("PauseMenu: No UIDocument found on this GameObject!");
            return;
        }


        startButton = document.rootVisualElement.Q("ButtonStartGame") as Button;
        continueButton = document.rootVisualElement.Q("ButtonContinue") as Button;
        mainMenuButton = document.rootVisualElement.Q("ButtonMainMenu") as Button;
        creditsButton = document.rootVisualElement.Q("ButtonCredits") as Button;
        backButton = document.rootVisualElement.Q("ButtonBack") as Button;
        quitButton = document.rootVisualElement.Q("ButtonQuitGame") as Button;

        var action = InputActions.FindAction("Pause");

        InputActions.Enable();

        if (startButton != null)
            startButton.RegisterCallback<ClickEvent>(OnStartGameClick);

        if (continueButton != null)
            continueButton.RegisterCallback<ClickEvent>(OnContinueClick);

        if (mainMenuButton != null)
            mainMenuButton.RegisterCallback<ClickEvent>(OnMainMenuClick);

        if (creditsButton != null)
            creditsButton.RegisterCallback<ClickEvent>(OnCreditsClick);

        if (backButton != null)
            backButton.RegisterCallback<ClickEvent>(OnBackClick);

        if (quitButton != null)
            quitButton.RegisterCallback<ClickEvent>(OnQuitGameClick);
    }

    private void OnDisable()
    {
        if (document == null)
            return;

        if (startButton != null)
            startButton.UnregisterCallback<ClickEvent>(OnStartGameClick);

        if (continueButton != null)
            continueButton.UnregisterCallback<ClickEvent>(OnContinueClick);

        if (mainMenuButton != null)
            mainMenuButton.UnregisterCallback<ClickEvent>(OnMainMenuClick);

        if (creditsButton != null)
            creditsButton.UnregisterCallback<ClickEvent>(OnCreditsClick);

        if (backButton != null)
            backButton.UnregisterCallback<ClickEvent>(OnBackClick);

        if (quitButton != null)
            quitButton.UnregisterCallback<ClickEvent>(OnQuitGameClick);

        InputActions.Disable();
    }

    private void OnMainMenuClick(ClickEvent evt)
    {
        SceneManager.LoadScene("MainMenu");
        Time.timeScale = 1f;
        PauseManager.GameIsPaused = false;
        PauseManager.CurrentPauseState = PauseManager.PauseState.None;
        PlayerHealth.GameIsOver = false;
    }

    private void OnStartGameClick(ClickEvent evt)
    {
        // Debug.Log("You Pressed the Start Button");
        PauseManager.GameIsPaused = false;
        PlayerHealth.GameIsOver = false;
        SceneManager.LoadScene("Design 1");// Change name to game
    }

    private void OnContinueClick(ClickEvent evt)
    {
        if (PlayerHealth.GameIsOver) return;

        pauseManager.ResumeGame();
        // Debug.Log("Continue Button Clicked, should continue.");
    }

    private void OnCreditsClick(ClickEvent evt)
    {
        creditsUI.SetActive(true);
        mainMenuUI.SetActive(false);

    }

    private void OnBackClick(ClickEvent evt)
    {
        creditsUI.SetActive(false);
        mainMenuUI.SetActive(true);
    }

    private void OnQuitGameClick(ClickEvent evt)
    {
        pauseManager.QuitGame();
    }

}
