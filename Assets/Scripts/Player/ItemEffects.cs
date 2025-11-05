using System;
using UnityEngine;

public class ItemEffects : MonoBehaviour
{
    [SerializeField] private bool enableHomingProjectiles = false;
    [SerializeField] private float homingEffectCooldown = 3f;
    [SerializeField] private int numberOfProjectiles = 3;
    //[SerializeField] private float spreadAngle = 15f;
    [SerializeField] private float projectileSpawnOffset = 1.0f;

    [Header("Homing Projectile Stats")]
    [SerializeField] private float projectileSpeed = 20f;
    [SerializeField] private float currentDamage = 10f;
    [SerializeField] private LayerMask shootMask = ~0;
    [SerializeField] private HomingProjectile homingProjectilePrefab;
    private Entity playerEntity;
    //Vector3 spawnPosition;

    private float timer;

    void Start()
    {
        playerEntity = GetComponent<Entity>();
        timer = homingEffectCooldown;
        //homingProjectilePrefab = Resources.Load<GameObject>("HomingProjectile");
    }

    void FixedUpdate()
    {
        if (!enableHomingProjectiles) return;
        timer -= Time.fixedDeltaTime;

        if (timer < 0f)
        {
            SpawnProjectiles();
            timer = homingEffectCooldown;
        }
    }

    void SpawnProjectiles()
    {
        if (homingProjectilePrefab == null)
        {
            Debug.LogError("Homing Projectile Prefab is not assigned!");
            return;
        }
        Debug.Log("Spawning homing projectiles");
        Vector3 playerPosition = transform.position + (Vector3.up * projectileSpawnOffset);
        float currentDamage = playerEntity.Stats.ProjectileAttack * 1.5f;

        for (int i = 0; i < numberOfProjectiles; i++)
        {
            // REPLACE THIS WILL OBJECT POOLING
            HomingProjectile homingProjectile = null;
            homingProjectile = Instantiate(homingProjectilePrefab, playerPosition, Quaternion.identity);

            if (homingProjectile != null)
            {
                // Here you would set the target of the homing projectile
                // For example, you might want to find the nearest enemy

                Transform target = playerPosition; // Implement this method based on your game logic
                homingProjectile.Init(projectileSpeed, shootMask, currentDamage, 100, playerEntity, playerEntity); // Example init

                //homingProjectile.targetLocation = target;
            }
        }
    }

    public void ToggleHomingProjectilesEffect()
    {
        enableHomingProjectiles = !enableHomingProjectiles;
        Debug.Log("Homing Projectiles Effect is now " + (enableHomingProjectiles ? "enabled" : "disabled"));
    }
}
