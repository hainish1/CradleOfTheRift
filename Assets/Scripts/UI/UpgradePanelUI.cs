using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Manages the UI for upgrade selection panel
/// </summary>
public class UpgradePanelUI : MonoBehaviour
{
    private UIDocument document;
    private VisualElement overlay;
    private Button[] choiceButtons;

    void Awake()
    {
        document = GetComponent<UIDocument>();
        var root = document.rootVisualElement;

        overlay = root.Q<VisualElement>("UpgradeOverlay");
        
        choiceButtons = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            int index = i; // capture for closure
            choiceButtons[i] = root.Q<Button>($"Choice{i}");
            choiceButtons[i].RegisterCallback<ClickEvent>(evt => OnChoiceClicked(index));
        }

        // start hidden
        Hide();
    }

    void OnEnable()
    {
        if (UpgradeLevelManager.Instance != null)
        {
            UpgradeLevelManager.Instance.UpgradePanelOpened += Show;
        }
    }

    void OnDisable()
    {
        if (UpgradeLevelManager.Instance != null)
        {
            UpgradeLevelManager.Instance.UpgradePanelOpened -= Show;
        }
    }

    public void Show()
    {
        overlay.style.display = DisplayStyle.Flex;

        // TODO: show actual upgrade names here

    }

    public void Hide()
    {
        overlay.style.display = DisplayStyle.None;
    }

    private void OnChoiceClicked(int index)
    {
        Debug.Log($"Player clicked choice {index}");
        Hide();

        if (UpgradeLevelManager.Instance != null)
        {
            UpgradeLevelManager.Instance.SelectUpgrade(index);
        }
    }
}