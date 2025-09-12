using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAimController : MonoBehaviour
{
    public enum CouplingMode { Coupled, CoupledWhenMoving, Decoupled}


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


    [Header("Aim Target (world point)")]
    [SerializeField] private Camera mainCam;
    [SerializeField] private Transform aimTarget;
    [SerializeField] private LayerMask aimLayers = ~0;
    [SerializeField] private float defaultDistance = 30f;
    [SerializeField] private float minAimDistance = 3f;

    [Header("Coupling Stuff")]
    [SerializeField] private CouplingMode playerRotation = CouplingMode.CoupledWhenMoving;
    [SerializeField] private float recenterDamping = 0.2f;

    [SerializeField] private Transform playerRoot;

    private CharacterController cc; // cached
    private PlayerMovement movement;


    private float yaw;
    private float pitch;

    private bool lookChangedThisFrame;

    private float lastPlayerYaw;
    private bool coupledThisFrame;

    void OnEnable()
    {
        if (playerInput == null) playerInput = new InputSystem_Actions();
        playerActions = playerInput.Player;

        lookAction = playerActions.Look;
        moveAction = playerActions.Move;
        lookAction.Enable();
        moveAction.Enable();

        if (!mainCam) mainCam = Camera.main;

        if (!playerRoot && transform.parent) playerRoot = transform.parent;
        if (playerRoot)
        {
            cc = playerRoot.GetComponent<CharacterController>();
            movement = playerRoot.GetComponent<PlayerMovement>();
            lastPlayerYaw = playerRoot.eulerAngles.y;
        }
        
        var e = transform.localRotation.eulerAngles;
        yaw = e.y;
        pitch = Normalize180(e.x);
        pitch = ClampPitch(pitch);
    }

    void OnDisable()
    {
        lookAction?.Disable();
        moveAction?.Disable();
    }


    void Update()
    {
        lookChangedThisFrame = ReadLook(); // did my mouse move

        bool shouldCouple = false;

        switch (playerRotation)
        {
            case CouplingMode.Coupled:
                shouldCouple = true;
                SetStrafe(true);
                break;
            case CouplingMode.CoupledWhenMoving:
                bool moving = IsMoving();
                bool aiming = lookChangedThisFrame;
                shouldCouple = moving && aiming;
                SetStrafe(shouldCouple);
                break;
            case CouplingMode.Decoupled:
                shouldCouple = false;
                SetStrafe(true);
                break;
        }
        coupledThisFrame = shouldCouple;

        if (shouldCouple)
        {
            RecenterPlayerTowardsAim();
        }

        // // recenter before we set aimCore transforms
        // ApplyCoupling();

        // ApplyAimCoreRotation();

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


        UpdateAimTarget();
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

        // pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        pitch = ClampPitch(pitch);
        return true;
    }


    private void RecenterPlayerTowardsAim()
    {
        if (!playerRoot) return;

        
        Vector3 aimFlat = transform.forward; aimFlat.y = 0f;
        if (aimFlat.sqrMagnitude < 1e-6f) return;

        Vector3 playerFlat = playerRoot.forward; playerFlat.y = 0f;
        if (playerFlat.sqrMagnitude < 1e-6f) playerFlat = Vector3.forward;

        float currentYaw = Mathf.Atan2(playerFlat.x, playerFlat.z) * Mathf.Rad2Deg;
        float targetYaw = Mathf.Atan2(aimFlat.x, aimFlat.z) * Mathf.Rad2Deg;
        
        float t = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, recenterDamping));
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
    }

    private void UpdateAimTarget()
    {
        if (!mainCam || !aimTarget) return;

        float start = mainCam.nearClipPlane + 0.05f;
        Ray ray = new Ray(mainCam.transform.position + mainCam.transform.forward * start,
                          mainCam.transform.forward);

        if (Physics.Raycast(ray, out var hit, 1000f, aimLayers, QueryTriggerInteraction.Ignore))
        {
            float d = Vector3.Distance(mainCam.transform.position, hit.point);
            aimTarget.position = (d < minAimDistance)
                ? mainCam.transform.position + mainCam.transform.forward * minAimDistance
                : hit.point;
        }
        else
        {
            aimTarget.position = mainCam.transform.position + mainCam.transform.forward * defaultDistance;
        }
    }

    // private void ApplyAimCoreRotation()
    // {
    //     // cinemachine forward should follow the forward along this axis, basically use this rotation for the camera rotation = voila AIM!
    //     transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    // }


    private float ClampPitch(float p) => Mathf.Clamp(p, minPitch, maxPitch);
   
    private static float Normalize180(float a)
    {
        while (a > 180f) a -= 360f;
        while (a < -180f) a += 360f;
        return a;
    }

}
