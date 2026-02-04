using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class InventoryMenu : MonoBehaviour
{
    private UIDocument document;
    public InputActionAsset InputActions;
    public PauseManager pauseManager;

    private Button backButton;

    void Awake()
    {
        document = GetComponent<UIDocument>();
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        backButton = root.Q<Button>("ButtonBack");
    }

    private void OnEnable()
    {
        document = GetComponent<UIDocument>();
        if (document == null)
        {
            Debug.LogError("PauseMenu: No UIDocument found on this GameObject!");
            return;
        }
        var action = InputActions.FindAction("Pause");
        InputActions.Enable();
        if (backButton != null)
            backButton.RegisterCallback<ClickEvent>(OnBackClick);


    }

    private void OnDisable()
    {
        if (document == null)
            return;

        if (backButton != null)
            backButton.UnregisterCallback<ClickEvent>(OnBackClick);

        InputActions.Disable();

    }

    private void OnBackClick(ClickEvent evt)
    {
        pauseManager.ResumeGame();
    }
}
