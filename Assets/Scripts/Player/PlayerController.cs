using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    private InputSystem_Actions playerInput;
    private InputSystem_Actions.PlayerActions playerActions;

    private InputAction lookActions;
    private InputAction moveActions;
    private InputAction jumpActions;
    private InputAction dashActions;
    private InputAction attackActions;
    private InputAction pauseActions;

    [Header("Player References")]
    [SerializeField] private Transform playerCenter;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Transform cameraMount;
    private Rigidbody playerRB;
    private float playerHalfHeight;
    private float playerHalfWidth;
    private float groundedRaycastFloorDistance;
    private float groundedRaycastRadialDistance;

    [Header("Look Parameters")]
    [SerializeField] private float xSensitivity;
    [SerializeField] private float ySensitivity;
    [SerializeField] private float upwardClampAngle;
    [SerializeField] private float downwardClampAngle;
    [SerializeField] private float cameraLerpSpeed;
    [Range(0, 1)][SerializeField] private float cameraCollisionOffset;
    private int cameraCollisionMasks;
    private float xRotation;
    private float yRotation;
    private Vector2 lookInput;

    [Header("Movement Parameters")]
    [SerializeField] private float m_MaxSpeed;
    [SerializeField] private float m_AccelerationSeconds;
    [SerializeField] private float m_LinearDamping;
    private float m_Acceleration;
    private Vector2 moveInput;
    private Vector3 moveInputUnitVector;

    [Header("Jump Parameters")]
    [SerializeField] private float jumpForce;
    [SerializeField] private int jumpMaxCharges;
    [SerializeField] private float gravityScale;
    [SerializeField] private float strafeScale;
    private bool didPerformJump;
    private int jumpCharges;
    private Vector3 gravityVector;

    //[Header("Coyote Time Parameters")]
    //[SerializeField] private float earlyCoyoteTime;
    //[SerializeField] private float lateCoyoteTime;
    //private float earlyCoyoteTimer;
    //private float lateCoyoteTimer;
    //private bool canEarlyJump;

    [Header("Dash Parameters")]
    [SerializeField] private float dashForce;
    [SerializeField] private float dashCooldown;
    [SerializeField] private int dashCharges;

    //[Header("UI References")]
    //public Image crosshair;
    //public Image pauseMenu;
    //public bool gamePaused;

    [Header("Layer Parameters")]
    [SerializeField] private LayerMask environmentLayer;
    private RaycastHit hitInfo;

    public bool lockControls = false;



    private void Awake()
    {
        //characterController = GetComponent<CharacterController>();
        playerRB = GetComponent<Rigidbody>();
        playerInput = new InputSystem_Actions();
        playerActions = playerInput.Player;
    }

    private void OnEnable()
    {
        lookActions = playerActions.Look;
        lookActions.Enable();

        moveActions = playerActions.Move;
        moveActions.Enable();

        jumpActions = playerActions.Jump;
        jumpActions.Enable();
        jumpActions.started += JumpInputAction;

        dashActions = playerActions.Dash;
        dashActions.Enable();
        dashActions.started += DashInputAction;

        attackActions = playerActions.Attack;
        attackActions.Enable();
        attackActions.started += AttackInputAction;

        pauseActions = playerInput.UI.Pause;
        pauseActions.Enable();
        pauseActions.started += PauseInputAction;
    }

    private void OnDisable()
    {
        lookActions.Disable();
        moveActions.Disable();
        jumpActions.Disable();
        dashActions.Disable();
        attackActions.Enable();
        pauseActions.Disable();

        jumpActions.started -= JumpInputAction;
        dashActions.started -= DashInputAction;
        attackActions.started -= AttackInputAction;
        pauseActions.started -= PauseInputAction;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerHalfHeight = GetComponent<CapsuleCollider>().bounds.extents.y;
        playerHalfWidth = GetComponent<CapsuleCollider>().bounds.extents.x;
        groundedRaycastFloorDistance = playerHalfHeight + 0.1f;
        groundedRaycastRadialDistance = playerHalfWidth * 0.7f;

        cameraCollisionMasks = environmentLayer.value;

        m_Acceleration = m_MaxSpeed / m_AccelerationSeconds;
        playerRB.linearDamping = m_LinearDamping;

        gravityVector = gravityScale * Physics.gravity;
        jumpCharges = jumpMaxCharges;

        //crosshair.enabled = true;
        //pauseMenu.enabled = false;
        
        //foreach (Transform child in pauseMenu.transform)
        //{
        //    child.gameObject.SetActive(false);
        //}
        //gamePaused = false;
    }

    void Update()
    {
        if (lockControls) return;

        Look();
    }

    private void FixedUpdate()
    {
        Move();
    }

    void LateUpdate()
    {
        if (lockControls) return;
        
        CameraCollision();
    }


    private void Look()
    {
        if (lockControls) return;

        lookInput = lookActions.ReadValue<Vector2>();

        // NOTE: lookInput.x and lookInput.y are not mistakenly swapped.
        xRotation -= Time.deltaTime * xSensitivity * lookInput.y;
        yRotation += Time.deltaTime * ySensitivity * lookInput.x;
        xRotation = Mathf.Clamp(xRotation, downwardClampAngle, upwardClampAngle);

        transform.rotation = Quaternion.Euler(0, yRotation, 0); // Rotate the player object horizontally around the y axis.
        cameraPivot.rotation = Quaternion.Euler(xRotation, yRotation, 0); // Rotate the player's camera pivot around the x and y axes.
    }


    private void CameraCollision()
    {
        // Raycast only registers for Environment.
        if (Physics.Linecast(cameraPivot.position, cameraMount.position, out hitInfo, cameraCollisionMasks))
        {
            Vector3 newCameraPosition = new Vector3(hitInfo.point.x, hitInfo.point.y, hitInfo.point.z);
            playerCamera.position = newCameraPosition * cameraCollisionOffset;
        }
        else
        {
            if (playerCamera.position != cameraMount.position)
            {
                playerCamera.position = cameraMount.position;
            }
        }
    }


    private Vector3 GetMoveInputDirection()
    {
        moveInput = moveActions.ReadValue<Vector2>();
        Vector3 inputDirection = new Vector3(moveInput.x, 0, moveInput.y);
        
        return transform.TransformDirection(inputDirection).normalized;
    }


    private float GetLinearDampingCorrection()
    {
        float appliedDamping = 1 - m_LinearDamping * Time.deltaTime;
        return 1 / appliedDamping;
    }
    

    private void Move()
    {
        moveInputUnitVector = GetMoveInputDirection();

        if (!IsGrounded())
        {
            if (didPerformJump)
            {
                didPerformJump = false;
            }

            ApplyGravity();
        }

        
        if (moveInputUnitVector != Vector3.zero)
        {
            // IS NOT TAKING INTO ACCOUNT THE DIRECTION OF THE CURRSPEED



            float movementDampingCorrection = GetLinearDampingCorrection();

            float linearVelX = playerRB.linearVelocity.x;
            float linearVelY = playerRB.linearVelocity.y;
            float linearVelZ = playerRB.linearVelocity.z;
            playerRB.linearVelocity = new Vector3(linearVelX * movementDampingCorrection,
                                                  linearVelY,
                                                  linearVelZ * movementDampingCorrection);

            Vector3 currSpeed = playerRB.linearVelocity;
            Vector3 accelIncrement = Time.deltaTime * (m_MaxSpeed / m_AccelerationSeconds) * moveInputUnitVector;

            if (currSpeed.magnitude < m_MaxSpeed)
            {
                if (currSpeed.magnitude + accelIncrement.magnitude > m_MaxSpeed)
                {
                    accelIncrement = (m_MaxSpeed - currSpeed.magnitude) * moveInputUnitVector;
                }

                playerRB.AddForce(accelIncrement, ForceMode.VelocityChange);
            }

            Debug.Log(playerRB.linearVelocity.magnitude);
        }
    }


    private bool IsGrounded()
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


    private void JumpInputAction(InputAction.CallbackContext context)
    {
        if (IsGrounded())
        {
            didPerformJump = true;
            playerRB.AddForce(jumpForce * Vector3.up, ForceMode.Force);
        }
    }


    private void ApplyGravity()
    {
        float gravityDampingCorrection = GetLinearDampingCorrection();

        float linearVelX = playerRB.linearVelocity.x;
        float linearVelY = playerRB.linearVelocity.y;
        float linearVelZ = playerRB.linearVelocity.z;
        playerRB.linearVelocity = new Vector3(linearVelX, linearVelY * gravityDampingCorrection, linearVelZ);

        playerRB.AddForce(gravityVector, ForceMode.Acceleration);
    }


    private void DashInputAction(InputAction.CallbackContext context)
    {
        if (dashCharges != 0)
        {
            playerRB.AddForce(dashForce * GetMoveInputDirection(), ForceMode.Force);
        }

        StartCoroutine(InitiateDashCooldown(dashCooldown));
    }


    private IEnumerator InitiateDashCooldown(float seconds)
    {
        dashCharges--;

        yield return new WaitForSeconds(seconds);

        dashCharges++;
    }


    private void AttackInputAction(InputAction.CallbackContext context)
    {
        
    }


    private void PauseInputAction(InputAction.CallbackContext context)
    {
        //if (gamePaused)
        //{
        //    gamePaused = false;
        //    Time.timeScale = 1;

        //    lockControls = false;
        //    Cursor.lockState = CursorLockMode.Locked;
        //    Cursor.visible = false;
        //    crosshair.enabled = true;
        //    pauseMenu.enabled = false;
            
        //    foreach (Transform child in pauseMenu.transform)
        //    {
        //        child.gameObject.SetActive(false);
        //    }
        //}
        //else
        //{
        //    gamePaused = true;
        //    Time.timeScale = 0;

        //    lockControls = true;
        //    Cursor.lockState = CursorLockMode.None;
        //    Cursor.visible = true;
        //    crosshair.enabled = false;
        //    pauseMenu.enabled = true;
            
        //    foreach (Transform child in pauseMenu.transform)
        //    {
        //        child.gameObject.SetActive(true);
        //    }
        //}
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
