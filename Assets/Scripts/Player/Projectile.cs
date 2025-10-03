using System.Reflection.Emit;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("flight")]
    [SerializeField] private float lifeTime = 6f;
    [SerializeField] private float gravity = 0f;

    [Header("hit")]
    [SerializeField] private int baseDamage = 1;
    [SerializeField] private float hitForce = 8f;
    [SerializeField] private float knockBackImpulse = 8f;
    [SerializeField] private LayerMask hitMask = ~0; // what can this bullet hit

    private int actualDamage; // THIS WILL STORE DAMAGE FROM STATS SYSTEM

    Rigidbody rb;
    private float age;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void Init(Vector3 velocity, LayerMask mask, int damage)
    {
        rb.linearVelocity = velocity;
        hitMask = mask;
        actualDamage = damage; // USE DAMAGE FROM STATS SYSTEM
        age = 0f;

        Debug.Log($"Projectile initialized with damage: {actualDamage}");
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
        // alright documenting time
        // check layer mask
        if (((1 << collision.gameObject.layer) & hitMask) == 0)
            return;

        // check if collided with enemy and if yes then damage it
        var enemy = collision.collider.GetComponentInParent<Enemy>();

        if (enemy != null)
        {
            var kb = enemy?.GetComponent<AgentKnockBack>();
            if (kb != null)
            {
                var contact = collision.GetContact(0);

                Vector3 dir = -contact.normal; // opposite of contact point
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f) dir.Normalize();

                kb.ApplyImpulse(dir * knockBackImpulse);
            }
            var flash = collision.collider.GetComponentInParent<TargetFlash>();
            if (flash != null) flash.Flash();

            var damageable = collision.collider.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                // HERE NOW I WILL USE THE MODIFIED DAMAGE
                damageable.TakeDamage(actualDamage);
                Debug.Log($"Dealt {actualDamage} damage to {collision.gameObject.name}");
            }
        }
        // apply physics force
        if (collision.rigidbody != null)
        {
            Vector3 force = rb.linearVelocity.normalized * hitForce;
            collision.rigidbody.AddForceAtPosition(force, collision.contacts[0].point, ForceMode.Impulse);
        }
        
        // plkace to add impact effects later
        Destroy(gameObject); // its done its job now
    }


}
