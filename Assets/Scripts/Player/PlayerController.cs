using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    private CharacterController characterController;
    private InputSystem_Actions playerInput;
    public InputSystem_Actions.PlayerActions playerActions;

    private InputAction lookActions;
    private InputAction moveActions;
    private InputAction jumpActions;
    private InputAction attackActions;
    private InputAction pauseActions;

    [Header("Player References")]
    [SerializeField] private Transform playerCenter;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Transform cameraMount;
    private float playerHalfHeight;
    private float playerHalfWidth;

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

    [Header("Movement Parameters")]
    [SerializeField] private float maxVelocity;

    [Header("Jump Parameters")]
    [SerializeField] private float JumpForce;
    [SerializeField] private float gravityScale;
    private bool playerJumped;

    [Header("UI References")]
    public Image crosshair;
    public Image pauseMenu;
    public bool gamePaused;

    [Header("Layer Parameters")]
    [SerializeField] private LayerMask environmentLayer;
    private RaycastHit hitInfo;

    private Vector2 lookInput;
    private Vector2 moveInput;
    private Vector3 moveVector;
    private Vector3 verticalVector;

    public bool lockControls = false;



    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
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
        jumpActions.started += Jump;

        attackActions = playerActions.Attack;
        attackActions.Enable();
        attackActions.started += Attack;

        pauseActions = playerInput.UI.Pause;
        pauseActions.Enable();
        pauseActions.started += Pause;
    }

    private void OnDisable()
    {
        lookActions.Disable();
        moveActions.Disable();
        jumpActions.Disable();
        attackActions.Enable();
        pauseActions.Disable();

        jumpActions.started -= Jump;
        attackActions.started -= Attack;
        pauseActions.started -= Pause;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerHalfHeight = GetComponent<CharacterController>().bounds.extents.y;
        playerHalfWidth = GetComponent<CharacterController>().bounds.extents.x * 0.7f;

        cameraCollisionMasks = environmentLayer.value;

        crosshair.enabled = true;
        pauseMenu.enabled = false;
        
        foreach (Transform child in pauseMenu.transform)
        {
            child.gameObject.SetActive(false);
        }
        gamePaused = false;
    }

    void Update()
    {
        if (lockControls) return;

        Look();
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

        // NOTE: lookInput.y and lookInput.x are not mistakenly swapped.
        xRotation -= Time.deltaTime * xSensitivity * lookInput.y;
        yRotation += Time.deltaTime * ySensitivity * lookInput.x;
        xRotation = Mathf.Clamp(xRotation, downwardClampAngle, upwardClampAngle);

        transform.rotation = Quaternion.Euler(0, yRotation, 0); // Rotate the player object horizontally around the y axis.
        cameraPivot.rotation = Quaternion.Euler(xRotation, yRotation, 0); // Rotate the player's camera pivot around the x and y axes.
    }

    private void CameraCollision()
    {

        // Raycast only registers for Environment, Grabbable and Button LayerMasks.
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
    
    private void Move()
    {

        moveInput = moveActions.ReadValue<Vector2>();
        Vector3 inputDirection = new Vector3(moveInput.x, 0, moveInput.y);
        moveVector = transform.TransformDirection(inputDirection).normalized;

        if (CheckIsGrounded())
        {
            if (playerJumped)
            {
                verticalVector.y = JumpForce;
            }
            else
            {
                verticalVector.y = 0;
            }

            characterController.Move(Time.deltaTime * verticalVector);
        }
        else
        {
            if (playerJumped)
            {
                playerJumped = false;
            }

            verticalVector.y += Physics.gravity.y * gravityScale * Time.deltaTime;

            if (verticalVector.y < Physics.gravity.y * gravityScale)
            {
                verticalVector.y = Physics.gravity.y * gravityScale;
            }

            characterController.Move(Time.deltaTime * verticalVector);
        }

        characterController.Move(Time.deltaTime * maxVelocity * moveVector);
    }

    private bool CheckIsGrounded()
    {
        // Exact center of the player's character.
        if (Physics.Raycast(playerCenter.position, Vector2.down, playerHalfHeight + 0.1f))
        {
            return true;
        }

        // Four corners of the player's character.
        for (int i = -1; i <= 1; i += 2)
        {
            for (int j = -1; j <= 1; j += 2)
            {
                if (Physics.Raycast(playerCenter.position
                        + new Vector3(playerHalfWidth * i, 0, playerHalfWidth * j), Vector2.down, playerHalfHeight + 0.1f))
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
            playerJumped = true;
        }
    }

    private void Attack(InputAction.CallbackContext context)
    {
        
    }

    private void Pause(InputAction.CallbackContext context)
    {
        if (gamePaused)
        {
            gamePaused = false;
            Time.timeScale = 1;

            lockControls = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            crosshair.enabled = true;
            pauseMenu.enabled = false;
            
            foreach (Transform child in pauseMenu.transform)
            {
                child.gameObject.SetActive(false);
            }
        }
        else
        {
            gamePaused = true;
            Time.timeScale = 0;

            lockControls = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            crosshair.enabled = false;
            pauseMenu.enabled = true;
            
            foreach (Transform child in pauseMenu.transform)
            {
                child.gameObject.SetActive(true);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        // Exact Center of the player's character.
        Gizmos.DrawRay(playerCenter.position, Vector2.down * (playerHalfHeight + 0.1f));

        // Four corners of the player's character.
        for (int i = -1; i <= 1; i += 2)
        {
            for (int j = -1; j <= 1; j += 2)
            {
                Gizmos.DrawRay(playerCenter.position + new Vector3(playerHalfWidth * i, 0, playerHalfWidth * j), Vector2.down * (playerHalfHeight + 0.1f));
            }
        }
    }
}
