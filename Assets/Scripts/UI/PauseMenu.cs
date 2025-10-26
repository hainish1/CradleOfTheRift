using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    private UIDocument document;
    private Button startButton;
    private Button continueButton;

    private Button helpButton; // Future setup
    private Button quitButton;

    [SerializeField] private UIDocument settingsMenu; // Future setup


    private void Awake()
    {
        document = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        startButton = document.rootVisualElement.Q("ButtonStartGame") as Button;
        continueButton = document.rootVisualElement.Q("Buttoncontinue") as Button;
        helpButton = document.rootVisualElement.Q("ButtonHelp") as Button;
        quitButton = document.rootVisualElement.Q("ButtonQuitGame") as Button;



        if (startButton != null)
            startButton.RegisterCallback<ClickEvent>(OnStartGameClick);

        if (continueButton != null)
            quitButton.RegisterCallback<ClickEvent>(OnContinueClick);

        if (helpButton != null)
            helpButton.RegisterCallback<ClickEvent>(OnHelpClick);

        if (quitButton != null)
            quitButton.RegisterCallback<ClickEvent>(OnQuitGameClick);
    }

    private void OnDisable()
    {
        if (startButton != null)
            startButton.UnregisterCallback<ClickEvent>(OnStartGameClick);

        if (helpButton != null)
            helpButton.UnregisterCallback<ClickEvent>(OnHelpClick);

        if (quitButton != null)
            quitButton.UnregisterCallback<ClickEvent>(OnQuitGameClick);

    }

    private void OnStartGameClick(ClickEvent evt)
    {
        // Debug.Log("You Pressed the Start Button");
        // SceneManager.LoadScene("Design");// Change name to game
        gameObject.SetActive(false);
    }

    private void OnContinueClick(ClickEvent evt)
    {
        
    }

    private void OnQuitGameClick(ClickEvent evt)
    {
        // Debug.Log("Quitting game...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    
    private void OnHelpClick(ClickEvent evt)
    {
        Debug.Log("Opening Help menu...");
        // if (helpMenu != null)
        //     helpMenu.gameObject.SetActive(true);
    }

}
