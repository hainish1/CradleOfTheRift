using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Copied from Hainish's Projectile Script
public class HomingProjectile : MonoBehaviour, Projectile
{
    [SerializeField] private GameObject bulletImpactFX;
    private TrailRenderer trail;
    [Header("Flight Properties")]
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private float gravity = 0f;

    [Header("Hit Properties")]
    [SerializeField] private float hitForce = 8f;
    [SerializeField] private float knockBackImpulse = 8f;
    [SerializeField] private LayerMask hitMask = ~0; // what can this bullet hit

    [Header("Homing Properties")]
    [SerializeField] private Transform targetLocation;
    [SerializeField] private float rotationForce = 30f;
    [SerializeField] private float homingForce = 15f;
    [SerializeField] private float initialLaunchForce = 15f;
    [SerializeField] private float delayBeforeTracking = 0.5f;
    private float age = 0f;
    private bool following = false;
    private Rigidbody rb;
    private Entity target;
    private Vector3 startPos;
    private float actualDamage;

    //private bool hasHit = false;

    // void Awake()
    // {
    //     trail = GetComponent<TrailRenderer>();
    //     rb = GetComponent<Rigidbody>();
    //     rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    //     rb.interpolation = RigidbodyInterpolation.Interpolate;
    //     StartCoroutine(WaitBeforeTracking());
    // }
    public override void Awake()
    {
        base.Awake();
        StartCoroutine(WaitBeforeTracking());
        // meshRenderer = GetComponent<MeshRenderer>();
    }

    // public void Init(Vector3 velocity, LayerMask mask, float damage, float flyDistance = 100, Entity attacker = null)
    // {
    //     rb.linearVelocity = velocity;
    //     hitMask = mask;
    //     actualDamage = damage; // USE DAMAGE FROM STATS SYSTEM
    //     this.attacker = attacker;
    //     age = 0f;

    //     trail.Clear();
    //     trail.time = 0.25f;
    //     //startPos = transform.position;
    //     //this.flyDistance = flyDistance + 1;

    //     Debug.Log($"Projectile initialized with damage: {actualDamage}");
    // }

    private void Start()
    {
        //rb = GetComponent<Rigidbody>();
        StartCoroutine(WaitBeforeTracking());
    }

    private override void Update()
    {
        // // Code ripped straight from Hainish. Thank you Hainish.
        // age += Time.fixedDeltaTime;
        // if (age >= lifeTime)
        // {
        //     Destroy(gameObject);
        //     return;
        // }

        base.Update();

        if (targetLocation == null) return;
        if (!following) return;     // Start homing after a delay

        Vector3 direction = targetLocation.position - transform.position;
        direction.Normalize();

        Vector3 rotateAmount = Vector3.Cross(transform.forward, direction);
        rb.angularVelocity = rotateAmount * rotationForce;
        rb.linearVelocity = transform.forward * homingForce;
    }

    // private void onTriggerEnter(Collider other)
    // {
    //     if (other.transform == target)
    //     {
    //         Destroy(gameObject);
    //     }
    // }

    private void onCollisionEnter(Collision collision)
    {
        // doesnt work lol kms
        Debug.Log("Collision Detected");
        Destroy(gameObject);
        // if (collision.transform == target)
        // {
        //     Destroy(gameObject);
        // }
    }

    private IEnumerator WaitBeforeTracking()
    {
        //rb.AddForce(transform.forward * force, ForceMode.VelocityChange);
        rb.AddForce(Vector3.up * initialLaunchForce, ForceMode.Impulse);
        yield return new WaitForSeconds(delayBeforeTracking);
        following = true;
    }

    private void OnDrawGizmos()
    {
        if (targetLocation != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, targetLocation.position);
        }
    }

    protected virtual void FadeTrailVisuals()
    {
        // if (Vector3.Distance(startPos, transform.position) > flyDistance - 1.5f)
        // {
        //     trail.time -= 5f * Time.fixedDeltaTime;
        // }
    }
}
