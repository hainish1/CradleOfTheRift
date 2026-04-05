using UnityEngine;

/// <summary>
/// Place this on a GameObject with a Trigger Collider (the "Target").
/// When a projectile (based on Layer) enters the trigger, it completes the current tutorial step.
/// 
/// Setup:
///   1. Ensure your Projectile Prefab is on a specific Layer (e.g., "Projectiles").
///   2. Create a Target GameObject, add a Collider, tick "Is Trigger".
///   3. Add this script and select the Projectile Layer in the dropdown.
/// </summary>
public class TutorialProjectileTarget : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The Layer(s) that count as a valid projectile.")]
    [SerializeField] private LayerMask projectileLayer;

    [Tooltip("The index of the step this target completes (0-based).")]
    [SerializeField] private int expectedStepIndex = -1;
    private bool _fired = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_fired) return;

        // Check if the object entering the trigger is on the projectile layer
        // (1 << other.gameObject.layer) creates a bitmask for the incoming object
        if ((projectileLayer.value & (1 << other.gameObject.layer)) == 0) return;

        // Ensure we are on the correct tutorial step
        if (expectedStepIndex >= 0 && 
            TutorialSceneManager.Instance?.CurrentStepIndex != expectedStepIndex)
            return;

        _fired = true;
        
        TutorialSceneManager.Instance?.CompleteCurrentStep();
    }

    public void ResetTarget()
    {
        _fired = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Gizmos.color = new Color(1f, 0.8f, 0f, 0.25f);;
        var col = GetComponent<Collider>();
        
        // This handles drawing the box or sphere target in the Scene view
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