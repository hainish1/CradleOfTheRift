using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashDamage : IDisposable
{
    private Entity owner;
    private PlayerMovement playerMovement;
    private CharacterController characterController;
    private Collider playerCollider;
    private int stacks;
    private float duration;
    private float timer;
    private bool disposed;

    private float baseDamage;
    private float baseDamageRange;
    
    private HashSet<Collider> ignoredColliders = new HashSet<Collider>();
    
    private GameObject effectObject;
    private TrailRenderer trailRenderer;
    private ParticleSystem particleEffect;

    public static float Damage { get; private set; }
    public static float DamageRange { get; private set; }
    public static bool IsEnabled { get; private set; }

    public DashDamage(Entity owner, float dashDamage, float dashDamageRange, int initialStacks, float durationSec = -1f)
    {
        this.owner = owner;
        stacks = Mathf.Max(1, initialStacks);
        duration = durationSec;
        timer = durationSec;

        playerMovement = owner.GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            Debug.LogError("DashDamage requires PlayerMovement component");
            return;
        }

        characterController = owner.GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("DashDamage requires CharacterController component");
            return;
        }

        playerCollider = owner.GetComponent<Collider>();
        if (playerCollider == null)
        {
            playerCollider = owner.GetComponentInChildren<Collider>();
        }

        baseDamage = dashDamage;
        baseDamageRange = dashDamageRange;

        IsEnabled = true;
        UpdateValues();

        playerMovement.DashCooldownStarted += OnDashStarted;
    }

    public void AddStack(int count = 1)
    {
        stacks += count;
        if (stacks <= 0) Dispose();
        else UpdateValues();
    }

    private void UpdateValues()
    {
        Damage = baseDamage * stacks;
        DamageRange = baseDamageRange * (1f + (stacks - 1) * 0.2f);
    }

    public void Update(float dt)
    {
        if (duration < 0f || disposed) return;
        timer -= dt;
        if (timer <= 0f) Dispose();
    }

    private void OnDashStarted(float dashDuration)
    {
        if (disposed || !IsEnabled) return;
        
        if (playerCollider != null)
        {
            IgnoreAllEnemyCollisions();
        }
        
        playerMovement.StartCoroutine(DashDamageCoroutine(dashDuration));
    }

    private IEnumerator DashDamageCoroutine(float dashDuration)
    {
        CreateDashEffect();
        
        float elapsed = 0f;
        float checkInterval = 0.1f;
        float collisionCheckInterval = 0.05f;
        float lastCollisionCheck = 0f;
        HashSet<Enemy> hitEnemies = new HashSet<Enemy>();

        while (elapsed < dashDuration)
        {
            yield return new WaitForSeconds(checkInterval);
            elapsed += checkInterval;
            lastCollisionCheck += checkInterval;

            if (playerMovement == null || owner == null) break;

            if (playerCollider != null && lastCollisionCheck >= collisionCheckInterval)
            {
                IgnoreAllEnemyCollisions();
                lastCollisionCheck = 0f;
            }

            UpdateDashEffect();

            Vector3 playerPos = playerMovement.transform.position;
            Vector3 behindDirection = -playerMovement.transform.forward;

            Enemy[] allEnemies = UnityEngine.Object.FindObjectsOfType<Enemy>();
            
            foreach (Enemy enemy in allEnemies)
            {
                if (enemy == null || hitEnemies.Contains(enemy)) continue;
                
                float distance = Vector3.Distance(enemy.transform.position, playerPos);
                if (distance > DamageRange) continue;
                
                Vector3 toEnemy = (enemy.transform.position - playerPos).normalized;
                toEnemy.y = 0f;
                toEnemy.Normalize();
                
                float dot = Vector3.Dot(behindDirection, toEnemy);
                if (dot > 0.3f)
                {
                    hitEnemies.Add(enemy);
                    
                    var damageable = enemy.GetComponent<IDamageable>();
                    if (damageable != null && !damageable.IsDead)
                    {
                        damageable.TakeDamage(Damage);
                        CombatEvents.ReportDamage(owner, enemy, Damage);
                        
                        var flash = enemy.GetComponentInChildren<TargetFlash>();
                        if (flash != null) flash.Flash();
                    }
                }
            }
        }

        RestoreAllEnemyCollisions();
        DestroyDashEffect();
    }

    private void CreateDashEffect()
    {
        if (playerMovement == null) return;

        effectObject = new GameObject("DashDamageTrail");
        effectObject.transform.position = playerMovement.transform.position;
        effectObject.transform.rotation = playerMovement.transform.rotation;

        trailRenderer = effectObject.AddComponent<TrailRenderer>();
        trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
        trailRenderer.startColor = new Color(1f, 0.3f, 0f, 0.8f);
        trailRenderer.endColor = new Color(1f, 0.1f, 0f, 0.2f);
        trailRenderer.startWidth = 0.2f;
        trailRenderer.endWidth = 0.05f;
        trailRenderer.time = 2f;
        trailRenderer.minVertexDistance = 0.1f;
        trailRenderer.sortingOrder = 1;

        particleEffect = effectObject.AddComponent<ParticleSystem>();
        var main = particleEffect.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 1f;
        main.startSize = 0.15f;
        main.startColor = new Color(1f, 0.3f, 0f, 0.8f);
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.playOnAwake = true;

        var emission = particleEffect.emission;
        emission.rateOverTime = 30f;

        var shape = particleEffect.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 30f;
        shape.radius = 0.3f;
        shape.rotation = new Vector3(0f, 180f, 0f);

        var velocity = particleEffect.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.radial = new ParticleSystem.MinMaxCurve(1.5f);

        var size = particleEffect.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(0.15f, new AnimationCurve(
            new Keyframe(0f, 0.15f),
            new Keyframe(0.5f, 0.25f),
            new Keyframe(1f, 0f)
        ));

        var color = particleEffect.colorOverLifetime;
        color.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(new Color(1f, 0.5f, 0f), 0f), 
                new GradientColorKey(new Color(1f, 0.2f, 0f), 0.5f),
                new GradientColorKey(new Color(1f, 0f, 0f), 1f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0.8f, 0f), 
                new GradientAlphaKey(1f, 0.3f),
                new GradientAlphaKey(0f, 1f) 
            }
        );
        color.color = grad;

        var renderer = particleEffect.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        particleEffect.Play();
    }

    private void UpdateDashEffect()
    {
        if (effectObject == null || playerMovement == null) return;

        Vector3 playerPos = playerMovement.transform.position;
        Vector3 behindDirection = -playerMovement.transform.forward;
        float offsetDistance = DamageRange * 0.5f;

        effectObject.transform.position = playerPos + behindDirection * offsetDistance;
        effectObject.transform.rotation = Quaternion.LookRotation(behindDirection);
    }

    private void DestroyDashEffect()
    {
        if (effectObject == null || playerMovement == null) return;
        
        if (particleEffect != null) particleEffect.Stop();
        
        float waitTime = trailRenderer != null ? trailRenderer.time : 2f;
        playerMovement.StartCoroutine(DestroyEffectDelayed(waitTime));
    }

    private IEnumerator DestroyEffectDelayed(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        
        if (effectObject != null)
        {
            UnityEngine.Object.Destroy(effectObject);
            effectObject = null;
            trailRenderer = null;
            particleEffect = null;
        }
    }

    private void IgnoreAllEnemyCollisions()
    {
        if (playerMovement == null || playerCollider == null) return;

        Collider[] enemyColliders = Physics.OverlapSphere(
            playerMovement.transform.position, 
            100f, 
            LayerMask.GetMask("Enemy")
        );

        foreach (Collider enemyCol in enemyColliders)
        {
            if (enemyCol == null || enemyCol == playerCollider) continue;
            if (ignoredColliders.Contains(enemyCol)) continue;

            Physics.IgnoreCollision(playerCollider, enemyCol, true);
            ignoredColliders.Add(enemyCol);
        }
    }

    private void RestoreAllEnemyCollisions()
    {
        if (playerCollider == null) return;

        foreach (Collider enemyCol in ignoredColliders)
        {
            if (enemyCol != null)
            {
                Physics.IgnoreCollision(playerCollider, enemyCol, false);
            }
        }
        ignoredColliders.Clear();
    }

    public void Dispose()
    {
        if (disposed) return;
        
        RestoreAllEnemyCollisions();
        DestroyDashEffect();
        
        disposed = true;
        IsEnabled = false;
        Damage = 0f;
        DamageRange = 0f;

        if (playerMovement != null)
        {
            playerMovement.DashCooldownStarted -= OnDashStarted;
        }
    }
}

