using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Interactor : MonoBehaviour
{
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private float interactRange = 0.5f;
    [SerializeField] private LayerMask interactablesLayer;
    [SerializeField] private InteractionPromptUI interactionPromptUI;
    private InputSystem_Actions inputActions;
    private InputSystem_Actions.PlayerActions playerActions;
    private InputAction interactionActions;

    private readonly Collider[] colliders = new Collider[3];

    [SerializeField] private int numCollidersFound;

    private IInteractable interactable;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        playerActions = inputActions.Player;
    }

    private void OnEnable()
    {
        interactionActions = playerActions.Interact;
        interactionActions.Enable();
        interactionActions.started += InteractActionStarted;
    }

    private void OnDisable()
    {
        interactionActions.Disable();
        interactionActions.started -= InteractActionStarted;
    }

    private void InteractActionStarted(InputAction.CallbackContext context)
    {
        if (interactable != null)
        {
            interactable.Interact(this);
            if (interactable.SingleActivation)
            {
                //interactable = null;
                interactionPromptUI.HidePrompt();
            }
        }
    }

    private void Update()
    {
        // Sets the amount of colliders found within the interaction range
        numCollidersFound = Physics.OverlapSphereNonAlloc(interactionPoint.position, interactRange, colliders, interactablesLayer);

        if (numCollidersFound > 0)
        {
            interactable = colliders[0].GetComponent<IInteractable>();
            if (interactable != null)
            {
                if (!interactionPromptUI.isDisplayed)
                {
                    interactionPromptUI.ShowPrompt(interactable.InteractionPrompt);
                }

                //Debug.Log(interactable.InteractionPrompt);
                //if (Input.GetKeyDown(KeyCode.E))    // Again, replace with interaction key
                //{
                //    interactable.Interact(this);

                //    if (interactable.SingleActivation)
                //    {
                //        //interactable = null;
                //        interactionPromptUI.HidePrompt();
                //    }
                //}
            }
        }
        else
        {
            if (interactable != null) interactable = null;
            if (interactionPromptUI.isDisplayed)
            {
                interactionPromptUI.HidePrompt();
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(interactionPoint.position, interactRange);
    }
}
