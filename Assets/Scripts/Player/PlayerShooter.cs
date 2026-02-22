// <summary>
//   <authors>
//     Hainish Acharya, Samuel Rigby
//   </authors>
//   <para>
//     Written by Hainish Acharya for GAMES 4500, University of Utah.
//     Contributed to by Samuel Rigby.
//          -Added compatability with spear throw animation.
//          -Added SpearFlip and SpearRegain animation coroutines.
//          -Separated try-firing logic from firing logic.
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
    private PlayerMeleeControllerV2 meleeController;

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
    public bool IsThrowing { get; private set; }
    private float nextFireTime;
    private bool isRegeneratingFireCharges;

    [Header("Projectiles")] [Space]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private ExplosiveProjectile explosiveProjectilePrefab;
    [SerializeField] private float projectileSpeed = 50f;
    [SerializeField] private float spawnOffset = 0.1f;

    [Header("Spear Animation")] [Space]
    [SerializeField] private Transform weaponHandMount;
    [SerializeField] private AnimationClip preTransitionAnim;
    [SerializeField] private AnimationClip throwAnim;
    [SerializeField] private AnimationClip postTransitionAnim;
    [SerializeField, Range(0, 1)]
    [Tooltip("How quickly the spear flip animation completes before being thrown (0.0 is instant, 0.5 is halfway, 1.0 is when the spear is thrown).")]
    private float flipAnimCompletionTime;
    [SerializeField] private float regainAnimCompletionSeconds;
    [SerializeField] private float regainDelaySeconds;
    private Animator shooterAnim;
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
        meleeController = GetComponentInParent<PlayerMeleeControllerV2>();
        shooterAnim = GetComponent<Animator>();
        audioController = GetComponentInParent<PlayerAudioController>();

        fireMaxCharges = playerEntity.Stats.FireCharges;
        fireChargeCooldown = playerEntity.Stats.FireChargeCooldown;
        currFireCharges = fireMaxCharges;
        isRegeneratingFireCharges = false;

        weaponOriginalRotation = weaponHandMount.localRotation;
        weaponFlippedRotation = Quaternion.Euler(weaponOriginalRotation.eulerAngles + new Vector3(0, 0, 180));
        weaponOriginalScale = weaponHandMount.localScale;
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
        Vector3 direction = aim.GetAimDirection(muzzle.position, muzzle.forward);
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
            TryToSpearThrow(false);
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
        if (!projectilePrefab) return;
        // if (!force && Time.time > nextFireTime) return;
        if (!force && Time.time < nextFireTime) return;

        nextFireTime = Time.time + (1f / Mathf.Max(0.01f, fireRate));
        Fire();
    }

    /// <summary>
    ///   <para>
    ///     Makes the player character throw their spear if all necessary conditions are met.
    ///   </para>
    /// </summary>
    /// <param name="force"> Force the throw regardless of cooldown. </param>
    private void TryToSpearThrow(bool force = false)
    {
        if (!aim || !muzzle || !projectilePrefab || currFireCharges <= 0) return;

        // Do not allows throws while dashing or attacking.
        if (playerMovement.IsDashing || meleeController.IsAttacking) return;

        if (!force) // Force throw regardless of cooldown.
            if (Time.time < nextFireTime) return;

        nextFireTime = Time.time + (1f / Mathf.Max(0.01f, fireRate));

        // Trigger spear throw animation.
        SetCurrentAnimationSpeed();
        shooterAnim.SetTrigger("SpearThrow");
        IsThrowing = true;
    }

    private void Fire()
    {
        Vector3 direction = aim.GetAimDirection(muzzle.position, muzzle.forward);

        Vector3 spawnPos = muzzle.position + direction * spawnOffset;
        Quaternion spawnRot = Quaternion.LookRotation(direction, Vector3.up);

        // now stat timeeee 

        float currentDamage = playerEntity.Stats.ProjectileDamage;

        Projectile proj = null;

        if (ObjectPool.instance != null)
        {
            GameObject pooled = ObjectPool.instance.GetObject(projectilePrefab.gameObject, muzzle);

            if (BounceProjectiles.IsEnabled)
            {
                proj = pooled.GetComponent<BounceProjectile>();
                if (proj == null)
                {
                    var oldProj = pooled.GetComponent<Projectile>();
                    if (oldProj != null)
                    {
                        var bulletFX = oldProj.BulletImpactFX;
                        var trail = oldProj.trail;
                        Destroy(oldProj);
                        proj = pooled.AddComponent<BounceProjectile>();
                        proj.BulletImpactFX = bulletFX;
                        proj.trail = trail;
                    }
                    else
                    {
                        var oldExpProj = pooled.GetComponent<ExplosiveProjectile>();
                        if (oldExpProj != null)
                        {
                            var bulletFX = oldExpProj.BulletImpactFX;
                            var trail = oldExpProj.trail;
                            Destroy(oldExpProj);
                            proj = pooled.AddComponent<BounceProjectile>();
                            proj.BulletImpactFX = bulletFX;
                            proj.trail = trail;
                        }
                        else
                        {
                            proj = pooled.AddComponent<BounceProjectile>();
                        }
                    }
                }
            }
            else if (ExplosiveProjectiles.IsEnabled)
            {
                proj = pooled.GetComponent<ExplosiveProjectile>();
                if (proj == null)
                {
                    var oldProj = pooled.GetComponent<Projectile>();
                    if (oldProj != null)
                    {
                        var bulletFX = oldProj.BulletImpactFX;
                        var trail = oldProj.trail;
                        Destroy(oldProj);
                        proj = pooled.AddComponent<ExplosiveProjectile>();
                        proj.BulletImpactFX = bulletFX;
                        proj.trail = trail;
                    }
                    else
                    {
                        var oldBounceProj = pooled.GetComponent<BounceProjectile>();
                        if (oldBounceProj != null)
                        {
                            var bulletFX = oldBounceProj.BulletImpactFX;
                            var trail = oldBounceProj.trail;
                            Destroy(oldBounceProj);
                            proj = pooled.AddComponent<ExplosiveProjectile>();
                            proj.BulletImpactFX = bulletFX;
                            proj.trail = trail;
                        }
                        else
                        {
                            proj = pooled.AddComponent<ExplosiveProjectile>();
                        }
                    }
                }
            }
            else
            {
                proj = pooled.GetComponent<Projectile>();
                if (proj == null)
                {
                    var oldExpProj = pooled.GetComponent<ExplosiveProjectile>();
                    if (oldExpProj != null)
                    {
                        var bulletFX = oldExpProj.BulletImpactFX;
                        var trail = oldExpProj.trail;
                        Destroy(oldExpProj);
                        proj = pooled.AddComponent<Projectile>();
                        proj.BulletImpactFX = bulletFX;
                        proj.trail = trail;
                    }
                    else
                    {
                        var oldBounceProj = pooled.GetComponent<BounceProjectile>();
                        if (oldBounceProj != null)
                        {
                            var bulletFX = oldBounceProj.BulletImpactFX;
                            var trail = oldBounceProj.trail;
                            Destroy(oldBounceProj);
                            proj = pooled.AddComponent<Projectile>();
                            proj.BulletImpactFX = bulletFX;
                            proj.trail = trail;
                        }
                        else
                        {
                            proj = pooled.AddComponent<Projectile>();
                        }
                    }
                }
            }

            proj.transform.position = spawnPos;
            proj.transform.rotation = spawnRot;
        }
        else
        {
            GameObject go = Instantiate(projectilePrefab.gameObject, spawnPos, spawnRot);

            if (BounceProjectiles.IsEnabled)
            {
                var oldProj = go.GetComponent<Projectile>();
                if (oldProj != null)
                {
                    var bulletFX = oldProj.BulletImpactFX;
                    var trail = oldProj.trail;
                    Destroy(oldProj);
                    proj = go.AddComponent<BounceProjectile>();
                    proj.BulletImpactFX = bulletFX;
                    proj.trail = trail;
                }
                else
                {
                    var oldExpProj = go.GetComponent<ExplosiveProjectile>();
                    if (oldExpProj != null)
                    {
                        var bulletFX = oldExpProj.BulletImpactFX;
                        var trail = oldExpProj.trail;
                        Destroy(oldExpProj);
                        proj = go.AddComponent<BounceProjectile>();
                        proj.BulletImpactFX = bulletFX;
                        proj.trail = trail;
                    }
                    else
                    {
                        proj = go.GetComponent<BounceProjectile>();
                        if (proj == null) proj = go.AddComponent<BounceProjectile>();
                    }
                }
            }
            else if (ExplosiveProjectiles.IsEnabled)
            {
                var oldProj = go.GetComponent<Projectile>();
                if (oldProj != null)
                {
                    var bulletFX = oldProj.BulletImpactFX;
                    var trail = oldProj.trail;
                    Destroy(oldProj);
                    proj = go.AddComponent<ExplosiveProjectile>();
                    proj.BulletImpactFX = bulletFX;
                    proj.trail = trail;
                }
                else
                {
                    var oldBounceProj = go.GetComponent<BounceProjectile>();
                    if (oldBounceProj != null)
                    {
                        var bulletFX = oldBounceProj.BulletImpactFX;
                        var trail = oldBounceProj.trail;
                        Destroy(oldBounceProj);
                        proj = go.AddComponent<ExplosiveProjectile>();
                        proj.BulletImpactFX = bulletFX;
                        proj.trail = trail;
                    }
                    else
                    {
                        proj = go.GetComponent<ExplosiveProjectile>();
                        if (proj == null) proj = go.AddComponent<ExplosiveProjectile>();
                    }
                }
            }
            else
            {
                var oldExpProj = go.GetComponent<ExplosiveProjectile>();
                if (oldExpProj != null)
                {
                    var bulletFX = oldExpProj.BulletImpactFX;
                    var trail = oldExpProj.trail;
                    Destroy(oldExpProj);
                    proj = go.AddComponent<Projectile>();
                    proj.BulletImpactFX = bulletFX;
                    proj.trail = trail;
                }
                else
                {
                    var oldBounceProj = go.GetComponent<BounceProjectile>();
                    if (oldBounceProj != null)
                    {
                        var bulletFX = oldBounceProj.BulletImpactFX;
                        var trail = oldBounceProj.trail;
                        Destroy(oldBounceProj);
                        proj = go.AddComponent<Projectile>();
                        proj.BulletImpactFX = bulletFX;
                        proj.trail = trail;
                    }
                    else
                    {
                        proj = go.GetComponent<Projectile>();
                        if (proj == null) proj = go.AddComponent<Projectile>();
                    }
                }
            }
        }

        float speed = projectileSpeed;
        if (ExplosiveProjectiles.IsEnabled && proj is ExplosiveProjectile)
        {
            speed *= 0.2f;
        }
        // 弹射投射物保持正常速度
        proj?.Init(direction * speed, shootMask, currentDamage, 100, playerEntity);

        // Debug.Log($"Fired projectile with {currentDamage} damage");
        // Play firing sound
        audioController?.PlayAttackSound();
        fireEvent.Post(gameObject);

        currFireCharges--;

        // Only initialize regeneration routine if not already regenerating.
        if (currFireCharges == fireMaxCharges - 1)
        {
            StartCoroutine(FireChargeRegeneration());
        }
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
            }

            if (currFireCharges >= fireMaxCharges) break;

            yield return null;
        }

        currFireCharges = Mathf.Min(currFireCharges, fireMaxCharges);  // In case fireMaxCharges is decreased during routine execution.

        isRegeneratingFireCharges = false;
    }

    /// <summary>
    ///   <para>
    ///     Resets the spear throw animation variables using the most up-to-date stats value on any frame this method is called.
    ///   </para>
    /// </summary>
    private void SetCurrentAnimationSpeed()
    {
        float scriptFlipAnimSpeed = flipAnimCompletionTime;
        float statsThrowAnimSpeed = 1 / playerEntity.Stats.ProjectileAnimationSpeed;
        float secondsUntilThrow = preTransitionAnim.length + throwAnim.events[0].time;
        flipAnimMaxSeconds = scriptFlipAnimSpeed * statsThrowAnimSpeed * secondsUntilThrow;
        shooterAnim.SetFloat("SpearThrowAnimSpeedMultiplier", playerEntity.Stats.ProjectileAnimationSpeed);
    }

    /// <summary>
    ///   <para>
    ///     Animation event to begin the SpearFlip animation coroutine.
    ///   </para>
    /// </summary>
    public void OnSpearThrowAnimBegin()
    {
        StartCoroutine(SpearFlip());
    }

    /// <summary>
    ///   <para>
    ///     Makes the spear flip 180 degrees around its Z-axis in the designated amount of time.
    ///   </para>
    /// </summary>
    /// <returns> IEnumerator object. </returns>
    private IEnumerator SpearFlip()
    {
        float timer = 0;
        while (timer < flipAnimMaxSeconds)
        {
            float completion = timer / flipAnimMaxSeconds;
            weaponHandMount.localRotation = Quaternion.Lerp(weaponOriginalRotation, weaponFlippedRotation, completion);

            timer += Time.deltaTime;
            yield return null;
        }

        // Ensure weapon rotation is exact when done rotating.
        weaponHandMount.localRotation = weaponFlippedRotation;
    }

    /// <summary>
    ///   <para>
    ///     Animation event to shoot a projectile and make the spear disappear.
    ///   </para>
    /// </summary>
    public void OnSpearThrow()
    {
        // Disappear the weapon by shrinking it to 0.
        weaponHandMount.localScale = new Vector3(0, 0, 0);
        Fire();
    }

    /// <summary>
    ///   <para>
    ///     Animation event to begin the SpearRegain animation coroutine.
    ///   </para>
    /// </summary>
    public void OnSpearThrowAnimEnd()
    {
        StartCoroutine(SpearRegain());
    }

    /// <summary>
    ///   <para>
    ///     Makes the spear grow back to its original scale in the designated amount of time.
    ///   </para>
    /// </summary>
    /// <returns> IEnumerator object. </returns>
    private IEnumerator SpearRegain()
    {
        yield return new WaitForSeconds(regainDelaySeconds);
        
        // Set weapon to original orientation and shrunk scale since the flip animation is complete.
        weaponHandMount.localRotation = weaponOriginalRotation;
        weaponHandMount.localScale = weaponShrunkScale;

        float timer = 0;
        while (timer < regainAnimCompletionSeconds)
        {
            float completion = timer / regainAnimCompletionSeconds;
            weaponHandMount.localScale = Vector3.Lerp(weaponShrunkScale, weaponOriginalScale, completion);

            timer += Time.deltaTime;
            yield return null;
        }

        // Ensure weapon scale restoration is exact when done growing.
        weaponHandMount.localScale = weaponOriginalScale;
        IsThrowing = false;
    }
}
