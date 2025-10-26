using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.ComponentModel.Design.Serialization;

public class PauseMenu : MonoBehaviour
{
    private UIDocument document;
    private Button startButton;
    private Button continueButton;

    private Button helpButton; // Future setup
    private Button quitButton;

    public InputActionAsset InputActions;

    // private InputAction m_pauseAction;
    public GameObject PauseObject;

    // [SerializeField] private UIDocument settingsMenu; // Future setup

    private void Start()
    {
        gameObject.SetActive(true);
        // Time.timeScale = 0f;
        Debug.Log("Can't Move");
    }
    // private void Awake()
    // {
        // document = GetComponent<UIDocument>();
        // VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        // startButton = root.Q<Button>("ButtonStartGame");
        // continueButton = root.Q<Button>("ButtonContinue");
        // helpButton = root.Q<Button>("ButtonHelp");
        // quitButton = root.Q<Button>("ButtonQuitGame");

        // m_pauseAction = InputActions.FindAction("Pause");

    // }

    // void Update()
    // {
    //     if (m_pauseAction.WasPressedThisFrame())
    //         TogglePause();
    // }

    private void OnEnable()
    {
        startButton = document.rootVisualElement.Q("ButtonStartGame") as Button;
        continueButton = document.rootVisualElement.Q("ButtonContinue") as Button;
        helpButton = document.rootVisualElement.Q("ButtonHelp") as Button;
        quitButton = document.rootVisualElement.Q("ButtonQuitGame") as Button;

        var action = InputActions.FindAction("Pause");
        // if (action != null)
        //     action.performed += OnPausePressed;

        InputActions.Enable();

        if (startButton != null)
            startButton.RegisterCallback<ClickEvent>(OnStartGameClick);

        if (continueButton != null)
            continueButton.RegisterCallback<ClickEvent>(OnContinueClick);

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
            continueButton.UnregisterCallback<ClickEvent>(OnContinueClick);

        if (helpButton != null)
            helpButton.UnregisterCallback<ClickEvent>(OnHelpClick);

        if (quitButton != null)
            quitButton.UnregisterCallback<ClickEvent>(OnQuitGameClick);

        // var action = InputActions.FindAction("Pause");
        // if (action != null)
        //     action.performed -= OnPausePressed;

        InputActions.Disable();
    }
    // private void OnPausePressed(InputAction.CallbackContext context)
    // {
    //     TogglePause();
    // }

    //     private void TogglePause()
    // {
    //     bool isActive = PauseObject.activeSelf;
    //     PauseObject.SetActive(!isActive);
    //     Time.timeScale = !isActive ? 0f : 1f;
    //     Debug.Log(!isActive ? "Game Paused" : "Game Resumed");
    // }


    private void OnStartGameClick(ClickEvent evt)
    {
        Debug.Log("You Pressed the Start Button");
        SceneManager.LoadScene("Design");// Change name to game
        // gameObject.SetActive(false);
    }

    private void OnContinueClick(ClickEvent evt)
    {
        PauseObject.SetActive(!PauseObject.activeSelf);
        Time.timeScale = PauseObject.activeSelf ? 0f : 1f;

    }

    private void OnQuitGameClick(ClickEvent evt)
    {
        Debug.Log("Quitting game...");
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
