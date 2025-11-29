using System.Reflection.Emit;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] protected GameObject bulletImpactFX;
    public TrailRenderer trail;
    
    public GameObject BulletImpactFX
    {
        get { return bulletImpactFX; }
        set { bulletImpactFX = value; }
    }
    [Header("flight")]
    [SerializeField] protected float lifeTime = 6f;
    [SerializeField] protected float gravity = 0f;

    [Header("hit")]
    [SerializeField] protected float hitForce = 8f;
    [SerializeField] protected float knockBackImpulse = 8f;
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

        // Debug.Log($"Projectile initialized with damage: {actualDamage}");

        //Debug.Log("This belongs to the parent");
    }

    public virtual void InitializeTrailVisuals()
    {
        trail.Clear();
        trail.time = 0.25f;
        startPos = transform.position;
        this.flyDistance = flyDistance + 1;
        //Debug.Log("Set trail visuals");
    }

    public virtual void Update()
    {
        FadeTrailVisuals();
        age += Time.deltaTime;
        if (age >= lifeTime)
        {
            // Destroy(gameObject);
            ReturnToSource();
            return;
        }
        if (gravity != 0f)
        {
            rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
        }

    }
    protected virtual void OnEnable()
    {
        hasHit = false; // so it does not double do it
        age = 0f;

        if (trail != null)
        {
            trail.Clear();
            trail.time = 0.25f;
        }
        
        if(rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    protected virtual void FadeTrailVisuals()
    {
        if (Vector3.Distance(startPos, transform.position) > flyDistance - 1.5f)
        {
            trail.time -= 5f * Time.deltaTime;
        }

        //Debug.Log("Fading trail visuals");
    }

    public virtual void OnCollisionEnter(Collision collision)
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
                damageable.TakeDamage(actualDamage);
                CombatEvents.ReportDamage(attacker, enemy, actualDamage);
                
                if (DelayedProjectiles.IsEnabled)
                {
                    CreateDelayedDamageMark(enemy, collision.GetContact(0).point);
                }
                
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
        if (bulletImpactFX == null) return;
        
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

    protected void CreateDelayedDamageMark(Enemy enemy, Vector3 hitPoint)
    {
        GameObject markObj = new GameObject("DelayedDamageMark");
        markObj.transform.position = hitPoint;
        
        DelayedDamageMark mark = markObj.AddComponent<DelayedDamageMark>();
        mark.Init(enemy, actualDamage, attacker, DelayedProjectiles.DelayTime, DelayedProjectiles.DamageMultiplier);

        if (DelayedProjectiles.MarkVFX != null)
        {
            GameObject vfx = Instantiate(DelayedProjectiles.MarkVFX);
            vfx.transform.position = hitPoint;
            vfx.transform.SetParent(markObj.transform);
            Destroy(vfx, DelayedProjectiles.DelayTime);
        }
        else
        {
            CreateDefaultMarkEffect(markObj);
        }
    }

    private void CreateDefaultMarkEffect(GameObject markObj)
    {
        DelayedDamageMark mark = markObj.GetComponent<DelayedDamageMark>();
        
        Light light = markObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.3f, 0f);
        light.range = 3f;
        light.intensity = 2f;
        
        if (mark != null)
        {
            mark.SetLight(light);
        }

        ParticleSystem ps = markObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = DelayedProjectiles.DelayTime;
        main.startSpeed = 0.5f;
        main.startSize = 0.5f;
        main.startColor = new Color(1f, 0f, 0f, 1f);
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        var emission = ps.emission;
        emission.rateOverTime = 15f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.radial = new ParticleSystem.MinMaxCurve(0.3f);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(0.5f, new AnimationCurve(
            new Keyframe(0f, 0.5f),
            new Keyframe(0.5f, 0.83f),
            new Keyframe(1f, 0.33f)
        ));

        var color = ps.colorOverLifetime;
        color.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(new Color(1f, 0f, 0f), 0f), 
                new GradientColorKey(new Color(1f, 0f, 0f), 0.5f),
                new GradientColorKey(new Color(0.8f, 0f, 0f), 1f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f), 
                new GradientAlphaKey(1f, 0.3f),
                new GradientAlphaKey(0f, 1f) 
            }
        );
        color.color = grad;

        ps.Play();
        
        if (mark != null)
        {
            mark.SetParticles(ps);
        }
    }

}
