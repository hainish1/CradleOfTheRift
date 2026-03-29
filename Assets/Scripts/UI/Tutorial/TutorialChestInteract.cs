using UnityEngine;

public class TutorialChestInteract : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int expectedStepIndex = -1;

    private bool _fired = false;

    private void OnDestroy()
    {
        TryCompleteStep();
    }

    private void TryCompleteStep()
    {
        if (_fired) return;
        if (expectedStepIndex >= 0 &&
            TutorialSceneManager.Instance?.CurrentStepIndex != expectedStepIndex)
            return;

        _fired = true;
        Debug.Log("[Tutorial] Chest destroyed — completing step.");
        TutorialSceneManager.Instance?.CompleteCurrentStep();
    }

    public void ResetInteract() => _fired = false;
}