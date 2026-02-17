using System;
using System.Collections.Generic;
using UnityEngine;

public class ElementReactionExplosion : IDisposable
{
    private Entity owner;
    private float explosionDamage;
    private float explosionRadius;
    private const float EXPLOSION_COOLDOWN_PER_ENEMY = 1f;
    private int stacks;
    private float duration;
    private float timer;
    private bool disposed;
    private LayerMask enemyLayer;
    
    private readonly Dictionary<Enemy, float> explosionCooldowns = new Dictionary<Enemy, float>();
    private GameObject explosionVFX;
    private bool isProcessingExplosion = false;
    
    public ElementReactionExplosion(
        Entity owner, 
        float explosionDamage, 
        float explosionRadius, 
        int initialStacks = 1, 
        float durationSec = -1f,
        GameObject explosionVFX = null)
    {
        this.owner = owner;
        this.explosionDamage = explosionDamage;
        this.explosionRadius = explosionRadius;
        this.stacks = initialStacks > 0 ? initialStacks : 1;
        this.duration = durationSec;
        this.timer = durationSec;
        this.explosionVFX = explosionVFX;
        
        enemyLayer = LayerMask.GetMask("Enemy");
        CombatEvents.DamageDealt += OnDamageDealt;
    }
    
    public void AddStack(int count = 1)
    {
        stacks += count > 0 ? count : 1;
        if (stacks <= 0) Dispose();
    }
    
    public void Update(float dt)
    {
        if (disposed) return;
        
        if (duration > 0f)
        {
            timer -= dt;
            if (timer <= 0f)
            {
                Dispose();
                return;
            }
        }
        
        UpdateCooldowns();
    }
    
    private void UpdateCooldowns()
    {
        var toRemove = new List<Enemy>();
        foreach (var kvp in explosionCooldowns)
        {
            if (Time.time >= kvp.Value)
                toRemove.Add(kvp.Key);
        }

        foreach (var enemy in toRemove)
            explosionCooldowns.Remove(enemy);
    }
    
    private void OnDamageDealt(Entity attacker, Component target, float damage, ElementType element)
    {
        if (disposed || attacker != owner) return;
        if (isProcessingExplosion) return;
        if (element != ElementType.Lightning && element != ElementType.Fire) return;
        
        Enemy enemy = target as Enemy;
        if (enemy == null) return;
        
        var tracker = enemy.GetComponent<ElementStatusTracker>();
        if (tracker == null)
            tracker = enemy.gameObject.AddComponent<ElementStatusTracker>();
        
        tracker.RecordElementHit(element);
        
        if (tracker.HasBothElements(ElementType.Lightning, ElementType.Fire))
        {
            if (IsOnCooldown(enemy)) return;
            isProcessingExplosion = true;
            TriggerExplosion(enemy);
            explosionCooldowns[enemy] = Time.time + EXPLOSION_COOLDOWN_PER_ENEMY;
            tracker.ClearStatuses();
            isProcessingExplosion = false;
        }
    }
    
    private bool IsOnCooldown(Enemy enemy)
    {
        if (!explosionCooldowns.TryGetValue(enemy, out float cooldownEnd)) return false;
        return Time.time < cooldownEnd;
    }
    
    private void TriggerExplosion(Enemy centerEnemy)
    {
        if (centerEnemy == null || centerEnemy.GetComponent<IDamageable>()?.IsDead == true) return;
        
        Vector3 explosionPos = centerEnemy.transform.position;
        float damage = explosionDamage * stacks;
        
        Collider[] hits = Physics.OverlapSphere(explosionPos, explosionRadius, enemyLayer);
        HashSet<Enemy> hitEnemies = new HashSet<Enemy>();
        
        foreach (var col in hits)
        {
            var enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null || hitEnemies.Contains(enemy)) continue;
            
            var damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                damageable.TakeDamage(damage);
                hitEnemies.Add(enemy);
            }
        }
        
        foreach (var enemy in hitEnemies)
        {
            CombatEvents.ReportDamage(owner, enemy, damage, ElementType.Fire);
        }
        
        if (explosionVFX != null)
        {
            var fx = UnityEngine.Object.Instantiate(explosionVFX);
            fx.transform.position = explosionPos;
            float scale = Mathf.Clamp(explosionRadius * 0.025f, 0.5f, 0.625f);
            fx.transform.localScale = Vector3.one * scale;
            UnityEngine.Object.Destroy(fx, 1f);
        }
        else
        {
            CreateSimpleExplosionVFX(explosionPos, explosionRadius);
        }
    }
    
    private void CreateSimpleExplosionVFX(Vector3 position, float radius)
    {
        var explosion = new GameObject("ElementReactionExplosion");
        explosion.transform.position = position;
        
        var ps = explosion.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.5f;
        main.startSpeed = radius * 0.5f;
        main.startSize = radius * 0.075f;
        main.startColor = new Color(1f, 0.8f, 0.2f);
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
            new GradientColorKey[] { 
                new GradientColorKey(Color.yellow, 0f), 
                new GradientColorKey(new Color(1f, 0.5f, 0f), 0.5f), 
                new GradientColorKey(Color.black, 1f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1f, 0f), 
                new GradientAlphaKey(0.5f, 0.5f), 
                new GradientAlphaKey(0f, 1f) 
            }
        );
        color.color = grad;
        
        ps.Play();
        UnityEngine.Object.Destroy(explosion, 1f);
    }
    
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        CombatEvents.DamageDealt -= OnDamageDealt;
        explosionCooldowns.Clear();
    }
}
