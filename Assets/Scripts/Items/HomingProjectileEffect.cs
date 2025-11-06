using UnityEngine;

public class HomingProjectileEffect : MonoBehaviour
{
    [SerializeField] private bool enableHomingProjectiles = false;
    [SerializeField] private float homingEffectCooldown = 3f;
    [SerializeField] private int numberOfProjectiles = 3;
    //[SerializeField] private float spreadAngle = 15f;
    [SerializeField] private float projectileSpawnOffset = 1.0f;

    [Header("Homing Projectile Stats")]
    private float projectileSpeed = 15f; // This doesn't do anything but its how the base projectile 
    [SerializeField] private LayerMask shootMask = ~0;
    [SerializeField] private HomingProjectile homingProjectilePrefab;
    private Entity playerEntity;
    //Vector3 spawnPosition;

    private float timer;
    //private PlayerShooter playerShooter;

    void Start()
    {
        playerEntity = GetComponent<Entity>();
        timer = homingEffectCooldown;
        //playerShooter = GetComponent<PlayerShooter>();
        //homingProjectilePrefab = Resources.Load<GameObject>("HomingProjectile");
    }

    void FixedUpdate()
    {
        if (playerEntity != null)
        {
            //Debug.Log(HomingProjectile());
            if (HomingProjectile() > 0)
            {
                enableHomingProjectiles = true;
            }
            else
            {
                enableHomingProjectiles = false;
            }
        }

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
        float currentDamage = playerEntity.Stats.ProjectileDamage * 1.5f;

        for (int i = 0; i < numberOfProjectiles; i++)
        {
            playerPosition += Vector3.forward * Random.Range(-2f, 2f) * projectileSpawnOffset;
            HomingProjectile homingProjectile = null;

            // I'll figure this out some other century
            // if (ObjectPool.instance != null)
            // {
            //     GameObject pooled = ObjectPool.instance.GetObject(homingProjectilePrefab.gameObject, transform); // spawn at muzzle
            //     homingProjectile = pooled.GetComponent<HomingProjectile>();
            //     homingProjectile.transform.position = playerPosition;
            //     homingProjectile.transform.rotation = Quaternion.identity;
            //     Debug.Log("Homing Projectile used ObjectPool");
            // }
            // else
            // {
            //     homingProjectile = Instantiate(homingProjectilePrefab, playerPosition, Quaternion.identity);
            // }

            homingProjectile = Instantiate(homingProjectilePrefab, playerPosition, Quaternion.identity);

            if (homingProjectile != null)
            {
                homingProjectile.Init(shootMask, currentDamage, 100, playerEntity); // Example init
            }
        }
    }

    public void ToggleHomingProjectilesEffect()
    {
        enableHomingProjectiles = !enableHomingProjectiles;
        Debug.Log("Homing Projectiles Effect is now " + (enableHomingProjectiles ? "enabled" : "disabled"));
    }

    private int HomingProjectile()
    {
        //numberOfProjectiles = numberOfProjectiles + playerEntity.Stats.HomingProjectiles - 1;
        return playerEntity.Stats.HomingProjectiles;
    }
}
