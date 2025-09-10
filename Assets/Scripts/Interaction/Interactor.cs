using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactor : MonoBehaviour
{
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private float interactRange = 0.5f;
    [SerializeField] private LayerMask interactablesLayer;

    private readonly Collider[] colliders = new Collider[3];

    [SerializeField] private int numCollidersFound;

    private void Update()
    {
        // Sets the amount of colliders found within the interaction range
        numCollidersFound = Physics.OverlapSphereNonAlloc(interactionPoint.position, interactRange, colliders, interactablesLayer);
        // if (Input.GetKeyDown(KeyCode.E))
        // {
        //     Ray r = new Ray(InteractorSource.position, InteractorSource.forward);
        //     if (Physics.Raycast(r, out RaycastHit hitInfo, InteractRange, interactablesLayer))
        //     {
        //         if (hitInfo.collider.gameObject.TryGetComponent(out IInteractable interactObj))
        //         {
        //             Debug.Log("test");
        //             interactObj.Interact();
        //         }
        //     }
        // }

        if (numCollidersFound > 0)
        {
            var interactable = colliders[0].GetComponent<IInteractable>();
            if (interactable != null) // Again, replace with interaction key
            {
                //Debug.Log(interactable.InteractionPrompt);
                if (Input.GetKeyDown(KeyCode.E))
                {
                    interactable.Interact(this);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(interactionPoint.position, interactRange);
    }
}
