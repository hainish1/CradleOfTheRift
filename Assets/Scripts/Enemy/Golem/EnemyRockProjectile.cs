using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Class - Represents a projectile that arcs towards the ground and creates a delayed area of effect on impact like a grenade.
/// </summary>
public class EnemyRockProjectile : MonoBehaviour
{

    [Header("flight")]
    [SerializeField] private float lifeTime = 6f;
    [SerializeField] private float gravity = 9f;

    [Header("hit")]
    [SerializeField] private float hitForce = 8f;
    private float knockBackImpulse;
    private LayerMask hitMask = ~0; // what can this bullet hit
    private bool hasHit;

    //[Header("Damage")]
    //[SerializeField] private float AOERadius = 8f;
    //private float AOEDamage = 5f;
    //[SerializeField] private float AOEDelay = 1f;
    //[SerializeField] private EnemyDelayedAOE delayedAOE;
    private float directDamage;

    Rigidbody rb;
    private float age;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Initialize this projectile with things like damage, velocity and what can it hit
    /// </summary>
    /// <param name="velocity"> Velocity of the projectile. </param>
    /// <param name="mask"> Collection of what types of objects this projectile can interact with. </param>
    /// <param name="newDamage"> Amount of damage the explosion will do. This projectile will do no direct damage on its own. </param>
    public void Init(Vector3 velocity, LayerMask mask, float damage, float knockback)
    {
        rb.linearVelocity = velocity;
        hitMask = mask;
        directDamage = damage;
        knockBackImpulse = knockback;
        age = 0f;
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
        rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
    }   

    /// <summary>
    /// Upon collision, damamge the player if it hit, apply force to other rigidbodies it might hit, and destroy this projectile
    /// </summary>
    /// <param name="collision"></param>
    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return; // prevent multiple collisions from one throw
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
            damageable.TakeDamage(directDamage);
        }


        // place to add impact effects later

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


    // void OnDrawGizmos()
    // {
    //     Gizmos.color = Color.red;
    //     Gizmos.DrawWireSphere(transform.position, AOERadius);
    // }
}
