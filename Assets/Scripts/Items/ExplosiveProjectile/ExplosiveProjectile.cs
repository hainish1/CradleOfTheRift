using System.Collections.Generic;
using UnityEngine;

public class ExplosiveProjectile : Projectile
{
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

    public override void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        if (((1 << collision.gameObject.layer) & hitMask) == 0) return;

        CreateImpactFX();

        if (ExplosiveProjectiles.IsEnabled)
        {
            SpawnExplosionEffect();
        }

        var enemy = collision.collider.GetComponentInParent<Enemy>();
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
                CombatEvents.ReportDamage(attacker, enemy, actualDamage);
                hasHit = true;
                Debug.Log($"Dealt {actualDamage} damage to {collision.gameObject.name}");
            }
        }

        if (collision.rigidbody != null)
        {
            Vector3 force = rb.linearVelocity.normalized * hitForce;
            collision.rigidbody.AddForceAtPosition(force, collision.contacts[0].point, ForceMode.Impulse);
        }

        ReturnToSource();
    }

    private void SpawnExplosionEffect()
    {
        float radius = ExplosiveProjectiles.AoeRadius;
        float aoeDamage = actualDamage * ExplosiveProjectiles.AoeDamageMultiplier;

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, hitMask);
        HashSet<IDamageable> hit = new HashSet<IDamageable>();

        foreach (var col in hits)
        {
            var enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null) continue;

            var damageable = col.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead && !hit.Contains(damageable))
            {
                damageable.TakeDamage(aoeDamage);
                CombatEvents.ReportDamage(attacker, enemy, aoeDamage);
                hit.Add(damageable);
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
        var explosion = new GameObject("Explosion");
        explosion.transform.position = transform.position;

        var ps = explosion.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.5f;
        main.startSpeed = radius * 0.5f;
        main.startSize = radius * 0.075f;
        main.startColor = new Color(1f, 0.5f, 0f);
        main.maxParticles = 30;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.radial = new ParticleSystem.MinMaxCurve(radius * 0.75f);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.5f, 1.5f),
            new Keyframe(1f, 0f)
        ));

        var color = ps.colorOverLifetime;
        color.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.yellow, 0f), new GradientColorKey(Color.red, 0.5f), new GradientColorKey(Color.black, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0f, 1f) }
        );
        color.color = grad;

        ps.Play();
        Destroy(explosion, 1f);
    }

    void OnDrawGizmos()
    {
        if (ExplosiveProjectiles.IsEnabled)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, ExplosiveProjectiles.AoeRadius);
        }
    }
}
