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
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private bool fullAuto = true;

    [Header("Projectiles")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private float projectileSpeed = 50f;
    [SerializeField] private float spawnOffset = 0.1f; // out of the muzzle alr 

    private InputSystem_Actions input;
    private InputSystem_Actions.PlayerActions actions;
    private InputAction fireAction;

    private Entity playerEntity; // REF FOR STATS

    private bool isFiring;
    private float nextFireTime;

    // projectiles should ignore their own kind
    Collider[] selfColliders;

    void Start()
    {
        playerEntity = GetComponent<Entity>();

        var input = new InputAction("Toggle Spawning", binding: "<Keyboard>/b");
        input.performed += _ => ToggleFullAuto();
        input.Enable();
    }

    private void ToggleFullAuto()
    {
        fullAuto = !fullAuto;// toggling between true and false
        Debug.Log("Full auto is now " + (fullAuto ? "enabled" : "disabled"));
    }

    void OnEnable()
    {
        if (input == null) input = new InputSystem_Actions();
        actions = input.Player;
        fireAction = actions.Melee; // Changed to Melee (right click in future)
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
        if (!aim || !muzzle) return;
        Vector3 direction = aim.GetAimDirection(muzzle.position, muzzle.forward);
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction, Vector3.up);
            muzzle.rotation = Quaternion.Slerp(muzzle.rotation, lookRot, 20f * Time.deltaTime);
        }
        if (isFiring) TryToFire();
    }


    private void OnFireStarted(InputAction.CallbackContext _)
    {
        if (fullAuto)
        {
            isFiring = true;
            aim?.ForceCoupleOnFire();
            TryToFire(true);
        }
        else
        {
            TryToFire();
        }
    }

    private void OnFirePerformed(InputAction.CallbackContext _)
    {
        if (!fullAuto)
        {
            isFiring = false;
        }
    }

    private void OnFireCancelled(InputAction.CallbackContext _)
    {
        isFiring = false;
    }

    private void TryToFire(bool force = false)
    {
        if (!aim || !muzzle || !projectilePrefab) return;
        // if (!force && Time.time > nextFireTime) return;
        if (!force && Time.time < nextFireTime) return;


        nextFireTime = Time.time + (1f / Mathf.Max(0.01f, fireRate));


        Vector3 direction = aim.GetAimDirection(muzzle.position, muzzle.forward);

        Vector3 spawnPos = muzzle.position + direction * spawnOffset;
        Quaternion spawnRot = Quaternion.LookRotation(direction, Vector3.up);

        // now stat timeeee 

        float currentDamage = playerEntity.Stats.ProjectileAttack;

        Projectile proj = null;

        if (ObjectPool.instance != null) // if there is object pooling, use that
        {
            GameObject pooled = ObjectPool.instance.GetObject(projectilePrefab.gameObject, muzzle); // spawn at muzzle
            proj = pooled.GetComponent<Projectile>();
            proj.transform.position = spawnPos;
            proj.transform.rotation = spawnRot;
            Debug.Log("Used ObjectPool");
        }
        else // use normal instantiating way
        {
            proj = Instantiate(projectilePrefab, spawnPos, spawnRot);
            Debug.Log("Used Normal");
        }
        // // NOTE TO SELF : USE OBJECT POOLING LATER TO REDUCE INSTANTIATING
        proj?.Init(direction * projectileSpeed, shootMask, currentDamage, 100);
        
        // Debug.Log($"Fired projectile with {currentDamage} damage");
    }
    

}
