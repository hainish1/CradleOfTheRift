using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    private UIDocument document;
    private Button startButton;
    private Button settingsButton; // Future setup
    private Button quitButton;

    [SerializeField] private UIDocument settingsMenu; // Future setup


    private void Awake()
    {
        document = GetComponent<UIDocument>();

        startButton = document.rootVisualElement.Q("ButtonStartGame") as Button;
        settingsButton = document.rootVisualElement.Q("ButtonSettings") as Button;
        quitButton = document.rootVisualElement.Q("ButtonQuitGame") as Button;



        if (startButton != null)
            startButton.RegisterCallback<ClickEvent>(OnStartGameClick);

        if (settingsButton != null)
            settingsButton.RegisterCallback<ClickEvent>(OnSettingsClick);

        if (quitButton != null)
            quitButton.RegisterCallback<ClickEvent>(OnQuitGameClick);
    }

    private void OnDisable()
    {
        if (startButton != null)
            startButton.UnregisterCallback<ClickEvent>(OnStartGameClick);

        if (settingsButton != null)
            settingsButton.UnregisterCallback<ClickEvent>(OnSettingsClick);

        if (quitButton != null)
            quitButton.UnregisterCallback<ClickEvent>(OnQuitGameClick);

    }

    private void OnStartGameClick(ClickEvent evt)
    {
        // Debug.Log("You Pressed the Start Button");
        SceneManager.LoadScene("Design");// Change name to game

    }

    private void OnQuitGameClick(ClickEvent evt)
    {
        Debug.Log("Quitting game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    
    private void OnSettingsClick(ClickEvent evt)
    {
        // Debug.Log("Opening settings menu...");
        // if (settingsMenu != null)
        //     settingsMenu.gameObject.SetActive(true);
    }

}
