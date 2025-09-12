using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private InputSystem_Actions playerInput;
    private InputSystem_Actions.PlayerActions playerActions;

    private InputAction moveActions;
    private InputAction jumpActions; 


    [Header("Player References")]
    [SerializeField] private Transform playerCenter;
    [SerializeField] private Transform cameraTransform;
    private CharacterController characterController;
    private float playerHalfHeight;
    private float playerHalfWidth;
    private float groundedRaycastFloorDistance;
    private float groundedRaycastRadialDistance;

    [Header("Movement Parameters")]
    [SerializeField] private float movementMaxVelocity;
    [SerializeField] private float movementAcceleration;
    [SerializeField] private float movementDeceleration;
    [SerializeField] private float rotationDamping = 0.25f;
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

        // set by AimController 
    private bool strafe = false;
    public void SetStrafeMode(bool on) => strafe = on;


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

        // CAMERA BASED vector calculations
        Vector3 fwd = cameraTransform ? cameraTransform.forward : Vector3.forward;
        Vector3 right = cameraTransform ? cameraTransform.right : Vector3.right;
        fwd.y = 0;
        right.y = 0;
        fwd.Normalize();
        right.Normalize();

        Vector3 inputDirection = new Vector3(moveInput.x, 0, moveInput.y);
        // moveVector = transform.TransformDirection(inputDirection).normalized;
        moveVector = (right * inputDirection.x + fwd * inputDirection.z);
        if (inputDirection.sqrMagnitude > 1f)
        {
            moveVector.Normalize();
        }

        // GRAVITY AND JUMP STUFF
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

        // facing the movement stuff, turning player around
        if (!strafe && moveVector.sqrMagnitude > 0.0001f)
        {
            Quaternion qa = transform.rotation;
            Quaternion qb = Quaternion.LookRotation(moveVector, Vector3.up);
            float t = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, rotationDamping));
            transform.rotation = Quaternion.Slerp(qa, qb, t);
        }
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



}
