using UnityEngine;
using UnityEngine.UIElements;

public class TutorialCompleteHandler : MonoBehaviour
{
    [SerializeField] private TutorialObjectiveUI objectiveUI;
    [SerializeField] private UIDocument playerUIDocument;
    private VisualElement objectivePanel;

    void Start()
    {
        objectivePanel = playerUIDocument.rootVisualElement.Q("ObjectivesPanelRoot");
        if (objectivePanel != null)
            objectivePanel.style.display = DisplayStyle.None;

        TutorialSceneManager.Instance.OnTutorialComplete += OnTutorialComplete;
    }

    void OnDestroy()
    {
        if (TutorialSceneManager.Instance != null)
            TutorialSceneManager.Instance.OnTutorialComplete -= OnTutorialComplete;
    }

    private void OnTutorialComplete()
    {
        objectiveUI.Hide();

        if (objectivePanel != null)
            objectivePanel.style.display = DisplayStyle.Flex;
    }
}