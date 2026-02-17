using System.Collections.Generic;
using UnityEngine;

public class FirePool : MonoBehaviour
{
    private static readonly Collider[] OverlapBuffer = new Collider[32];
    private static readonly HashSet<Enemy> HitCache = new HashSet<Enemy>();

    private Entity owner;
    private float damage;
    private float radius;
    private float endTime;
    private bool hasDealtDamage;
    private ParticleSystem fireVfx;

    public void Initialize(Entity owner, float damage, float radius, float lifetime)
    {
        this.owner = owner;
        this.damage = damage;
        this.radius = Mathf.Max(0.5f, radius);
        endTime = Time.time + lifetime;
        hasDealtDamage = false;
        BuildVfx();
    }

    private void BuildVfx()
    {
        var obj = new GameObject("FirePoolVFX");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localScale = Vector3.one;

        fireVfx = obj.AddComponent<ParticleSystem>();
        var main = fireVfx.main;
        main.startLifetime = 0.6f;
        main.startSpeed = 0.8f;
        main.startSize = 0.15f;
        main.startColor = new Color(1f, 0.4f, 0f, 0.9f);
        main.maxParticles = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;

        var emission = fireVfx.emission;
        emission.rateOverTime = 25f;

        var shape = fireVfx.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius * 0.5f;

        var colorOverLifetime = fireVfx.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.5f, 0f), 0f), new GradientColorKey(new Color(1f, 0.2f, 0f), 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        fireVfx.Play();
    }

    private void Update()
    {
        if (!hasDealtDamage)
        {
            ApplyDamage();
            hasDealtDamage = true;
        }

        if (Time.time >= endTime)
        {
            Destroy(gameObject);
        }
    }

    private void ApplyDamage()
    {
        if (owner == null) return;

        HitCache.Clear(); 
        Vector3 center = transform.position;
        int hitCount = Physics.OverlapSphereNonAlloc(center, radius, OverlapBuffer, ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = OverlapBuffer[i];
            if (col == null) continue;

            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null || HitCache.Contains(enemy)) continue;

            IDamageable dmg = enemy.GetComponent<IDamageable>();
            if (dmg == null || dmg.IsDead) continue;

            HitCache.Add(enemy);
            dmg.TakeDamage(damage);
            CombatEvents.ReportDamage(owner, enemy, damage, ElementType.Fire);

            TargetFlash flash = enemy.GetComponent<TargetFlash>();
            if (flash != null) flash.Flash();
        }
    }
}
