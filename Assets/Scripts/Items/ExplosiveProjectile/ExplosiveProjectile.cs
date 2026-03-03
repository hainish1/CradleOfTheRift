using System.Collections.Generic;
using UnityEngine;

public class ExplosiveProjectile : Projectile
{
    private GameObject fireballVisual;
    private static Shader cachedShader;
    
    private static Collider[] overlapBuffer = new Collider[64];
    private static HashSet<IDamageable> hitBuffer = new HashSet<IDamageable>();
    
    void Start()
    {
        if (ExplosiveProjectiles.IsEnabled)
        {
            HideOriginalProjectileModel();
            CreateFireballVisual();
            SetProjectileSpeed();
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
        fireballVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fireballVisual.name = "FireballVisual";
        fireballVisual.transform.SetParent(transform);
        fireballVisual.transform.localPosition = Vector3.zero;
        fireballVisual.transform.localScale = Vector3.one * ExplosiveProjectiles.FireballVisualScale;
        
        var col = fireballVisual.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        if (cachedShader == null)
            cachedShader = Shader.Find("Sprites/Default");
        
        var material = new Material(cachedShader);
        material.color = new Color(1f, 0f, 0f, 1f);
        
        var renderer = fireballVisual.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = material;
        }
    }
    
    public override void Update()
    {
        FadeTrailVisuals();
        age += Time.deltaTime;
        
        if (ExplosiveProjectiles.IsEnabled)
        {
            float maxRange = ExplosiveProjectiles.MaxRange > 0f ? ExplosiveProjectiles.MaxRange : flyDistance;
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
            if (ExplosiveProjectiles.IsEnabled) SpawnExplosionEffect();
            ReturnToSource();
            return;
        }
        
        if (gravity != 0f)
        {
            rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
        }
    }
    
    private void SetProjectileSpeed()
    {
        if (rb != null)
            rb.linearVelocity *= ExplosiveProjectiles.ProjectileSpeed;
    }

    public override void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        if (((1 << collision.gameObject.layer) & hitMask) == 0) return;

        CreateImpactFX();

        var enemy = collision.collider.GetComponentInParent<Enemy>();
        if (enemy != null && ExplosiveProjectiles.IsEnabled)
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
                hasHit = true;
            }
        }

        if (collision.rigidbody != null)
        {
            Vector3 force = rb.linearVelocity.normalized * hitForce;
            collision.rigidbody.AddForceAtPosition(force, collision.contacts[0].point, ForceMode.Impulse);
        }

        if (enemy == null && ExplosiveProjectiles.IsEnabled)
            SpawnExplosionEffect();

        ReturnToSource();
    }

    private void SpawnExplosionEffect()
    {
        float radius = ExplosiveProjectiles.AoeRadius;
        float aoeDamage = actualDamage * ExplosiveProjectiles.AoeDamageMultiplier;

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

        if (ExplosiveProjectiles.ExplosionVFX != null)
        {
            var fx = Instantiate(ExplosiveProjectiles.ExplosionVFX);
            fx.transform.position = transform.position;
            float scale = Mathf.Clamp(radius * 0.025f, 0.5f, 0.625f);
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
        if (ExplosiveProjectiles.IsEnabled)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, ExplosiveProjectiles.AoeRadius);
        }
    }
    
    void OnDestroy()
    {
        if (fireballVisual != null)
        {
            var renderer = fireballVisual.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                Destroy(renderer.material);
            }
        }
    }
}
