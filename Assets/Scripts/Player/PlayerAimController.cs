using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine.Samples;

public class PlayerAimController : MonoBehaviour
{
    public enum CouplingMode { Coupled, CoupledWhenMoving, Decoupled}
    public bool IsPaused { get; set; }


    // input things
    private InputSystem_Actions playerInput;
    private InputSystem_Actions.PlayerActions playerActions;
    private InputAction lookAction;
    private InputAction moveAction;

    [Header("Sensitivity")]
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private bool invertY = false;

    [Header("Pitch limit")]
    [SerializeField] private float minPitch = -40f;
    [SerializeField] private float maxPitch = 70f;


    [Header("Coupling Stuff")]
    [SerializeField] private CouplingMode playerRotation = CouplingMode.CoupledWhenMoving;
    [SerializeField] private float recenterDamping = 0.2f;
    [SerializeField] private float fireRecenterDamping = 0.05f;
    [SerializeField] private bool snapOnFire = false;
    [SerializeField] private float snapAngleThreshold = 25f;

    [Header("CenterRay aim")]
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask aimMask = ~0;
    private float maxAimDistance = 500f;




    [SerializeField] private Transform playerRoot;
    [SerializeField] private AimTargetManager aimTargetManager;

    private CharacterController cc; // cached
    private PlayerMovement movement;
    private PlayerMovement movementV4;


    private float yaw;
    private float pitch;

    private bool lookChangedThisFrame;

    private float lastPlayerYaw;
    private bool coupledThisFrame;


    float forceCoupleTimer = 0f;
    const float kForceCoupleDuration = 0.3f; // sec

    public void ForceCoupleOnFire() => forceCoupleTimer = kForceCoupleDuration;

    public Vector3 GetAimDirection(Vector3 origin, Vector3 fallbackForward)
    {
        // if (aimTargetManager != null)
        // {
        //     return (aimTargetManager.transform.position - origin).normalized;
        // }
        // return fallbackForward;

        if (!cam) cam = Camera.main;
        if (!cam) return fallbackForward;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out var hit, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore))
        {
            return (hit.point - origin).normalized;
        }
        return ray.direction;
    }

    void OnEnable()
    {
        if (playerInput == null) playerInput = new InputSystem_Actions();
        playerActions = playerInput.Player;

        lookAction = playerActions.Look;
        moveAction = playerActions.Move;
        lookAction.Enable();
        moveAction.Enable();

        if (!playerRoot && transform.parent) playerRoot = transform.parent;
        if (playerRoot)
        {
            cc = playerRoot.GetComponent<CharacterController>();
            // Try PlayerMovement first, then PlayerMovementV4 for compatibility
            movement = playerRoot.GetComponent<PlayerMovement>();
            if (movement == null)
            {
                movementV4 = playerRoot.GetComponent<PlayerMovement>();
            }
            lastPlayerYaw = playerRoot.eulerAngles.y;
        }

        var e = transform.localRotation.eulerAngles;
        yaw = e.y;
        pitch = Normalize180(e.x);
        pitch = ClampPitch(pitch);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        lookAction?.Disable();
        moveAction?.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (IsPaused) return;   // <--- NEW

        if (forceCoupleTimer > 0f) forceCoupleTimer -= Time.unscaledDeltaTime;

        lookChangedThisFrame = ReadLook(); // did my mouse move


        bool moving = IsMoving();
        bool aiming = lookChangedThisFrame;
        bool forceCouple = forceCoupleTimer > 0;

        bool shouldCouple = false;
        switch (playerRotation)
        {
            case CouplingMode.Coupled:
                shouldCouple = true;
                SetStrafe(true);
                break;
            case CouplingMode.CoupledWhenMoving:
                shouldCouple = (moving && aiming) || forceCouple;
                SetStrafe(shouldCouple);
                break;
            case CouplingMode.Decoupled:
                shouldCouple = forceCouple;
                SetStrafe(true);
                break;
        }
        coupledThisFrame = shouldCouple;

        if (shouldCouple)
        {
            if (forceCouple && snapOnFire)
            {
                
                Vector3 aimFlat = transform.forward; aimFlat.y = 0f;
                Vector3 playerFlat = playerRoot.forward; playerFlat.y = 0f;
                float targetYaw  = Mathf.Atan2(aimFlat.x,   aimFlat.z) * Mathf.Rad2Deg;
                float currentYaw = Mathf.Atan2(playerFlat.x,playerFlat.z)* Mathf.Rad2Deg;
                float delta = Mathf.DeltaAngle(currentYaw, targetYaw);
                if (Mathf.Abs(delta) > snapAngleThreshold)
                {
                    playerRoot.rotation = Quaternion.Euler(0f, targetYaw, 0f);
                    yaw -= delta; // keep camera stable
                }
                else
                {
                    RecenterPlayerTowardsAim(fireRecenterDamping); // fast
                }
            }
            else
            {
                RecenterPlayerTowardsAim(); // normal damping
            }
        }

        transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        
    }
    void LateUpdate()
    {
        if (playerRoot)
        {
            float currentYaw = playerRoot.eulerAngles.y;
            float deltaPlayerYaw = Mathf.DeltaAngle(lastPlayerYaw, currentYaw);

            if (!coupledThisFrame && Mathf.Abs(deltaPlayerYaw) > 0.0001f)
            {
                yaw -= deltaPlayerYaw;
                transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);

            }
            lastPlayerYaw = currentYaw;
        }


        // UpdateAimTarget();
    }

    private bool ReadLook()
    {
        if (lookAction == null) return false;
        Vector2 delta = lookAction.ReadValue<Vector2>();
        if (delta.sqrMagnitude < 1e-8f) return false;

        float dx = delta.x;
        float dy = delta.y * (invertY ? 1f : -1f);


        yaw += dx * mouseSensitivity;
        pitch += dy * mouseSensitivity;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        return true;
    }


    private void RecenterPlayerTowardsAim(float dampingOverride = -1f)
    {
        if (!playerRoot) return;

        float damping = (dampingOverride >= 0f) ? dampingOverride : recenterDamping;

        Vector3 aimFlat = transform.forward; aimFlat.y = 0f;
        if (aimFlat.sqrMagnitude < 1e-6f) return;

        Vector3 playerFlat = playerRoot.forward; playerFlat.y = 0f;
        if (playerFlat.sqrMagnitude < 1e-6f) playerFlat = Vector3.forward;

        float currentYaw = Mathf.Atan2(playerFlat.x, playerFlat.z) * Mathf.Rad2Deg;
        float targetYaw = Mathf.Atan2(aimFlat.x, aimFlat.z) * Mathf.Rad2Deg;
        
        float t = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, damping));
        float newYaw = Mathf.LerpAngle(currentYaw, targetYaw, t);

        // Apply delta to player root
        float deltaYaw = Mathf.DeltaAngle(currentYaw, newYaw);
        playerRoot.rotation = Quaternion.Euler(0f, deltaYaw, 0f) * playerRoot.rotation;

        // cancel so player dont double rotate
        yaw -= deltaYaw;


    }

    private bool IsMoving()
    {
        if (moveAction != null && moveAction.ReadValue<Vector2>().sqrMagnitude > 0.001f)
        {
            return true;
        }

        if (cc != null && cc.velocity.sqrMagnitude > 0.01f) return true;
        return false; 
    }


    private void SetStrafe(bool on)
    {
        if (movement != null)
        {
            movement.SetStrafeMode(on);
        }
        else if (movementV4 != null)
        {
            movementV4.SetStrafeMode(on);
        }
    }


    private float ClampPitch(float p) => Mathf.Clamp(p, minPitch, maxPitch);

    private static float Normalize180(float a)
    {
        while (a > 180f) a -= 360f;
        while (a < -180f) a += 360f;
        return a;
    }
    // Called by PauseManager to freeze all camera look input.
    public void SetLookEnabled(bool enabled)
    {
        if (enabled)
        {
            lookAction?.Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            lookAction?.Disable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

}
