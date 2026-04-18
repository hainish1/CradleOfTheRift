using UnityEngine;


/// <summary>
/// Class - Represents a EnemyProjectiles, which is shot by the EnemyRange.
/// almost a copy paste of my projectile script for player
/// </summary>
public class EnemyProjectile : MonoBehaviour
{

    [Header("flight")]
    [SerializeField] private float lifeTime = 6f;
    [SerializeField] private float gravity = 0f;

    [Header("hit")]
    [SerializeField] private float damage = 1; // probably gonna hide this soon
    [SerializeField] private float hitForce = 8f;
    [SerializeField] private float knockBackImpulse = 8f;
    [SerializeField] private LayerMask hitMask = ~0; // what can this bullet hit

    Rigidbody rb;
    private float age;
    private bool hasHit;
    private EnemyRange owner;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void OnEnable()
    {
        age = 0f;
        hasHit = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Initiailize this projectile with things like damage, velocity and what can it hit
    /// </summary>
    /// <param name="velocity"></param>
    /// <param name="mask"></param>
    /// <param name="newDamage"></param>
    public void Init(Vector3 velocity, LayerMask mask, float newDamage)
    {
        Init(velocity, mask, newDamage, null);
    }

    public void Init(Vector3 velocity, LayerMask mask, float newDamage, EnemyRange shooter)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.linearVelocity = velocity;
        hitMask = mask;
        age = 0f;
        hasHit = false;
        this.damage = newDamage;
        this.owner = shooter;
    }

    /// <summary>
    /// Check the lifetime of the projectile and add force to help move in a direction
    /// </summary>
    void Update()
    {
        age += Time.deltaTime;
        if (age >= lifeTime)
        {
            ReturnToPool();
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
        if (hasHit) return;
        if (((1 << collision.gameObject.layer) & hitMask) == 0)
            return;

        hasHit = true;

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
            damageable.TakeDamage(damage);
            if (owner != null) owner.TryApplyElementalOnHit(damageable);
        }


        // plkace to add impact effects later

        ReturnToPool();
    }

    void ReturnToPool()
    {
        if (ObjectPool.instance != null)
        {
            ObjectPool.instance.ReturnObject(gameObject, 0.01f);
        }
        else
        {
            Destroy(gameObject);
        }
    }


}
