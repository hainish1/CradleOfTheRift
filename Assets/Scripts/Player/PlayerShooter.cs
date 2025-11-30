using System;
using System.Collections;
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
    private int fireMaxCharges;
    private float fireChargeCooldown;
    private float currFireCharges;
    private bool isRegeneratingFireCharges;

    [Header("Projectiles")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private ExplosiveProjectile explosiveProjectilePrefab;
    [SerializeField] private float projectileSpeed = 50f;
    [SerializeField] private float spawnOffset = 0.1f; 

    private InputSystem_Actions input;
    private InputSystem_Actions.PlayerActions actions;
    private InputAction fireAction;

    private Entity playerEntity; // REF FOR STATS

    private bool isFiring;
    private float nextFireTime;

    // Sounds
    private PlayerAudioController audioController;

    // projectiles should ignore their own kind
    Collider[] selfColliders;

    void Start()
    {
        playerEntity = GetComponent<Entity>();

        var input = new InputAction("Toggle Spawning", binding: "<Keyboard>/b");
        input.performed += _ => ToggleFullAuto();
        input.Enable();

        audioController = GetComponent<PlayerAudioController>();

        fireMaxCharges = playerEntity.Stats.FireCharges;
        fireChargeCooldown = playerEntity.Stats.FireChargeCooldown;
        currFireCharges = fireMaxCharges;
        isRegeneratingFireCharges = false;
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

    public Transform GetMuzzleTransform()
    {
        return muzzle;
    }

    private void TryToFire(bool force = false)
    {
        if (!aim || !muzzle || !projectilePrefab || currFireCharges <= 0) return;
        // if (!force && Time.time > nextFireTime) return;
        if (!force && Time.time < nextFireTime) return;


        nextFireTime = Time.time + (1f / Mathf.Max(0.01f, fireRate));


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
}
