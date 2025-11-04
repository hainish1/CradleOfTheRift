using System.Reflection.Emit;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private GameObject bulletImpactFX;
    public TrailRenderer trail;
    [Header("flight")]
    [SerializeField] private float lifeTime = 6f;
    [SerializeField] private float gravity = 0f;

    [Header("hit")]
    [SerializeField] private float hitForce = 8f;
    [SerializeField] private float knockBackImpulse = 8f;
    [SerializeField] protected LayerMask hitMask = ~0; // what can this bullet hit

    protected float actualDamage; // THIS WILL STORE DAMAGE FROM STATS SYSTEM

    public Rigidbody rb;
    protected float age;
    protected Vector3 startPos;
    protected float flyDistance;
    protected Entity attacker;
    protected bool hasHit;

    public virtual void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // meshRenderer = GetComponent<MeshRenderer>();
    }

    public virtual void Init(Vector3 velocity, LayerMask mask, float damage, float flyDistance = 100, Entity attacker = null)
    {
        rb.linearVelocity = velocity;
        hitMask = mask;
        actualDamage = damage; // USE DAMAGE FROM STATS SYSTEM
        this.attacker = attacker;
        age = 0f;

        trail.Clear();
        trail.time = 0.25f;
        startPos = transform.position;
        this.flyDistance = flyDistance + 1;

        Debug.Log($"Projectile initialized with damage: {actualDamage}");

        Debug.Log("This belongs to the parent");
    }

    public virtual void InitializeTrailVisuals()
    {
        trail.Clear();
        trail.time = 0.25f;
        startPos = transform.position;
        this.flyDistance = flyDistance + 1;
        Debug.Log("Set trail visuals");
    }

    public virtual void Update()
    {
        FadeTrailVisuals();
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
    void OnEnable()
    {
        hasHit = false; // so it does not double do it
    }

    protected virtual void FadeTrailVisuals()
    {
        if (Vector3.Distance(startPos, transform.position) > flyDistance - 1.5f)
        {
            trail.time -= 5f * Time.deltaTime;
        }

        Debug.Log("Fading trail visuals");
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        // alright documenting time
        // check layer mask
        if (((1 << collision.gameObject.layer) & hitMask) == 0)
            return;

        CreateImpactFX();

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

                // report the combat event for damage
                CombatEvents.ReportDamage(attacker, enemy, actualDamage);
                hasHit = true;

                // checking teh event here
                
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
        // Destroy(gameObject); // its done its job now
        ReturnToSource(); // use object pooling
    }

    protected void CreateImpactFX()
    {
        GameObject newFX = Instantiate(bulletImpactFX);
        newFX.transform.position = transform.position;

        Destroy(newFX, 1);

        // GameObject newImpacFX = ObjectPool.instance.GetObject(bulletImpactFX, transform);
        // ObjectPool.instance.ReturnObject(newImpacFX, 1f); // return the effect back to the pool after 1 second of delay

    }
    
    public virtual void ReturnToSource()
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
