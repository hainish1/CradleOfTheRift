using System;
using System.Collections.Generic;
using UnityEngine;

public class OrbitingFireballsTest : IDisposable
{
    private Entity owner;
    private int stacks;
    private float duration;
    private float timer;
    private bool disposed;
    private float orbitRadius;
    private float rotationSpeed;
    private float baseDamage;
    private List<FireballTest> fireballs = new List<FireballTest>();
    private bool isPaused;

    private float lightningDamageAccumulated;
    private float damagePerFireball;
    private int maxFireballs;
    private float fireballLifetime;

    public OrbitingFireballsTest(Entity owner, float damage, float orbitRadius, float rotationSpeed, float damageThreshold = 100f, int maxCount = 20, float ballLifetime = 5f, int initialStacks = 1, float durationSec = -1f)
    {
        this.owner = owner;
        this.stacks = Mathf.Max(1, initialStacks);
        this.duration = durationSec;
        this.timer = durationSec;
        this.baseDamage = damage;
        this.orbitRadius = orbitRadius;
        this.rotationSpeed = rotationSpeed;
        this.damagePerFireball = damageThreshold;
        this.maxFireballs = maxCount;
        this.fireballLifetime = ballLifetime;
        this.lightningDamageAccumulated = 0f;

        CombatEvents.DamageDealt += OnDamageDealt;
    }

    private void OnDamageDealt(Entity attacker, Component target, float damage, ElementType element)
    {
        if (disposed || attacker != owner || element != ElementType.Lightning) return;
        
        lightningDamageAccumulated += damage;
        
        while (lightningDamageAccumulated >= damagePerFireball && fireballs.Count < maxFireballs)
        {
            lightningDamageAccumulated -= damagePerFireball;
            SpawnFireball(fireballLifetime);
        }
    }

    public void AddStack(int count = 1)
    {
        stacks += count;
        if (stacks <= 0) Dispose();
    }

    public void Pause()
    {
        isPaused = true;
        foreach (var ball in fireballs)
            if (ball != null) ball.gameObject.SetActive(false);
    }

    public void Resume()
    {
        isPaused = false;
        foreach (var ball in fireballs)
            if (ball != null) ball.gameObject.SetActive(true);
    }

    public void Update(float dt)
    {
        if (disposed || owner == null || isPaused) return;

        if (duration > 0f)
        {
            timer -= dt;
            if (timer <= 0f)
            {
                Dispose();
                return;
            }
        }

        Vector3 playerPos = owner.transform.position;
        int activeCount = fireballs.Count;
        float angleStep = activeCount > 0 ? 360f / activeCount : 0f;
        float uniformScale = CalculateFireballScale(activeCount);

        for (int i = fireballs.Count - 1; i >= 0; i--)
        {
            if (fireballs[i] == null || fireballs[i].IsDestroyed)
            {
                fireballs.RemoveAt(i);
                continue;
            }

            fireballs[i].UpdateLifetime(dt);

            if (fireballs[i].IsExpired)
            {
                fireballs[i].Destroy();
                fireballs.RemoveAt(i);
                continue;
            }

            fireballs[i].transform.localScale = Vector3.one * uniformScale;

            float angle = (Time.time * rotationSpeed + angleStep * i) % 360f;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad) * orbitRadius * 0.75f, 0.8f, Mathf.Sin(rad) * orbitRadius * 0.75f);
            fireballs[i].transform.position = playerPos + offset;
        }
    }

    public void SpawnFireball(float lifetime = -1f)
    {
        if (disposed || owner == null || fireballs.Count >= maxFireballs) return;

        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.transform.localScale = Vector3.one * 1.2f;

        var col = obj.GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.0f;

        var rb = obj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(1f, 0.3f, 0f, 0.8f);
        obj.GetComponent<Renderer>().material = mat;

        var script = obj.AddComponent<FireballTest>();
        script.Initialize(owner, baseDamage, lifetime);
        fireballs.Add(script);
    }

    private float CalculateFireballScale(int count)
    {
        if (count <= 3) return 1.2f;
        if (count <= 10) return Mathf.Lerp(1.2f, 0.8f, (count - 3) / 7f);
        return Mathf.Lerp(0.8f, 0.5f, (count - 10) / 10f);
    }

    public void RemoveFireball(FireballTest fireball)
    {
        if (fireballs.Contains(fireball))
        {
            fireballs.Remove(fireball);
            fireball.Destroy();
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        CombatEvents.DamageDealt -= OnDamageDealt;

        foreach (var ball in fireballs)
            if (ball != null) ball.Destroy();
        
        fireballs.Clear();
    }
}

public class FireballTest : MonoBehaviour
{
    private Entity playerEntity;
    private float damage;
    private float lifetime;
    private float elapsed;
    private bool destroyed;
    private Dictionary<Enemy, float> lastHit = new Dictionary<Enemy, float>();

    public bool IsDestroyed => destroyed;
    public bool IsExpired => lifetime > 0f && elapsed >= lifetime;

    public void Initialize(Entity player, float dmg, float life)
    {
        playerEntity = player;
        damage = dmg;
        lifetime = life;
    }

    public void UpdateLifetime(float dt)
    {
        if (lifetime > 0f) elapsed += dt;
    }

    void OnTriggerEnter(Collider other)
    {
        if (destroyed || playerEntity == null) return;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) return;

        if (lastHit.TryGetValue(enemy, out float last) && Time.time - last < 1.0f)
            return;

        var dmg = enemy.GetComponent<IDamageable>();
        if (dmg != null && !dmg.IsDead)
        {
            dmg.TakeDamage(damage);
            CombatEvents.ReportDamage(playerEntity, enemy, damage, ElementType.Fire);
            lastHit[enemy] = Time.time;

            var flash = enemy.GetComponentInChildren<TargetFlash>();
            if (flash != null) flash.Flash();
        }
    }

    public void Destroy()
    {
        if (destroyed) return;
        destroyed = true;
        if (gameObject != null) UnityEngine.Object.Destroy(gameObject);
    }

    void OnDestroy() => lastHit.Clear();
}
