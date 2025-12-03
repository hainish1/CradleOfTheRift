using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Class - Represents a stationary explosion that damages anything in its area after a delay.
/// </summary>
public class EnemyDelayedAOE : MonoBehaviour
{
    [Header("hit")]
    private float hitForce = 8f;
    private float knockBackImpulse = 8f;
    [SerializeField] private LayerMask hitMask = ~0; // what can this bullet hit

    [Header("AOE Effect")]
    private float radius = 8f;
    private float damage = 5f;
    private float delay = 1f;
    [SerializeField] private GameObject explosionVFX;

    //private float age;
    //private AudioSource audioSource;
    //private AudioClip explosionSound;

    // void Awake()
    // {
    //     audioSource = GetComponent<AudioSource>();
    // }

    /// <summary>
    /// Initialize the AOE explosion with what it can hit, its damage, and its radius
    /// </summary>
    /// <param name="mask"></param>
    /// <param name="aoeDamage"></param>
    /// <param name="aoeRadius"></param>
    public void Init(float newRadius, float newDamage, float newDelay)
    {
        //this.hitMask = mask;
        //this.explosionVFX = explosionVFX;
        radius = newRadius;
        damage = newDamage;
        delay = newDelay;

        StartCoroutine(SpawnAOEEffect());
    }

    /// <summary>
    /// Spawn the AOE effect at the current position and deal damage to players within the radius    
    public IEnumerator SpawnAOEEffect()
    {
        CreateExplosionVFX();

        yield return new WaitForSeconds(delay);

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, hitMask);
        HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
        foreach (var col in hits)
        {
            var dmg = col.GetComponentInParent<IDamageable>();
            if (dmg != null && !dmg.IsDead && !damagedTargets.Contains(dmg))
            {
                var pm = col.GetComponentInParent<PlayerMovement>();
                if (pm != null)
                {
                    dmg.TakeDamage(damage);

                    damagedTargets.Add(dmg);
                    Debug.Log(damage + " Delayed AOE Damage dealt to " + dmg.ToString() + " by " + this.ToString());
                }
            }
        }

        Destroy(gameObject);
    }

    public void CreateExplosionVFX()
    {
        if (explosionVFX == null)
        {
            Debug.LogError("No explosion VFX has been assigned!");
            return;
        }
        GameObject newFx = Instantiate(explosionVFX);
        newFx.transform.position = transform.position;
        newFx.transform.rotation = Quaternion.identity;
        newFx.transform.localScale = Vector3.one * radius * 0.25f;
        Destroy(newFx, 2f); // destroy after two second
    }

    // /// <summary>
    // /// Check the lifetime of the projectile and add force to help move in a direction
    // /// </summary>
    // void Update()
    // {
    //     age += Time.deltaTime;
    //     if (age >= lifeTime)
    //     {
    //         Destroy(gameObject);
    //         return;
    //     }
    //     if (gravity != 0f)
    //     {
    //         rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
    //     }
    // }   

    // /// <summary>
    // /// If collided with something, check if its a player. If yes, apply damage and knockback to it. Once that is done, Return to object pool or destroy it
    // /// </summary>
    // /// <param name="collision"></param>
    // void OnCollisionEnter(Collision collision)
    // {
    //     if (((1 << collision.gameObject.layer) & hitMask) == 0)
    //         return;

    //     // spawn AOE effect on collision
    //     StartCoroutine(SpawnAOEEffect());
    //     CreateExplosionVFX();
    //     PlayExplosionSound();

    //     // check if collided with enemy and if yes then damage it
    //     var pm = collision.collider.GetComponentInParent<PlayerMovement>();


    //     if (pm != null)
    //     {
    //         var contact = collision.GetContact(0);

    //         Vector3 dir = -contact.normal; // opposite of contact point
    //         dir.y = 0f;
    //         if (dir.sqrMagnitude > 0.0001f) dir.Normalize();

    //         pm.ApplyImpulse(dir * knockBackImpulse);

    //     }

    //     // other rigidbodies it might hit
    //     if (collision.rigidbody != null)
    //     {
    //         Vector3 force = rb.linearVelocity.normalized * hitForce;
    //         collision.rigidbody.AddForceAtPosition(force, collision.GetContact(0).point, ForceMode.Impulse);
    //     }
    //     var damageable = collision.collider.GetComponentInParent<IDamageable>();
    //     if (damageable != null && !damageable.IsDead)
    //     {
    //         damageable.TakeDamage(this.directDamage);
    //     }


    //     // plkace to add impact effects later

    //     Destroy(gameObject); // its done its job now
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
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
