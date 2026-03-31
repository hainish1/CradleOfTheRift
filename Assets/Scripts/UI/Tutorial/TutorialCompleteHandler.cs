using UnityEngine;

public class TutorialCompleteHandler : MonoBehaviour
{
    [SerializeField] private TutorialObjectiveUI objectiveUI;
    [SerializeField] private GameObject objectivePanel;

    void Start()
    {
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
        objectivePanel?.SetActive(true);
    }
}