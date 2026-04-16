using System.Collections.Generic;
using UnityEngine;

public class ExplosiveProjectile : Projectile
{
    [SerializeField] private bool alwaysExplosive = false;

    [Tooltip("Used when alwaysExplosive is true and the upgrade thing is not active. Otherwise values come from the upgrade")]
    [SerializeField] private float standaloneAoeRadius = 4f;
    [SerializeField] private float standaloneAoeDamageMultiplier = 1f;
    [SerializeField] private float standaloneMaxRange = 0f;
    [SerializeField] private float standaloneFireballVisualScale = 1f;
    [SerializeField] private GameObject standaloneExplosionVFX;
    [SerializeField] private GameObject standaloneTravelVFX;

    private GameObject fireballVisual;
    private static Shader cachedShader;

    private static Collider[] overlapBuffer = new Collider[64];
    private static HashSet<IDamageable> hitBuffer = new HashSet<IDamageable>();

    private const string FireballVisualLayerName = "Projectile";

    private bool IsActive => alwaysExplosive || ExplosiveProjectiles.IsEnabled;
    private float CurrentAoeRadius => alwaysExplosive && !ExplosiveProjectiles.IsEnabled ? standaloneAoeRadius : ExplosiveProjectiles.AoeRadius;
    private float CurrentAoeDamageMultiplier => alwaysExplosive && !ExplosiveProjectiles.IsEnabled ? standaloneAoeDamageMultiplier : ExplosiveProjectiles.AoeDamageMultiplier;
    private float CurrentMaxRange => alwaysExplosive && !ExplosiveProjectiles.IsEnabled ? standaloneMaxRange : ExplosiveProjectiles.MaxRange;
    private float CurrentFireballScale => alwaysExplosive && !ExplosiveProjectiles.IsEnabled ? standaloneFireballVisualScale : ExplosiveProjectiles.FireballVisualScale;
    private GameObject CurrentExplosionVFX => alwaysExplosive && !ExplosiveProjectiles.IsEnabled ? standaloneExplosionVFX : ExplosiveProjectiles.ExplosionVFX;
    private GameObject CurrentTravelVFX => alwaysExplosive && !ExplosiveProjectiles.IsEnabled ? standaloneTravelVFX : ExplosiveProjectiles.FireballTravelVFX;

    protected override void OnEnable()
    {
        base.OnEnable();
        if (IsActive)
        {
            // For alwaysExplosive (Mace), the prefab itself is already the fireball
            if (!alwaysExplosive) HideOriginalProjectileModel();

            if (fireballVisual == null) CreateFireballVisual();
            else fireballVisual.SetActive(true);
        }
    }

    public override void Init(Vector3 velocity, LayerMask mask, float damage, float flyDistance = 100, Entity attacker = null)
    {
        base.Init(velocity, mask, damage, flyDistance, attacker);

        if (IsActive && fireballVisual == null)
        {
            if (!alwaysExplosive) HideOriginalProjectileModel();
            CreateFireballVisual();
        }

        ResetTravelVFX();

        if (!IsActive || rb == null) return;

        // override speed when the upgrade proivides one, otherwise use from Shooter
        if (ExplosiveProjectiles.IsEnabled)
        {
            Vector3 dir = velocity.sqrMagnitude > 0.0001f ? velocity.normalized : transform.forward;
            rb.linearVelocity = dir * ExplosiveProjectiles.ProjectileSpeed;
        }
    }

    private void HideOriginalProjectileModel()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = false;
        }
    }

    private void CreateFireballVisual()
    {
        bool usingTravelVFX = CurrentTravelVFX != null;

        if (usingTravelVFX)
        {
            Debug.Log("Using assigned travel VFX prefab for ExplosiveProjectile.");
            fireballVisual = Instantiate(CurrentTravelVFX, transform);
            fireballVisual.name = "FireballVisual";
            fireballVisual.transform.localPosition = Vector3.zero;
            fireballVisual.transform.localScale = Vector3.one;

        }
        else
        {
            Debug.LogWarning("Travel VFX prefab not assigned for ExplosiveProjectile. Using simple sphere visual.");
            fireballVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fireballVisual.name = "FireballVisual";
            fireballVisual.transform.SetParent(transform);
            fireballVisual.transform.localPosition = Vector3.zero;
            fireballVisual.transform.localScale = Vector3.one * CurrentFireballScale;
            //CreateSimpleFireballVisual();

            // int layer = LayerMask.NameToLayer(FireballVisualLayerName);
            // if (layer >= 0)
            //     fireballVisual.layer = layer;

            // var col = fireballVisual.GetComponent<Collider>();
            // if (col != null)
            //     DestroyImmediate(col);

            // if (cachedShader == null)
            //     cachedShader = Shader.Find("Sprites/Default");

            // var material = new Material(cachedShader);
            // material.color = new Color(1f, 0f, 0f, 1f);

            // var renderer = fireballVisual.GetComponent<Renderer>();
            // if (renderer != null)
            //     renderer.material = material;
        }

        int layer = LayerMask.NameToLayer(FireballVisualLayerName);
        if (layer >= 0)
            fireballVisual.layer = layer;

        var col = fireballVisual.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        // Keep travel VFX prefab materials untouched.
        // Only apply the red fallback material to the primitive sphere fallback.
        if (!usingTravelVFX)
        {
            if (cachedShader == null)
                cachedShader = Shader.Find("Sprites/Default");

            var material = new Material(cachedShader);
            material.color = new Color(1f, 0f, 0f, 1f);

            var renderer = fireballVisual.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material = material;
        }
        // fireballVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        // fireballVisual.name = "FireballVisual";
        // fireballVisual.transform.SetParent(transform);
        // fireballVisual.transform.localPosition = Vector3.zero;
        // fireballVisual.transform.localScale = Vector3.one * ExplosiveProjectiles.FireballVisualScale;

        // int layer = LayerMask.NameToLayer(FireballVisualLayerName);
        // if (layer >= 0)
        //     fireballVisual.layer = layer;

        // var col = fireballVisual.GetComponent<Collider>();
        // if (col != null)
        //     DestroyImmediate(col);

        // if (cachedShader == null)
        //     cachedShader = Shader.Find("Sprites/Default");

        // var material = new Material(cachedShader);
        // material.color = new Color(1f, 0f, 0f, 1f);

        // var renderer = fireballVisual.GetComponent<Renderer>();
        // if (renderer != null)
        //     renderer.material = material;
    }

    public override void Update()
    {
        FadeTrailVisuals();
        age += Time.deltaTime;

        if (IsActive)
        {
            float configuredMaxRange = CurrentMaxRange;
            float maxRange = configuredMaxRange > 0f ? configuredMaxRange : flyDistance;
            if (maxRange > 0f)
            {
                float dist = Vector3.Distance(startPos, transform.position);
                if (dist >= maxRange)
                {
                    SpawnExplosionEffect();
                    ReturnToSource();
                    return;
                }
            }
        }

        if (age >= lifeTime)
        {
            if (IsActive) SpawnExplosionEffect();
            ReturnToSource();
            return;
        }
        
        if (gravity != 0f)
        {
            rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
        }
    }

    private void ResetTravelVFX()
    {
        if (fireballVisual == null) return;

        var trails = fireballVisual.GetComponentsInChildren<TrailRenderer>(true);
        foreach (var t in trails)
        {
            t.Clear();
            t.emitting = false;
            t.emitting = true;
        }

        var particles = fireballVisual.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particles)
        {
            ps.Clear(true);
            ps.Play(true);
        }
    }

    public override void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        if (((1 << collision.gameObject.layer) & hitMask) == 0) return;

        CreateImpactFX();

        var enemy = collision.collider.GetComponentInParent<Enemy>();
        if (enemy != null && IsActive)
        {
            SpawnExplosionEffect();
            ReturnToSource();
            return;
        }

        if (enemy != null)
        {
            var kb = enemy?.GetComponent<AgentKnockBack>();
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
                CombatEvents.ReportDamage(attacker, enemy, actualDamage, ElementType.Fire);

                if (DelayedProjectiles.IsEnabled)
                {
                    CreateDelayedDamageMark(enemy, collision.GetContact(0).point);
                }

                hasHit = true;
            }
        }

        if (collision.rigidbody != null)
        {
            Vector3 force = rb.linearVelocity.normalized * hitForce;
            collision.rigidbody.AddForceAtPosition(force, collision.contacts[0].point, ForceMode.Impulse);
        }

        if (enemy == null && IsActive)
            SpawnExplosionEffect();

        ReturnToSource();
    }

    private void SpawnExplosionEffect()
    {
        float radius = CurrentAoeRadius;
        float aoeDamage = actualDamage * CurrentAoeDamageMultiplier;

        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, radius, overlapBuffer, hitMask);
        hitBuffer.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            var col = overlapBuffer[i];
            var enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null) continue;

            var damageable = col.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead && !hitBuffer.Contains(damageable))
            {
                damageable.TakeDamage(aoeDamage);
                CombatEvents.ReportDamage(attacker, enemy, aoeDamage, ElementType.Fire);
                hitBuffer.Add(damageable);

                if (DelayedProjectiles.IsEnabled)
                {
                    CreateDelayedDamageMark(enemy, col.ClosestPoint(transform.position));
                }

                var dotDebuff = enemy.GetComponent<DotDebuff>();
                if (dotDebuff != null)
                {
                    float remainingDamage = dotDebuff.GetRemainingPoisonDamageAndClear(out bool hadPoison);
                    if (hadPoison && remainingDamage > 0f)
                    {
                        float bonusDamage = remainingDamage * 2f;
                        damageable.TakeDamage(bonusDamage);
                        CombatEvents.ReportDamage(attacker, enemy, bonusDamage, ElementType.Poison);
                    }
                }
            }
        }

        if (CurrentExplosionVFX != null)
        {
            var fx = Instantiate(CurrentExplosionVFX);
            fx.transform.position = transform.position;
            float scale = Mathf.Clamp(radius * 0.025f, 0.5f, 0.625f) * 2;   // idk its really not big enough so i added a x2
            fx.transform.localScale = Vector3.one * scale;
            Destroy(fx, 1f);
        }
        else
        {
            CreateSimpleExplosionVFX(radius);
        }
    }

    private void CreateSimpleExplosionVFX(float radius)
    {
        var explosion = new GameObject("FireballExplosion");
        explosion.transform.position = transform.position;

        var ps = explosion.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 1f;
        main.startSpeed = radius * 0.8f;
        main.startSize = new ParticleSystem.MinMaxCurve(radius * 0.1f, radius * 0.15f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.9f, 0f),
            new Color(1f, 0.4f, 0f)
        );
        main.maxParticles = 60;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 60) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.radial = new ParticleSystem.MinMaxCurve(radius * 1.2f);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.5f),
            new Keyframe(0.3f, 1.5f),
            new Keyframe(1f, 0f)
        ));

        var color = ps.colorOverLifetime;
        color.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.yellow, 0.2f), 
                new GradientColorKey(new Color(1f, 0.5f, 0f), 0.5f),
                new GradientColorKey(Color.red, 0.8f),
                new GradientColorKey(new Color(0.2f, 0.1f, 0.1f), 1f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f), 
                new GradientAlphaKey(0.9f, 0.3f),
                new GradientAlphaKey(0.5f, 0.6f), 
                new GradientAlphaKey(0f, 1f) 
            }
        );
        color.color = grad;

        ps.Play();
        Destroy(explosion, 2f);
    }

    void OnDrawGizmos()
    {
        if (IsActive)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, CurrentAoeRadius);
        }
    }

    void OnDestroy()
    {
        if (fireballVisual != null)
        {
            var r = fireballVisual.GetComponent<Renderer>();
            if (r != null && r.material != null)
                Destroy(r.material);
        }
    }
}
