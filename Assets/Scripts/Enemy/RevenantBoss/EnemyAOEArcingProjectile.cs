using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Class - Represents a projectile that arcs towards the ground and creates a delayed area of effect on impact like a grenade.
/// </summary>
public class EnemyAOEArcingProjectile : MonoBehaviour
{

    [Header("flight")]
    [SerializeField] private float lifeTime = 6f;
    [SerializeField] private float gravity = 9f;

    [Header("hit")]
    private float directDamage = 0;
    [SerializeField] private float hitForce = 8f;
    [SerializeField] private float knockBackImpulse = 8f;
    [SerializeField] private LayerMask hitMask = ~0; // what can this bullet hit

    [Header("AOE Effect")]
    [SerializeField] private float AOERadius = 8f;
    private float AOEDamage = 5f;
    [SerializeField] private float AOEDelay = 1f;
    [SerializeField] private EnemyDelayedAOE delayedAOE;

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
    public void Init(Vector3 velocity, LayerMask mask, float newDamage)
    {
        rb.linearVelocity = velocity;
        hitMask = mask;
        AOEDamage = newDamage;
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
    /// Upon collision, spawn the delayed AOE effect at the contact point and destroy this projectile
    /// </summary>
    /// <param name="collision"></param>
    void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & hitMask) == 0)
            return;

        // spawn AOE effect on collision
        SpawnAOEObject(collision.GetContact(0).point);
        Destroy(gameObject);
    }

    /// <summary>
    /// Spawn the delayed AOE effect at the given position
    /// </summary>
    /// <param name="position"> Spawn AOE at this position, usually at point of collision. </param>
    private void SpawnAOEObject(Vector3 position)
    {
        if (delayedAOE != null)
        {
            EnemyDelayedAOE explosion = Instantiate(delayedAOE, position, Quaternion.identity);
            explosion.Init(AOERadius, AOEDamage, AOEDelay);
        }
        else
        {
            Debug.LogError("No delayed AOE prefab assigned in editor!");
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AOERadius);
    }
}
