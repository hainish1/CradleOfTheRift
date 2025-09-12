using System;
using Unity.Cinemachine.Samples;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerShooter : MonoBehaviour
{
    [Header("AimReferences")]
    [SerializeField] private PlayerAimController aim; // Player AIming core
    [SerializeField] private Transform muzzle; // our cube thing
    [SerializeField] private AimTargetManager aimTargetManager;
    [SerializeField] private LayerMask shootMask = ~0;


    [Header("Fire info")]
    [SerializeField] private float fireRate = 10f;
    [SerializeField] private float maxDistance = 200f;

    [Header("Debug")]
    [SerializeField] private Color tracerColor = Color.red;
    [SerializeField] private float tracerTime = 0.05f;

    private InputSystem_Actions input;
    private InputSystem_Actions.PlayerActions actions;
    private InputAction fireAction;

    private bool isFiring;
    private float nextFireTime;

    void OnEnable()
    {
        if (input == null) input = new InputSystem_Actions();
        actions = input.Player;
        fireAction = actions.Attack;
        if (fireAction != null)
        {
            fireAction.Enable();
            fireAction.started += OnFireStarted;
            fireAction.performed += OnFirePerformed;
            fireAction.canceled += OnFireCancelled;

        }
    }

    void OnDisable()
    {
        if (fireAction != null)
        {
            fireAction.started -= OnFireStarted;
            fireAction.performed -= OnFirePerformed;
            fireAction.canceled -= OnFireCancelled;
            fireAction.Disable();
        }
    }


    void Update()
    {
        if (isFiring) TryToFire();
    }


    private void OnFireStarted(InputAction.CallbackContext _)
    {
        isFiring = true;
        aim?.ForceCoupleOnFire();
        TryToFire(true);
    }

    private void OnFirePerformed(InputAction.CallbackContext _)
    {
        isFiring = false;
    }

    private void OnFireCancelled(InputAction.CallbackContext _)
    {
        isFiring = false;
    }

    private void TryToFire(bool force = false)
    {
        if (!aim || !muzzle) return;

        if (force || Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + (1f / Mathf.Max(0.01f, fireRate));

            
            // Vector3 direction = aim.AimDirectionFrom(muzzle);
            Vector3 direction = aimTargetManager ? (aimTargetManager.transform.position - muzzle.position).normalized : aim.GetAimDirection(muzzle.position, muzzle.forward);

            Vector3 start = muzzle.position;

            if (Physics.Raycast(start, direction, out var hit, maxDistance, shootMask, QueryTriggerInteraction.Ignore))
            {
                Debug.DrawLine(start, hit.point, tracerColor, tracerTime);
            }
            else
            {
                Debug.DrawLine(start, start + direction * maxDistance, tracerColor, tracerTime);
            }
        }
    }

}
