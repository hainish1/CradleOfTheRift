using System.Collections.Generic;
using UnityEngine;

public class PoisonCloud : MonoBehaviour
{
    private static readonly Dictionary<long, float> s_nextPoisonCloudDamageAllowed = new Dictionary<long, float>(256);
    private static float s_lastGatePruneTime;
    private static readonly List<long> s_pruneKeyBuffer = new List<long>(64);
    private static readonly HashSet<Enemy> s_enemyDedup = new HashSet<Enemy>();

    private static long PackOwnerEnemyIds(Entity attacker, Component target)
    {
        int a = attacker != null ? attacker.GetInstanceID() : 0;
        int b = target != null ? target.GetInstanceID() : 0;
        return ((long)a << 32) | (uint)b;
    }

    private static bool TryConsumeSharedDamageWindow(Entity attacker, Enemy enemy, float interval)
    {
        if (attacker == null || enemy == null) return false;
        long key = PackOwnerEnemyIds(attacker, enemy);
        if (s_nextPoisonCloudDamageAllowed.TryGetValue(key, out float notBefore) && Time.time < notBefore)
            return false;
        s_nextPoisonCloudDamageAllowed[key] = Time.time + interval;
        return true;
    }

    private static void MaybePruneDamageGateMap()
    {
        if (Time.time - s_lastGatePruneTime < 4f) return;
        s_lastGatePruneTime = Time.time;
        if (s_nextPoisonCloudDamageAllowed.Count < 400) return;
        s_pruneKeyBuffer.Clear();
        foreach (var kv in s_nextPoisonCloudDamageAllowed)
        {
            if (kv.Value < Time.time - 2f)
                s_pruneKeyBuffer.Add(kv.Key);
        }
        for (int i = 0; i < s_pruneKeyBuffer.Count; i++)
            s_nextPoisonCloudDamageAllowed.Remove(s_pruneKeyBuffer[i]);
    }

    private static Material s_billboardParticleMat;

    static Material GetOrCreateBillboardMaterial()
    {
        if (s_billboardParticleMat != null) return s_billboardParticleMat;
        Shader shader =
            Shader.Find("Universal Render Pipeline/Particles/Unlit")
            ?? Shader.Find("Particles/Standard Unlit")
            ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended")
            ?? Shader.Find("Sprites/Default");
        if (shader == null) return null;
        s_billboardParticleMat = new Material(shader);
        return s_billboardParticleMat;
    }

    private Entity owner;
    private float damagePerTick;
    private float damageTickInterval;
    private float radius;
    private float endTime;
    private bool initialized;

    private LayerMask enemyLayerMask;
    private readonly Collider[] overlapBuffer = new Collider[64];
    private float _nextDamageTick;

    private Vector3 VerticalCenterOffset => Vector3.up * (radius * 0.5f);

    public void Initialize(Entity owner, float damagePerTick, float radius, float lifetime, float damageTickInterval, GameObject vfxPrefab = null)
    {
        this.owner = owner;
        this.damagePerTick = Mathf.Max(0f, damagePerTick);
        this.damageTickInterval = Mathf.Max(0.1f, damageTickInterval);
        this.radius = Mathf.Max(0.25f, radius);

        enemyLayerMask = LayerMask.GetMask("Enemy");
        endTime = Time.time + Mathf.Max(0.1f, lifetime);
        _nextDamageTick = Time.time + this.damageTickInterval;
        initialized = true;

        if (vfxPrefab != null)
        {
            var fx = Instantiate(vfxPrefab, transform);
            fx.transform.localPosition = Vector3.zero;
            fx.transform.localRotation = Quaternion.identity;
        }
        else
            BuildParticleVfx();
    }

    private void Update()
    {
        if (!initialized) return;

        if (Time.time >= endTime)
        {
            Destroy(gameObject);
            return;
        }

        if (Time.time < _nextDamageTick) return;
        _nextDamageTick = Time.time + damageTickInterval;
        ApplyPoisonDamage();
    }

    private void ApplyPoisonDamage()
    {
        if (owner == null) return;
        MaybePruneDamageGateMap();

        Vector3 center = transform.position + VerticalCenterOffset;
        int hitCount = Physics.OverlapSphereNonAlloc(center, radius, overlapBuffer, enemyLayerMask, QueryTriggerInteraction.Collide);
        s_enemyDedup.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            var col = overlapBuffer[i];
            if (col == null) continue;

            var enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null || !s_enemyDedup.Add(enemy)) continue;

            var damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                if (!TryConsumeSharedDamageWindow(owner, enemy, damageTickInterval))
                    continue;
                damageable.TakeDamage(damagePerTick);
                CombatEvents.ReportDamage(owner, enemy, damagePerTick, ElementType.Poison);
            }
        }
    }

    private void BuildParticleVfx()
    {
        var root = new GameObject("PoisonCloudVFX");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;

        float lift = VerticalCenterOffset.y;
        root.transform.localPosition = new Vector3(0f, lift, 0f);

        CreateFogLayer(root.transform, radius, emission: 240f, startSize: 0.1f, speed: 0.14f, alpha: 0.55f);
        CreateFogLayer(root.transform, radius * 0.85f, emission: 160f, startSize: 0.07f, speed: 0.2f, alpha: 0.42f);
    }

    private static void CreateFogLayer(Transform parent, float shapeRadius, float emission, float startSize, float speed, float alpha)
    {
        var go = new GameObject("FogParticles");
        go.transform.SetParent(parent, false);
        go.transform.localRotation = Quaternion.identity;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(startSize * 0.65f, startSize);
        main.maxParticles = 400;
        main.gravityModifier = -0.04f;

        var startCol = new Color(0.2f, 0.85f, 0.22f, alpha);
        main.startColor = startCol;

        var emissionMod = ps.emission;
        emissionMod.rateOverTime = emission;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = Mathf.Max(0.1f, shapeRadius * 0.55f);
        shape.scale = new Vector3(1f, 2f, 1f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.Local;
        vel.y = new ParticleSystem.MinMaxCurve(0.2f, 0.55f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        noise.frequency = 0.35f;
        noise.scrollSpeed = 0.2f;

        var colLife = ps.colorOverLifetime;
        colLife.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.35f, 0.95f, 0.25f), 0f),
                new GradientColorKey(new Color(0.05f, 0.45f, 0.08f), 0.55f),
                new GradientColorKey(new Color(0.02f, 0.28f, 0.05f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(alpha, 0f),
                new GradientAlphaKey(0f, 1f),
            });
        colLife.color = grad;

        var rnd = ps.rotationOverLifetime;
        rnd.enabled = true;
        rnd.z = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortingOrder = 2;
        var mat = GetOrCreateBillboardMaterial();
        if (mat != null)
            r.sharedMaterial = mat;
    }
}
