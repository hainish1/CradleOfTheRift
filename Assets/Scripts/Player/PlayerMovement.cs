using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private InputSystem_Actions playerInput;
    private InputSystem_Actions.PlayerActions playerActions;

    private InputAction moveActions;
    private InputAction jumpActions;
    private InputAction dashActions;


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

    [Header("KnockBack Parameters")]
    [SerializeField] private float externalDamping = 8f;
    [SerializeField] private float kbControlsLock = 0.18f;
    [SerializeField] private float kbDashLock = 0.12f; // for how much time after being knocked back can we not do dash, safety lock
    private Vector3 externalVelocity; // our fake stuff
    private float kbLockTimer; // blocks movement + rotaion
    private float kbDashLockTimer; // blocks dash

    [Header("Jump Parameters")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float groundedGravityScale;
    [SerializeField] private float midairGravityScale;
    [SerializeField] private float strafeScale;
    private bool didPerformJump;
    private Vector3 verticalVector;
    [Space]

    [Header("Dash Parameters")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDistance = 8f;
    [SerializeField] private float dashCooldown = .6f;
    [SerializeField] private int dashMaxCharges = 1;
    private bool isDashing;
    private int dashCharges;
    private Vector3 dashVector;

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

        dashActions = playerActions.Dash;
        dashActions.Enable();
        dashActions.started += Dash;

    }

    private void OnDisable()
    {
        moveActions.Disable();
        jumpActions.Disable();

        jumpActions.started -= Jump;
        dashActions.Disable();
        dashActions.started -= Dash;
    }


    void Start()
    {
        playerHalfHeight = GetComponent<CharacterController>().bounds.extents.y;
        playerHalfWidth = GetComponent<CharacterController>().bounds.extents.x;
        groundedRaycastFloorDistance = playerHalfHeight + 0.1f;
        groundedRaycastRadialDistance = playerHalfWidth * 0.7f;

        dashCharges = dashMaxCharges;
    }


    void Update()
    {
        if (lockControls) return;

        if (kbLockTimer > 0f) kbLockTimer -= Time.deltaTime;
        if (kbDashLockTimer > 0f) kbDashLockTimer -= Time.deltaTime;

        Move();
    }

    private void Move()
    {
        // moveInput = moveActions.ReadValue<Vector2>();
        moveInput = (kbLockTimer > 0f) ? Vector2.zero : moveActions.ReadValue<Vector2>(); // only read input if lock timer is over

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

        // characterController.Move(Time.deltaTime * movementMaxVelocity * moveVector);
        // Dash overrides normal movement while active
        if (isDashing)
        {
            // I am making a CHANGE here to use moveVector instead of dashVector, to fix the direction bug while backward dash
            characterController.Move(moveVector * dashSpeed * Time.deltaTime);
        }
        else
        {
            characterController.Move(Time.deltaTime * movementMaxVelocity * moveVector);

            // Apply knockback after normal movement
            if (externalVelocity.sqrMagnitude > 1e-6f)
            {
                // optional clamp to avoid crazy impulses
                // if (externalVelocity.magnitude > 100f)
                //     externalVelocity = externalVelocity.normalized * 100f;

                characterController.Move(externalVelocity * Time.deltaTime);
                externalVelocity = Vector3.Lerp(externalVelocity, Vector3.zero, externalDamping * Time.deltaTime);
            }
        }

        // facing the movement stuff, turning player around
        if (kbLockTimer <= 0f && !strafe && moveVector.sqrMagnitude > 0.0001f)
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


    // DASH IMPLEMENTATION
    // ALL OF SAMUEL's CODE

    private void Dash(InputAction.CallbackContext context)
    {
        // first check if we are being knocked back
        if (kbDashLockTimer > 0f) return;

        if (!isDashing && dashCharges != 0)
        {
            moveInput = moveActions.ReadValue<Vector2>();

            if (moveInput.x == 0 && moveInput.y == 0)
            {
                dashVector = GetComponentInParent<Transform>().forward;
            }
            else
            {
                Vector3 inputDirection = new Vector3(moveInput.x, 0, moveInput.y);
                dashVector = transform.TransformDirection(inputDirection).normalized;
            }

            StartCoroutine(InitiateDashCooldown(dashCooldown));
            StartCoroutine(InitiateDashDuration(dashDistance / dashSpeed));
        }
    }

    private IEnumerator InitiateDashCooldown(float seconds)
    {
        dashCharges--;

        yield return new WaitForSeconds(seconds);

        dashCharges++;
    }

    private IEnumerator InitiateDashDuration(float seconds)
    {
        isDashing = true;

        yield return new WaitForSeconds(seconds);

        isDashing = false;
    }




    // FAKE KNOCKBACK STUFF, Called by ENEMIES
    public void ApplyImpulse(Vector3 impulse)
    {
        externalVelocity += impulse;

        // start lock timers
        kbLockTimer = Mathf.Max(kbLockTimer, kbControlsLock);
        kbDashLockTimer = Mathf.Max(kbDashLockTimer, kbControlsLock + kbDashLock);

        isDashing = false; // just extra safety
    }




}
