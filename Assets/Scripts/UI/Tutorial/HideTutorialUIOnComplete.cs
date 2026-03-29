using UnityEngine;
using UnityEngine.UIElements;

public class HideTutorialUIOnComplete : MonoBehaviour
{
    [SerializeField] private UIDocument tutorialDocument;

    void OnEnable()
    {
        TutorialSceneManager.Instance.OnTutorialComplete += HandleTutorialComplete;
    }

    void OnDisable()
    {
        if (TutorialSceneManager.Instance != null)
            TutorialSceneManager.Instance.OnTutorialComplete -= HandleTutorialComplete;
    }

    private void HandleTutorialComplete()
    {
        Debug.Log("TUTORIAL SHOULD BE GONE");
        if (tutorialDocument != null)
        {
            // This hides the UI but keeps the script running
            tutorialDocument.rootVisualElement.style.display = DisplayStyle.None;
            Debug.Log("TUTORIAL SHOULD BE GONE");
            
            // OR: This completely disables the component
            // tutorialDocument.enabled = false;
        }
    }
}