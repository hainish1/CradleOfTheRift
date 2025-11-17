using UnityEngine;


/// <summary>
/// Class - Represents a projectile that creates an area of effect on impact.
/// I copied this from EnemyProjectile because I'm not inheriting anything again.
/// </summary>
public class EnemyAOEProjectile : MonoBehaviour
{

    [Header("flight")]
    [SerializeField] private float lifeTime = 6f;
    [SerializeField] private float gravity = 0f;

    [Header("hit")]
    [SerializeField] private float directDamage = 1;
    [SerializeField] private float hitForce = 8f;
    [SerializeField] private float knockBackImpulse = 8f;
    [SerializeField] private LayerMask hitMask = ~0; // what can this bullet hit

    [Header("AOE Effect")]
    [SerializeField] private float aoeRadius = 5f;
    [SerializeField] private float aoeDamage = 1f;

    public GameObject explosionVFX;

    Rigidbody rb;
    private float age;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    /// <summary>
    /// Initiailize this projectile with things like damage, velocity and what can it hit
    /// </summary>
    /// <param name="velocity"></param>
    /// <param name="mask"></param>
    /// <param name="newDamage"></param>
    public void Init(Vector3 velocity, LayerMask mask, float newDamage)
    {
        rb.linearVelocity = velocity;
        hitMask = mask;
        age = 0f;


        this.directDamage = newDamage;
        this.aoeDamage = newDamage; // for now, both damages are same
    }

    /// <summary>
    /// Check the lifetime of the projectile and add force to help move in a direction
    /// </summary>
    void Update()
    {
        age += Time.deltaTime;
        if (age >= lifeTime)
        {
            Destroy(gameObject);
            return;
        }
        if (gravity != 0f)
        {
            rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
        }
    }   

    /// <summary>
    /// If collided with something, check if its a player. If yes, apply damage and knockback to it. Once that is done, Return to object pool or destroy it
    /// </summary>
    /// <param name="collision"></param>
    void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & hitMask) == 0)
            return;

        // spawn AOE effect on collision
        SpawnAOEEffect();
        CreateExplosionVFX();

        // check if collided with enemy and if yes then damage it
        var pm = collision.collider.GetComponentInParent<PlayerMovement>();


        if (pm != null)
        {
            var contact = collision.GetContact(0);

            Vector3 dir = -contact.normal; // opposite of contact point
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f) dir.Normalize();

            pm.ApplyImpulse(dir * knockBackImpulse);

        }

        // other rigidbodies it might hit
        if (collision.rigidbody != null)
        {
            Vector3 force = rb.linearVelocity.normalized * hitForce;
            collision.rigidbody.AddForceAtPosition(force, collision.GetContact(0).point, ForceMode.Impulse);
        }
        var damageable = collision.collider.GetComponentInParent<IDamageable>();
        if (damageable != null && !damageable.IsDead)
        {
            damageable.TakeDamage(this.directDamage);
        }


        // plkace to add impact effects later

        Destroy(gameObject); // its done its job now
    }

    /// <summary>
    /// Spawn the AOE effect at the current position and deal damage to players within the radius    
    void SpawnAOEEffect()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, aoeRadius, hitMask);
        foreach (var col in hits)
        {
            var dmg = col.GetComponentInParent<IDamageable>();
            if (dmg != null && !dmg.IsDead)
            {
                var pm = col.GetComponentInParent<PlayerMovement>();
                if (pm != null)
                {
                    dmg.TakeDamage(aoeDamage);

                    Debug.Log(aoeDamage + " AOE Damage dealt to " + dmg.ToString() + " by " + this.ToString());
                }
            }
        }
    }

    public void CreateExplosionVFX()
    {
        if (explosionVFX == null) return;
        GameObject newFx = Instantiate(explosionVFX);
        newFx.transform.position = transform.position;
        newFx.transform.rotation = Quaternion.identity;

        Destroy(newFx, 1); // destroy after one second
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aoeRadius);
    }
}
