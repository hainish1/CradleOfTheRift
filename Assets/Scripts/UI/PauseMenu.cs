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

    private Button inventoryButton; // Future setup
    private Button quitButton;

    public GameObject inventoryObject;

    public InputActionAsset InputActions;
    public PauseManager pauseManager;
    // private InputAction m_pauseAction;
    //public GameObject PauseObject;

    // [SerializeField] private UIDocument settingsMenu; // Future setup

    private void Awake()
    {
        document = GetComponent<UIDocument>();
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        startButton = root.Q<Button>("ButtonStartGame");
        continueButton = root.Q<Button>("ButtonContinue");
        inventoryButton = root.Q<Button>("ButtonInventory");
        quitButton = root.Q<Button>("ButtonQuitGame");

        // m_pauseAction = InputActions.FindAction("Pause");

    }

    // void Update()
    // {
    //     if (m_pauseAction.WasPressedThisFrame())
    //         TogglePause();
    // }

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
        inventoryButton = document.rootVisualElement.Q("ButtonInventory") as Button;
        quitButton = document.rootVisualElement.Q("ButtonQuitGame") as Button;

        var action = InputActions.FindAction("Pause");
        // if (action != null)
        //     action.performed += OnPausePressed;

        InputActions.Enable();

        if (startButton != null)
            startButton.RegisterCallback<ClickEvent>(OnStartGameClick);

        if (continueButton != null)
            continueButton.RegisterCallback<ClickEvent>(OnContinueClick);

        if (inventoryButton != null)
            inventoryButton.RegisterCallback<ClickEvent>(OnInventoryClick);

        if (quitButton != null)
            quitButton.RegisterCallback<ClickEvent>(OnQuitGameClick);
    }

    private void OnDisable()
    {
        if (document == null)
            return;

        if (startButton != null)
            startButton.UnregisterCallback<ClickEvent>(OnStartGameClick);

        if (inventoryButton != null)
            continueButton.UnregisterCallback<ClickEvent>(OnContinueClick);

        if (inventoryButton != null)
            inventoryButton.UnregisterCallback<ClickEvent>(OnInventoryClick);

        if (quitButton != null)
            quitButton.UnregisterCallback<ClickEvent>(OnQuitGameClick);

        // var action = InputActions.FindAction("Pause");
        // if (action != null)
        //     action.performed -= OnPausePressed;

        InputActions.Disable();
    }

    private void OnStartGameClick(ClickEvent evt)
    {
        Debug.Log("You Pressed the Start Button");
        SceneManager.LoadScene("Design");// Change name to game
        // gameObject.SetActive(false);
    }

    private void OnContinueClick(ClickEvent evt)
    {
        //PauseObject.SetActive(!PauseObject.activeSelf);
        //Time.timeScale = PauseObject.activeSelf ? 0f : 1f;
        pauseManager.ResumeGame();
        Debug.Log("Continue Button Clicked, should continue.");

    }

    private void OnInventoryClick(ClickEvent evt)
    {
        Debug.Log("Opening inventory...");
        pauseManager.OpenInventory();
    }

    private void OnQuitGameClick(ClickEvent evt)
    {
        pauseManager.QuitGame();
    }

}
