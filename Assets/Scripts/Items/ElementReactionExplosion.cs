using System;
using System.Collections.Generic;
using UnityEngine;

public class ElementReactionExplosion : IDisposable
{
    private Entity owner;
    private float explosionDamage;
    private float explosionRadius;
    private float cooldownPerEnemy;
    private int stacks;
    private float duration;
    private float timer;
    private bool disposed;
    private LayerMask enemyLayer;
    
    private readonly Dictionary<Enemy, float> explosionCooldowns = new Dictionary<Enemy, float>();
    private GameObject explosionVFX;
    private AK.Wwise.Event explosionSFX;
    private bool isProcessingExplosion = false;

    private static readonly Collider[] s_overlapBuffer = new Collider[64];
    private readonly List<Enemy> _expiredEnemies = new List<Enemy>();
    private readonly HashSet<Enemy> _explosionHitEnemies = new HashSet<Enemy>();
    
    public ElementReactionExplosion(
        Entity owner, 
        float explosionDamage, 
        float explosionRadius, 
        float cooldownPerEnemy = 1f,
        int initialStacks = 1, 
        float durationSec = -1f,
        GameObject explosionVFX = null,
        AK.Wwise.Event explosionSFX = null)
    {
        this.owner = owner;
        this.explosionDamage = explosionDamage;
        this.explosionRadius = explosionRadius;
        this.cooldownPerEnemy = Mathf.Max(0f, cooldownPerEnemy);
        this.stacks = initialStacks > 0 ? initialStacks : 1;
        this.duration = durationSec;
        this.timer = durationSec;
        this.explosionVFX = explosionVFX;
        this.explosionSFX = explosionSFX;
        
        enemyLayer = LayerMask.GetMask("Enemy");
        CombatEvents.DamageDealt += OnDamageDealt;
    }
    
    public void AddStack(int count = 1)
    {
        stacks += count;
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
        _expiredEnemies.Clear();
        foreach (var kvp in explosionCooldowns)
        {
            if (Time.time >= kvp.Value)
                _expiredEnemies.Add(kvp.Key);
        }

        for (int i = 0; i < _expiredEnemies.Count; i++)
            explosionCooldowns.Remove(_expiredEnemies[i]);
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
            explosionCooldowns[enemy] = Time.time + cooldownPerEnemy;
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
        
        int hitCount = Physics.OverlapSphereNonAlloc(explosionPos, explosionRadius, s_overlapBuffer, enemyLayer);
        _explosionHitEnemies.Clear();
        
        for (int i = 0; i < hitCount; i++)
        {
            var col = s_overlapBuffer[i];
            var enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null || _explosionHitEnemies.Contains(enemy)) continue;
            
            var damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                damageable.TakeDamage(damage);
                _explosionHitEnemies.Add(enemy);
            }
        }
        
        foreach (var enemy in _explosionHitEnemies)
        {
            CombatEvents.ReportDamage(owner, enemy, damage, ElementType.Fire);
        }
        
        GameObject sfxEmitter = null;
        if (explosionVFX != null)
        {
            var fx = UnityEngine.Object.Instantiate(explosionVFX);
            fx.transform.position = explosionPos;
            float scale = Mathf.Clamp(explosionRadius * 0.025f, 0.5f, 0.625f);
            fx.transform.localScale = Vector3.one * scale;
            UnityEngine.Object.Destroy(fx, 1f);
            sfxEmitter = fx;
        }
        else
        {
            sfxEmitter = CreateSimpleExplosionVFX(explosionPos, explosionRadius);
        }

        if (explosionSFX != null && explosionSFX.IsValid() && sfxEmitter != null)
            explosionSFX.Post(sfxEmitter);
    }
    
    private GameObject CreateSimpleExplosionVFX(Vector3 position, float radius)
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
        return explosion;
    }
    
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        CombatEvents.DamageDealt -= OnDamageDealt;
        explosionCooldowns.Clear();
    }
}
