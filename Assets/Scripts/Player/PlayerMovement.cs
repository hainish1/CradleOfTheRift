using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private InputSystem_Actions playerInput;
    private InputSystem_Actions.PlayerActions playerActions;

    private InputAction moveActions;
    private InputAction jumpActions;
    // private InputAction attackAction;
    // private InputAction pauseAction;

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool shouldFaceMoveDirection = false;

    [Header("Player References")]
    [SerializeField] private Transform playerCenter;
    private CharacterController characterController;
    private float playerHalfHeight;
    private float playerHalfWidth;
    private float groundedRaycastFloorDistance;
    private float groundedRaycastRadialDistance;

    [Header("Movement Parameters")]
    [SerializeField] private float movementMaxVelocity;
    [SerializeField] private float movementAcceleration;
    [SerializeField] private float movementDeceleration;
    private Vector2 moveInput;
    private Vector3 moveVector;

    [Header("Jump Parameters")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float groundedGravityScale;
    [SerializeField] private float midairGravityScale;
    [SerializeField] private float strafeScale;
    private bool didPerformJump;
    private Vector3 verticalVector;

    [Header("Layer Parameters")]
    [SerializeField] private LayerMask environmentLayer;

    public bool lockControls = false;


    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerInput = new InputSystem_Actions();
        playerActions = playerInput.Player;
    }

    private void OnEnable()
    {

        moveActions = playerActions.Move;
        moveActions.Enable();

        jumpActions = playerActions.Jump;
        jumpActions.Enable();
        jumpActions.started += Jump;

        // attackActions = playerActions.Attack;
        // attackActions.Enable();
        // attackActions.started += Attack;

        // pauseActions = playerInput.UI.Pause;
        // pauseActions.Enable();
        // pauseActions.started += Pause;
    }

    private void OnDisable()
    {
        moveActions.Disable();
        jumpActions.Disable();

        jumpActions.started -= Jump;
    }


    void Start()
    {
        playerHalfHeight = GetComponent<CharacterController>().bounds.extents.y;
        playerHalfWidth = GetComponent<CharacterController>().bounds.extents.x;
        groundedRaycastFloorDistance = playerHalfHeight + 0.1f;
        groundedRaycastRadialDistance = playerHalfWidth * 0.7f;
    }


    void Update()
    {
        if (lockControls) return;

        Move();
    }

    private void Move()
    {
        moveInput = moveActions.ReadValue<Vector2>();
        
        
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        
        
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();
        
        
        Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
        
        
        moveVector = moveDirection;


        if (shouldFaceMoveDirection && moveInput.sqrMagnitude > 0.001f)
        {
            
            Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, 10f * Time.deltaTime);
        }

        
        if (CheckIsGrounded())
        {
            if (didPerformJump)
            {
                verticalVector.y = jumpForce;
            }
            else
            {
                verticalVector.y = 0;
            }

            characterController.Move(Time.deltaTime * verticalVector);
        }
        else
        {
            if (didPerformJump)
            {
                didPerformJump = false;
            }

            verticalVector.y += Physics.gravity.y * groundedGravityScale * Time.deltaTime;

            if (verticalVector.y < Physics.gravity.y * groundedGravityScale)
            {
                verticalVector.y = Physics.gravity.y * groundedGravityScale;
            }

            characterController.Move(Time.deltaTime * verticalVector);
        }

        
        characterController.Move(Time.deltaTime * movementMaxVelocity * moveVector);
    }




    private bool CheckIsGrounded()
    {
        // Exact center of the player's character.
        if (Physics.Raycast(playerCenter.position, Vector2.down, groundedRaycastFloorDistance))
        {
            return true;
        }

        // Four corners of the player's character.
        for (int i = -1; i <= 1; i += 2)
        {
            for (int j = -1; j <= 1; j += 2)
            {
                if (Physics.Raycast(playerCenter.position + new Vector3(groundedRaycastRadialDistance * i, 0, groundedRaycastRadialDistance * j),
                                    Vector2.down, groundedRaycastFloorDistance))
                {
                    return true;
                }
            }
        }

        return false;
    }
    private void Jump(InputAction.CallbackContext context)
    {
        if (CheckIsGrounded())
        {
            didPerformJump = true;
        }
    }


    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.red;

    //    // Exact Center of the player's character.
    //    Gizmos.DrawRay(playerCenter.position, Vector2.down * groundedRaycastFloorDistance);

    //    // Four corners of the player's character.
    //    for (int i = -1; i <= 1; i += 2)
    //    {
    //        for (int j = -1; j <= 1; j += 2)
    //        {
    //            Gizmos.DrawRay(playerCenter.position + new Vector3(groundedRaycastRadialDistance * i, 0, groundedRaycastRadialDistance * j),
    //                           Vector2.down * groundedRaycastFloorDistance);
    //        }
    //    }
    //}



}
