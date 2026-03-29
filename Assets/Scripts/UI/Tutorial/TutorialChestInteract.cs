using UnityEngine;

/// <summary>
/// Place this on your Chest GameObject. 
/// Requires a Trigger Collider to define the "Interaction Range".
/// </summary>
public class TutorialChestInteract : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int expectedStepIndex = -1;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";

    private bool _playerInRange = false;
    private bool _fired = false;

    private void Update()
    {
        if (_fired || !_playerInRange) return;

        // Check for the interaction key press
        if (Input.GetKeyDown(interactKey))
        {
            TryCompleteStep();
        }
    }

    private void TryCompleteStep()
    {
        // Verify tutorial step alignment
        if (expectedStepIndex >= 0 && 
            TutorialSceneManager.Instance?.CurrentStepIndex != expectedStepIndex)
            return;

        _fired = true;
        Debug.Log("[Tutorial] Chest Interacted!");

        TutorialSceneManager.Instance?.CompleteCurrentStep();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            _playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            _playerInRange = false;
        }
    }

    public void ResetInteract()
    {
        _fired = false;
        _playerInRange = false;
    }

    private void OnDrawGizmos()
    {
        // Visualizing the interaction radius in the editor
        Gizmos.color = _playerInRange ? Color.green : Color.cyan;
        var col = GetComponent<Collider>();
        if (col is SphereCollider sphere)
        {
            Gizmos.DrawWireSphere(transform.position, sphere.radius);
        }
        else if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}