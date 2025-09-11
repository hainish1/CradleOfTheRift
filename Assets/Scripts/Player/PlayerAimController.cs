using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAimController : MonoBehaviour
{


    [Header("Input")]
    private InputSystem_Actions playerInput;
    private InputSystem_Actions.PlayerActions playerActions;
    private InputAction lookAction;

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


    private float yaw;
    private float pitch;

    void OnEnable()
    {
        if (playerInput == null) playerInput = new InputSystem_Actions();
        playerActions = playerInput.Player;

        lookAction = playerActions.Look;
        lookAction.Enable();

        if (!mainCam) mainCam = Camera.main;

        var e = transform.localRotation.eulerAngles;
        yaw = e.y;
        pitch = Normalize180(e.x);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    void OnDisable()
    {
        lookAction?.Disable();
    }

    
    void Update()
    {
        ReadLook();
        ApplyRotation();
    }


    void LateUpdate()
    {
        UpdateAimTarget();
    }

    private void ReadLook()
    {
        if (lookAction == null) return;
        Vector2 delta = lookAction.ReadValue<Vector2>();
        if (delta.sqrMagnitude < 1e-8f) return;

        float dx = delta.x;
        float dy = delta.y * (invertY ? 1f : -1f);

        
        yaw   += dx * mouseSensitivity;
        pitch += dy * mouseSensitivity;
        
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }


    private void ApplyRotation()
    {
        // cinemachine forward should follow the forward along this axis, basically use this rotation for the camera rotation = voila AIM!
        transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
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

   
    private static float Normalize180(float a)
    {
        while (a > 180f) a -= 360f;
        while (a < -180f) a += 360f;
        return a;
    }

}
