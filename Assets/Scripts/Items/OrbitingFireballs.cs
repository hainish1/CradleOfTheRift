using System;
using System.Collections.Generic;
using UnityEngine;

public class OrbitingFireballs : IDisposable
{
    private Entity owner;
    private int stacks;
    private float duration;
    private float timer;
    private bool disposed;
    private float orbitRadius;
    private float rotationSpeed;
    private float baseDamage;
    private List<Fireball> fireballs = new List<Fireball>();
    private bool isPaused;

    public OrbitingFireballs(Entity owner, float damage, float orbitRadius, float rotationSpeed, int initialStacks = 1, float durationSec = -1f)
    {
        this.owner = owner;
        this.stacks = Mathf.Max(1, initialStacks);
        this.duration = durationSec;
        this.timer = durationSec;
        this.baseDamage = damage;
        this.orbitRadius = orbitRadius;
        this.rotationSpeed = rotationSpeed;

        for (int i = 0; i < 3; i++)
            SpawnFireball(-1f);
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
        float angleStep = 360f / fireballs.Count;

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

            float angle = (Time.time * rotationSpeed + angleStep * i) % 360f;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad) * orbitRadius * 0.75f, 0.8f, Mathf.Sin(rad) * orbitRadius * 0.75f);
            fireballs[i].transform.position = playerPos + offset;
        }
    }

    public void SpawnFireball(float lifetime = -1f)
    {
        if (disposed || owner == null) return;

        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.transform.localScale = Vector3.one * 1.2f;

        var col = obj.GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.0f;

        var rb = obj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var script = obj.AddComponent<Fireball>();
        script.Initialize(owner, baseDamage, lifetime, obj.GetComponent<Renderer>());
        fireballs.Add(script);
    }

    public void RemoveFireball(Fireball fireball)
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

        foreach (var ball in fireballs)
            if (ball != null) ball.Destroy();
        
        fireballs.Clear();
    }
}

public class Fireball : MonoBehaviour
{
    private static Shader cachedShader;
    private Entity playerEntity;
    private float damage;
    private float lifetime;
    private float elapsed;
    private bool destroyed;
    private Material fireballMaterial;
    private Dictionary<Enemy, float> lastHit = new Dictionary<Enemy, float>();

    public bool IsDestroyed => destroyed;
    public bool IsExpired => lifetime > 0f && elapsed >= lifetime;

    public void Initialize(Entity player, float dmg, float life, Renderer renderer)
    {
        playerEntity = player;
        damage = dmg;
        lifetime = life;
        
        if (cachedShader == null)
            cachedShader = Shader.Find("Sprites/Default");
        
        fireballMaterial = new Material(cachedShader);
        fireballMaterial.color = new Color(1f, 0.3f, 0f, 0.8f);
        renderer.material = fireballMaterial;
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
        
        if (fireballMaterial != null)
            UnityEngine.Object.Destroy(fireballMaterial);
        
        if (gameObject != null)
            UnityEngine.Object.Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (fireballMaterial != null)
            UnityEngine.Object.Destroy(fireballMaterial);
        lastHit.Clear();
    }
}
