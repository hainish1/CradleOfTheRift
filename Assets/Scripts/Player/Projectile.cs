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
    private Collider selfCollider;
    protected float age;
    protected Vector3 startPos;
    protected float flyDistance;
    protected Entity attacker;
    protected bool hasHit;

    // Pass-through spear tracking
    private Vector3 savedVelocity;
    private Vector3 savedPosition;
    private int enemiesPassedThrough;

    public virtual void Awake()
    {
        trail = GetComponent<TrailRenderer>();

        rb = GetComponent<Rigidbody>();
        selfCollider = GetComponent<Collider>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.freezeRotation = true;

        // meshRenderer = GetComponent<MeshRenderer>();
    }

    public virtual void Init(Vector3 velocity, LayerMask mask, float damage, float flyDistance = 100, Entity attacker = null)
    {
        rb.linearVelocity = velocity;
        hitMask = mask;
        actualDamage = damage; // USE DAMAGE FROM STATS SYSTEM
        this.attacker = attacker;
        age = 0f;

        rb.freezeRotation = true;

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

    protected virtual void FixedUpdate()
    {
        if (rb != null && !hasHit)
        {
            savedVelocity = rb.linearVelocity;
            savedPosition = rb.position;
        }
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

        // create a natural arc motion or skip once we have hit something
        if (!hasHit && rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
        }

    }
    protected virtual void OnEnable()
    {
        hasHit = false; // so it does not double do it
        age = 0f;
        enemiesPassedThrough = 0;
        savedVelocity = Vector3.zero;
        savedPosition = Vector3.zero;

        if (trail != null)
        {
            trail.Clear();
            trail.time = 0.25f;
        }

        if (rb != null)
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
        // check layer mask
        if (((1 << collision.gameObject.layer) & hitMask) == 0)
            return;

        CreateImpactFX();

        // check if collided with enemy and if yes then damage it
        var enemy = collision.collider.GetComponentInParent<Enemy>();

        // Passthrough spear- pass through enemies, destroy on anything else
        if (PassThroughSpear.IsEnabled && enemy != null
            && enemiesPassedThrough < PassThroughSpear.MaxPassThroughCount)
        {
            // Damage this enemy while passing through
            ApplyEnemyHit(collision, enemy, passingThrough: true);

            // Ignore future collisions with ALL other any colliders on this enemy
            Collider myCollider = GetComponent<Collider>();
            if (myCollider != null)
            {
                Collider[] enemyColliders = enemy.GetComponentsInChildren<Collider>();
                foreach (Collider ec in enemyColliders)
                {
                    Physics.IgnoreCollision(myCollider, ec);
                }
            }

            enemiesPassedThrough++;

            // restore the velocity and position from before the collision so the projectile continues on its original trajectory.
            if (rb != null)
            {
                rb.linearVelocity = savedVelocity;
                rb.angularVelocity = Vector3.zero;
                // Push forward slightly to clear the enemy collider and prevent collision again 
                rb.position = savedPosition + savedVelocity.normalized * 0.1f;
            }

            Debug.Log($"[PassThroughSpear] Passed through {collision.gameObject.name} "
                    + $"({enemiesPassedThrough}/{PassThroughSpear.MaxPassThroughCount})");
            return; // do NOT destroy, keep flying
        }

        // Normal hit 
        hasHit = true; // lock rotation 
        CreateImpactFX();

        if (enemy != null)
        {
            ApplyEnemyHit(collision, enemy);
        }

        // apply physics force
        if (collision.rigidbody != null)
        {
            Vector3 force = rb.linearVelocity.normalized * hitForce;
            collision.rigidbody.AddForceAtPosition(force, collision.contacts[0].point, ForceMode.Impulse);
        }

        ReturnToSource(); // destroy / return to pool
    }

    /// <summary>
    /// Applies damage, knockback, flash, and delayed-damage marks to an enemy.
    /// When passingThrough is true, hasHit is NOT set so the projectile can keep hitting more enemies.
    /// </summary>
    protected void ApplyEnemyHit(Collision collision, Enemy enemy, bool passingThrough = false)
    {
        var kb = enemy.GetComponent<AgentKnockBack>();
        if (kb != null)
        {
            var contact = collision.GetContact(0);
            Vector3 dir = -contact.normal;
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

            if (!passingThrough) hasHit = true;
            Debug.Log($"Dealt {actualDamage} damage to {collision.gameObject.name}");
        }
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
            ObjectPool.instance.ReturnObject(gameObject);
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
