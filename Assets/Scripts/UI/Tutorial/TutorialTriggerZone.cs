using UnityEngine;

/// <summary>
/// Place this on a GameObject with a Trigger Collider in the tutorial scene.
/// When the player enters the trigger, it completes the current tutorial step.
///
/// Use this for steps like "fly to the marked area" or "walk to this location"
/// where completion is based on the player reaching a physical place in the world.
///
/// Setup:
///   1. Create a GameObject, add a Collider, tick "Is Trigger"
///   2. Add this script
///   3. Set expectedStepIndex to match the step's position in TutorialSceneManager.steps[]
///      (0 = first step, 1 = second step, etc.)
///   4. Make sure your Player GameObject has the tag "Player"
/// </summary>
public class TutorialTriggerZone : MonoBehaviour
{
    [Tooltip("The index of the step this zone completes (0-based). " +
             "Must match the step's position in TutorialSceneManager.steps[]. " +
             "Set to -1 to complete whatever step is currently active.")]
    [SerializeField] private int expectedStepIndex = -1;

    [Tooltip("The tag used to identify the player. Must match the Player GameObject's tag.")]
    [SerializeField] private string playerTag = "Player";

    private bool _fired = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_fired) return;
        if (!other.CompareTag(playerTag)) return;

        // If an expected step is set, only fire during that specific step.
        // This prevents the zone from triggering early if the player wanders in
        // before reaching this point in the tutorial.
        if (expectedStepIndex >= 0 &&
            TutorialSceneManager.Instance?.CurrentStepIndex != expectedStepIndex)
            return;

        _fired = true;
        TutorialSceneManager.Instance?.CompleteCurrentStep();
    }

    // Reset the fired flag if the tutorial restarts (e.g. during development testing)
    public void Reset()
    {
        _fired = false;
    }

    // Draw a visible gizmo in the editor so you can see where the zone is
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.25f);
        var col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.8f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(transform.position, sphere.radius * transform.lossyScale.x);
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, sphere.radius * transform.lossyScale.x);
        }
    }
}
