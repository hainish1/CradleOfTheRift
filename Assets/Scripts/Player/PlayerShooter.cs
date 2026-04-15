// <summary>
//   <authors>
//     Hainish Acharya, Samuel Rigby
//   </authors>
//   <para>
//     Written by Hainish Acharya for GAMES 4500, University of Utah.
//     Contributed to by Samuel Rigby.
//          -Wrote charge regeneration logic.
//          -Separated try-firing logic from firing logic.
//          -Added weapon projectile changing logic.
//          -Added compatability with weapon throw animation.
//          -Added WeaponFlip and WeaponRegain animation coroutines.
//   </para>
// </summary>

using System;
using System.Collections;
using Unity.Cinemachine;
using Unity.Cinemachine.Samples;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerShooter : MonoBehaviour
{
    private InputSystem_Actions input;
    private InputSystem_Actions.PlayerActions actions;
    private InputAction fireAction;
    private Entity playerEntity; // REF FOR STATS
    private PlayerMovement playerMovement;
    private PlayerHeldWeaponController playerHeldWeaponController;
    private PlayerMeleeControllerV2 meleeController;
    private PlayerShockwaveController _shockwaveController;

    [Header("AimReferences")] [Space]
    [SerializeField] private PlayerAimController aim; // Player AIming core
    [SerializeField] private Transform muzzle; // our cube thing
    [SerializeField] private AimTargetManager aimTargetManager;
    [SerializeField] private LayerMask shootMask = ~0;

    [Header("Fire info")] [Space]
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private bool fullAuto = true;
    private int fireMaxCharges;
    private float fireChargeCooldown;
    private float currFireCharges;
    private bool isFiring;
    private float nextFireTime;
    private bool isRegeneratingFireCharges;
    public static event Action<int, int> OnFireChargeSpent;      // (current, max)
    public static event Action<int, int> OnFireChargeRestored;   // (current, max)
    public bool IsThrowing { get; private set; }
    private Coroutine weaponRegainCoroutine;

    [Header("Projectiles")] [Space]
    [SerializeField] private Transform playerCenter;
    [SerializeField] Projectile spearProjectilePrefab;
    [SerializeField] Projectile axeProjectilePrefab;
    [SerializeField] Projectile maceProjectilePrefab;
    [SerializeField] private ExplosiveProjectile explosiveProjectilePrefab;
    [SerializeField] private float projectileSpeed = 50f;
    [SerializeField] private float spawnOffset = 0.1f;
    private Projectile currProjectilePrefab;

    [Header("Weapon Animation")] [Space]
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private AnimationClip preTransitionAnim;
    [SerializeField] private AnimationClip throwAnim;
    [SerializeField] private AnimationClip postTransitionAnim;
    [SerializeField, Range(0, 1)]
    [Tooltip("How quickly the weapon flip animation completes before being thrown. (0.0 is instant, 0.5 is halfway, 1.0 is when the weapon is thrown.)")]
    private float flipAnimCompletionTime;
    [SerializeField] private float regainAnimCompletionSeconds;
    [SerializeField] private float regainDelaySeconds;
    private Animator playerAnim;
    private Quaternion weaponOriginalRotation;
    private Quaternion weaponFlippedRotation;
    private Vector3 weaponOriginalScale;
    private Vector3 weaponShrunkScale;
    private float flipAnimMaxSeconds;

    [Header("Sounds")] [Space]
    [SerializeField] private AK.Wwise.Event fireEvent;
    private PlayerAudioController audioController;

    // projectiles should ignore their own kind
    Collider[] selfColliders;

    void Start()
    {
        var input = new InputAction("Toggle Spawning", binding: "<Keyboard>/b");
        input.performed += _ => ToggleFullAuto();
        input.Enable();

        playerEntity = GetComponentInParent<Entity>();
        playerMovement = GetComponentInParent<PlayerMovement>();
        playerHeldWeaponController = GetComponentInParent<PlayerHeldWeaponController>();
        meleeController = GetComponentInParent<PlayerMeleeControllerV2>();
        _shockwaveController = GetComponentInParent<PlayerShockwaveController>();
        playerAnim = GetComponent<Animator>();
        audioController = GetComponentInParent<PlayerAudioController>();

        SetProjectileType(playerHeldWeaponController.HeldWeapon);
        fireMaxCharges = playerEntity.Stats.FireCharges;
        fireChargeCooldown = playerEntity.Stats.FireChargeCooldown;
        currFireCharges = fireMaxCharges;
        isRegeneratingFireCharges = false;
        IsThrowing = false;
        weaponRegainCoroutine = null;

        weaponOriginalRotation = weaponPivot.localRotation;
        weaponFlippedRotation = Quaternion.Euler(weaponOriginalRotation.eulerAngles + new Vector3(0, 0, 180));
        weaponOriginalScale = weaponPivot.localScale;
        weaponShrunkScale = new Vector3(1, 0.01f, 1);

        SetCurrentAnimationSpeed();
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
        if (PauseManager.GameIsPaused) return;

        if (!aim || !muzzle) return;
        Vector3 direction = aim.GetAimDirection(muzzle.position, muzzle.forward, out RaycastHit _);
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction, Vector3.up);
            muzzle.rotation = Quaternion.Slerp(muzzle.rotation, lookRot, 20f * Time.deltaTime);
        }
        if (isFiring) TryToFire();

        // TESTING : Update fire rate with stats
        if (playerEntity != null)
        {
            fireRate = playerEntity.Stats.ProjectileFireRate;
            
            if (fireMaxCharges != playerEntity.Stats.FireCharges)
            {
                int changeDifference = playerEntity.Stats.FireCharges - fireMaxCharges;
                fireMaxCharges = playerEntity.Stats.FireCharges;

                // Add positive difference to current charge count, even while regenerating.
                if (changeDifference > 0)
                {
                    currFireCharges += changeDifference;
                }
                // Ensure negative difference is not affected by regeneration.
                else
                {
                    if (currFireCharges >= fireMaxCharges)
                    {
                        currFireCharges = fireMaxCharges;
                        isRegeneratingFireCharges = false;
                    }
                }
            }

            fireChargeCooldown = playerEntity.Stats.FireChargeCooldown;
        }
    }

    /// <summary>
    ///   <para>
    ///     Changes the throwable projectile to the given type.
    ///   </para>
    /// </summary>
    /// <param name="weaponType"> The weapon projectile to change to. </param>
    public void SetProjectileType(HeldWeaponType weaponType)
    {
        switch (weaponType)
        {
            case HeldWeaponType.None:
                currProjectilePrefab = null;
                break;
            case HeldWeaponType.Spear:
                currProjectilePrefab = spearProjectilePrefab;
                break;
            case HeldWeaponType.Axe:
                currProjectilePrefab = axeProjectilePrefab;
                break;
            case HeldWeaponType.Mace:
                currProjectilePrefab = maceProjectilePrefab;
                break;
            default:
                break;
        }
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
            TryToWeaponThrow(false);
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

    public Transform GetMuzzleTransform()
    {
        return muzzle;
    }

    private void TryToFire(bool force = false)
    {
        if (!aim || !muzzle || currFireCharges <= 0) return;
        if (!currProjectilePrefab) return;
        // if (!force && Time.time > nextFireTime) return;
        if (!force && Time.time < nextFireTime) return;

        nextFireTime = Time.time + (1f / Mathf.Max(0.01f, fireRate));
        Fire();
        DecrementFireCharges();
    }

    /// <summary>
    ///   <para>
    ///     Makes the player character throw their weapon if all necessary conditions are met.
    ///   </para>
    /// </summary>
    /// <param name="force"> Force the throw regardless of cooldown. </param>
    private void TryToWeaponThrow(bool force = false)
    {
        if (!aim || !muzzle || !currProjectilePrefab || currFireCharges <= 0) return;
        
        // Do not allow throws while dashing, attacking or casting shockwave.
        if (playerMovement.IsDashing || meleeController.IsAttacking || _shockwaveController.IsCastingShockwave) return;
        
        if (!force) // Force throw regardless of cooldown.
            if (Time.time < nextFireTime) return;

        nextFireTime = Time.time + (1f / Mathf.Max(0.01f, fireRate));

        // Trigger weapon throw animation.
        SetCurrentAnimationSpeed();
        playerAnim.SetTrigger("WeaponThrow");
        
        // Stop the current WeaponRegain coroutine if a new throw was performed in the middle of it.
        if (weaponRegainCoroutine != null) StopCoroutine(weaponRegainCoroutine);
        
        DecrementFireCharges();
        IsThrowing = true;
    }

    private void Fire()
    {
        Vector3 direction = aim.GetAimDirection(muzzle.position, muzzle.forward, out RaycastHit raycastHit);

        Vector3 spawnPos = muzzle.position + direction * spawnOffset;
        Quaternion spawnRot = Quaternion.LookRotation(direction, Vector3.up);

        // now stat timeeee 

        float currentDamage = playerEntity.Stats.ProjectileDamage;

        GameObject proj = Instantiate(currProjectilePrefab.gameObject, spawnPos, spawnRot);
        var projScript = proj.GetComponent<Projectile>();

        float speed = projectileSpeed;
        Vector3 velocity;
        if (GloomUpgrade.IsEnabled && projScript is GloomProjectile)
        {
            proj.transform.position = playerEntity.transform.position + Vector3.up * 5f;
            velocity = Vector3.down * 2f;
        }
        else if (PoisonPoolProjectiles.IsEnabled && projScript is PoisonPoolBottleProjectile)
        {
            Vector3 dirXZ = direction;
            dirXZ.y = 0f;
            if (dirXZ.sqrMagnitude < 0.01f) dirXZ = Vector3.forward;
            else dirXZ.Normalize();
            velocity = dirXZ * 20f + Vector3.up * 15f;
        }
        else
        {
            velocity = direction * speed;
        }

        // Initialize the projectile according to its weapon type.
        if (currProjectilePrefab == spearProjectilePrefab)
            projScript?.Init(velocity, shootMask, currentDamage, flyDistance: 100, playerEntity);
        else if (currProjectilePrefab == maceProjectilePrefab)
        {
            MaceProjectile maceProjScript = proj.GetComponent<MaceProjectile>();
            maceProjScript.Init(velocity, shootMask, currentDamage, flyDistance: 100, playerEntity);
        }
        else
        {
            AxeProjectile axeProjScript = proj.GetComponent<AxeProjectile>();

            // Initialize target position.
            Vector3 targetPos = raycastHit.collider ? raycastHit.point : aim.GetAimIntersectPoint(axeProjScript.MaxTravelDistance);
            axeProjScript.Init(targetPos, playerCenter, shootMask, currentDamage, flyDistance: 100, playerEntity);
        }

        // Debug.Log($"Fired projectile with {currentDamage} damage");
        // Play firing sound
        audioController?.PlayAttackSound();
        fireEvent.Post(gameObject);
    }

    /// <summary>
    ///   <para>
    ///     Decreases the current amount of fire charges by 1.
    ///   </para>
    /// </summary>
    private void DecrementFireCharges()
    {
        currFireCharges--;
        OnFireChargeSpent?.Invoke((int)currFireCharges, fireMaxCharges);

        // Only initialize regeneration routine if not already regenerating.
        if (currFireCharges == fireMaxCharges - 1) StartCoroutine(FireChargeRegeneration());
    }

    private IEnumerator FireChargeRegeneration()
    {
        isRegeneratingFireCharges = true;

        float timer = 0;

        while (currFireCharges < fireMaxCharges && isRegeneratingFireCharges)
        {
            timer += Time.deltaTime;

            if (timer >= fireChargeCooldown)
            {
                timer = 0;
                currFireCharges++;
                OnFireChargeRestored?.Invoke((int)currFireCharges, fireMaxCharges);
            }

            if (currFireCharges >= fireMaxCharges) break;

            yield return null;
        }

        currFireCharges = Mathf.Min(currFireCharges, fireMaxCharges);  // In case fireMaxCharges is decreased during routine execution.

        isRegeneratingFireCharges = false;
    }

    /// <summary>
    ///   <para>
    ///     Resets the weapon throw animation variables using the most up-to-date stats value on any frame this method is called.
    ///   </para>
    /// </summary>
    private void SetCurrentAnimationSpeed()
    {
        float scriptFlipAnimSpeed = flipAnimCompletionTime;
        float statsThrowAnimSpeed = 1 / playerEntity.Stats.ProjectileAnimationSpeed;
        float secondsUntilThrow = preTransitionAnim.length + throwAnim.events[0].time;
        flipAnimMaxSeconds = scriptFlipAnimSpeed * statsThrowAnimSpeed * secondsUntilThrow;
        playerAnim.SetFloat("WeaponThrowAnimSpeedMultiplier", playerEntity.Stats.ProjectileAnimationSpeed);
    }

    /// <summary>
    ///   <para>
    ///     Animation event to begin the WeaponFlip animation coroutine.
    ///   </para>
    /// </summary>
    public void OnWeaponThrowAnimBegin()
    {
        // Do not flip the axe.
        if (currProjectilePrefab != axeProjectilePrefab) StartCoroutine(WeaponFlip());
    }

    /// <summary>
    ///   <para>
    ///     Makes the weapon flip 180 degrees around its Z-axis in the designated amount of time.
    ///   </para>
    /// </summary>
    /// <returns> IEnumerator object. </returns>
    private IEnumerator WeaponFlip()
    {
        // Ensure weapon is visible and oriented correctly in the case of multiple quick consecutive throws.
        weaponPivot.localRotation = weaponOriginalRotation;
        weaponPivot.localScale = weaponOriginalScale;
        
        float timer = 0;
        while (timer < flipAnimMaxSeconds)
        {
            float completion = timer / flipAnimMaxSeconds;
            weaponPivot.localRotation = Quaternion.Lerp(weaponOriginalRotation, weaponFlippedRotation, completion);

            timer += Time.deltaTime;
            yield return null;
        }

        // Ensure weapon rotation is exact when done rotating.
        weaponPivot.localRotation = weaponFlippedRotation;
    }

    /// <summary>
    ///   <para>
    ///     Animation event to shoot a projectile and make the weapon disappear.
    ///   </para>
    /// </summary>
    public void OnWeaponThrow()
    {
        // Disappear the weapon by shrinking it to 0.
        weaponPivot.localScale = new Vector3(0, 0, 0);
        Fire();
    }

    /// <summary>
    ///   <para>
    ///     Animation event to begin the WeaponRegain animation coroutine.
    ///   </para>
    /// </summary>
    public void OnWeaponThrowAnimEnd()
    {
        weaponRegainCoroutine = StartCoroutine(WeaponRegain());
    }

    /// <summary>
    ///   <para>
    ///     Makes the weapon grow back to its original scale in the designated amount of time.
    ///   </para>
    /// </summary>
    /// <returns> IEnumerator object. </returns>
    private IEnumerator WeaponRegain()
    {
        yield return new WaitForSeconds(regainDelaySeconds);
        
        // Set weapon to original orientation and shrunk scale since the flip animation is complete.
        weaponPivot.localRotation = weaponOriginalRotation;
        weaponPivot.localScale = weaponShrunkScale;

        float timer = 0;
        while (timer < regainAnimCompletionSeconds)
        {  
            float completion = timer / regainAnimCompletionSeconds;
            weaponPivot.localScale = Vector3.Lerp(weaponShrunkScale, weaponOriginalScale, completion);

            timer += Time.deltaTime;
            yield return null;
        }

        // Ensure weapon scale restoration is exact when done growing.
        weaponPivot.localScale = weaponOriginalScale;
        IsThrowing = false;
    }
}
