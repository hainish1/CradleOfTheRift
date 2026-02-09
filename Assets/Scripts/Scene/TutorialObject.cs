using UnityEngine;

public class TutorialObject : MonoBehaviour
{
    public string EventID;
    public bool isKillable;
    [HideInInspector] public bool wasTriggered = false;

    // Collision handling is located inside the PlayerMovement script.

    private void OnTriggerEnter(Collider other)
    {
        if (!isKillable)
        {
            wasTriggered = true;
            TutorialManager.TriggerTutorialEvent(EventID);
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (!wasTriggered) TutorialManager.TriggerTutorialEvent(EventID);
    }
}
