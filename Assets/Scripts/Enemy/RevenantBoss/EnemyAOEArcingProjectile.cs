using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Class - Represents a projectile that arcs towards the ground and creates a delayed area of effect on impact like a grenade.
/// Copied again cus im not inheriting
/// </summary>
public class EnemyAOEArcingProjectile : MonoBehaviour
{

    [Header("flight")]
    [SerializeField] private float lifeTime = 6f;
    [SerializeField] private float gravity = 9f;

    [Header("hit")]
    [SerializeField] private float directDamage = 0;
    [SerializeField] private float hitForce = 8f;
    [SerializeField] private float knockBackImpulse = 8f;
    [SerializeField] private LayerMask hitMask = ~0; // what can this bullet hit

    [Header("AOE Effect")]
    [SerializeField] private float AOERadius = 8f;
    [SerializeField] private float AOEDamage = 5f;
    [SerializeField] private float AOEDelay = 1f;
    [SerializeField] private EnemyDelayedAOE delayedAOE;

    //public GameObject explosionVFX;

    Rigidbody rb;
    private float age;
    //private AudioSource audioSource;
    //[SerializeField] private AudioClip explosionSound;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        // rb.interpolation = RigidbodyInterpolation.Interpolate;
        //audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Initiailize this projectile with things like damage, velocity and what can it hit
    /// </summary>
    /// <param name="velocity"></param>
    /// <param name="mask"></param>
    /// <param name="newDamage"></param>
    public void Init(Vector3 velocity, LayerMask mask)
    {
        rb.linearVelocity = velocity;
        hitMask = mask;
        age = 0f;


        //this.directDamage = newDamage;
        //this.aoeDamage = newDamage; // for now, both damages are same
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
    /// If collided with something, check if its a player. If yes, apply damage and knockback to it. Once that is done, Return to object pool or destroy it
    /// </summary>
    /// <param name="collision"></param>
    void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & hitMask) == 0)
            return;

        // spawn AOE effect on collision
        // CreateExplosionVFX();
        SpawnAOEObject(collision.GetContact(0).point);
        //PlayExplosionSound();

        Destroy(gameObject);
    }

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

    // public void CreateExplosionVFX()
    // {
    //     if (explosionVFX == null) return;
    //     GameObject newFx = Instantiate(explosionVFX);
    //     newFx.transform.position = transform.position;
    //     newFx.transform.rotation = Quaternion.identity;
    //     newFx.transform.localScale = Vector3.one * aoeRadius * 0.3f;
    //     Destroy(newFx, 1); // destroy after one second
    // }

    // public void PlayExplosionSound()
    // {
    //     if (audioSource != null && explosionSound != null)
    //     {
    //         audioSource.Stop();
    //         audioSource.PlayOneShot(explosionSound);
    //     }
    // }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AOERadius);
    }
}
