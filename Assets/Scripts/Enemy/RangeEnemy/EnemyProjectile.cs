using UnityEngine;


// almost a copy paste of my projectile script for player
public class EnemyProjectile : MonoBehaviour
{

    [Header("flight")]
    [SerializeField] private float lifeTime = 6f;
    [SerializeField] private float gravity = 0f;

    [Header("hit")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float hitForce = 8f;
    [SerializeField] private float knockBackImpulse = 8f;
    [SerializeField] private LayerMask hitMask = ~0; // what can this bullet hit

    Rigidbody rb;
    private float age;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void Init(Vector3 velocity, LayerMask mask, int newDamage)
    {
        rb.linearVelocity = velocity;
        hitMask = mask;
        age = 0f;


        this.damage = newDamage;
    }


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

    void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & hitMask) == 0)
            return;

        // check if collided with enemy and if yes then damage it
        var pm = collision.collider.GetComponentInParent<PlayerMovement>();


        if (pm != null)
        {
            var contact = collision.GetContact(0);

            Vector3 dir = -contact.normal; // opposite of contact point
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f) dir.Normalize();

            pm.ApplyImpulse(dir * knockBackImpulse);


            // TODO
            // Damage to Player's Health component here
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
        }


        // plkace to add impact effects later

        Destroy(gameObject); // its done its job now
    }


}
