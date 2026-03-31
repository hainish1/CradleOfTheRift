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
    private float bonusDamage;
    private float bonusSpeed;
    private List<Fireball> fireballs = new List<Fireball>();
    private bool isPaused;
    private GameObject fireballVFX;

    private static readonly int[] RingCapacity = { 6, 12, 24 };
    private const int MaxFireballs = 6 + 12 + 24;

    public OrbitingFireballs(Entity owner, float damage, float orbitRadius, float rotationSpeed, int initialStacks = 1, float durationSec = -1f, GameObject fireballVFX = null)
    {
        this.owner = owner;
        this.stacks = Mathf.Max(1, initialStacks);
        this.duration = durationSec;
        this.timer = durationSec;
        this.baseDamage = damage;
        this.orbitRadius = orbitRadius;
        this.rotationSpeed = rotationSpeed;
        this.fireballVFX = fireballVFX;

        for (int i = 0; i < 3; i++)
            SpawnFireball(-1f);
    }

    public bool IsDisposed => disposed;

    public void AddStack(int count = 1)
    {
        stacks += count;
        if (stacks <= 0) Dispose();
    }

    public void AddDamageBonus(float amount) { bonusDamage += amount; UpdateAllFireballDamages(); }
    public void RemoveDamageBonus(float amount) { bonusDamage -= amount; UpdateAllFireballDamages(); }
    public void AddSpeedBonus(float amount) => bonusSpeed += amount;
    public void RemoveSpeedBonus(float amount) => bonusSpeed -= amount;

    public void AddBonusBalls(int count)
    {
        for (int i = 0; i < count; i++) SpawnFireball(-1f);
    }

    public void RemoveBonusBalls(int count)
    {
        for (int i = fireballs.Count - 1; i >= 0 && count > 0; i--)
        {
            if (fireballs[i] == null || !fireballs[i].IsPermanent) continue;
            fireballs[i].Destroy();
            fireballs.RemoveAt(i);
            count--;
        }
    }

    private void UpdateAllFireballDamages()
    {
        float effective = baseDamage + bonusDamage;
        foreach (var ball in fireballs)
            if (ball != null && !ball.IsDestroyed) ball.SetDamage(effective);
    }

    private float CalculateFireballScale(int count)
    {
        return 2.5f;
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
            }
        }

        Vector3 playerPos = owner.transform.position;
        float effectiveSpeed = rotationSpeed + bonusSpeed;
        int activeCount = fireballs.Count;
        float scale = CalculateFireballScale(activeCount);

        int ringCount = RingCapacity.Length;
        int[] ringStart = new int[ringCount];
        int[] ringCounts = new int[ringCount];
        int cumulative = 0;
        for (int r = 0; r < ringCount; r++)
        {
            ringStart[r] = cumulative;
            int inThisRing = Mathf.Min(Mathf.Max(activeCount - cumulative, 0), RingCapacity[r]);
            ringCounts[r] = inThisRing;
            cumulative += RingCapacity[r];
        }

        for (int i = 0; i < activeCount; i++)
        {
            int ring = 0;
            for (int r = 0; r < ringCount; r++)
            {
                if (i < ringStart[r] + ringCounts[r]) { ring = r; break; }
            }
            int indexInRing = i - ringStart[ring];
            int countInRing = ringCounts[ring];

            float ringRadius = orbitRadius * (1f + ring * 1.0f);
            float angleStep = 360f / countInRing;

            fireballs[i].transform.localScale = Vector3.one * scale;
            float angle = (Time.time * effectiveSpeed + angleStep * indexInRing) % 360f;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Cos(rad) * ringRadius * 0.75f,
                0.8f,
                Mathf.Sin(rad) * ringRadius * 0.75f
            );
            fireballs[i].transform.position = playerPos + offset;
        }
    }

    public void SpawnFireball(float lifetime = -1f)
    {
        if (disposed || owner == null || fireballs.Count >= MaxFireballs) return;

        GameObject obj;
        Renderer renderer = null;

        if (fireballVFX != null)
        {
            obj = GameObject.Instantiate(fireballVFX);
            obj.transform.localScale = Vector3.one * CalculateFireballScale(fireballs.Count + 1);

            if (obj.GetComponent<SphereCollider>() == null)
            {
                var col = obj.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 2.0f;
            }
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.transform.localScale = Vector3.one * CalculateFireballScale(fireballs.Count + 1);

            var col = obj.GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 2.0f;

            renderer = obj.GetComponent<Renderer>();
        }

        if (obj.GetComponent<Rigidbody>() == null)
        {
            var rb = obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        var script = obj.AddComponent<Fireball>();
        script.Initialize(owner, baseDamage + bonusDamage, lifetime, renderer);
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
    public bool IsPermanent => lifetime <= 0f;

    public void SetDamage(float dmg) => damage = dmg;

    public void Initialize(Entity player, float dmg, float life, Renderer renderer = null)
    {
        playerEntity = player;
        damage = dmg;
        lifetime = life;

        if (renderer != null)
        {
            if (cachedShader == null)
                cachedShader = Shader.Find("Sprites/Default");

            fireballMaterial = new Material(cachedShader);
            fireballMaterial.color = new Color(1f, 0.3f, 0f, 0.8f);
            renderer.material = fireballMaterial;
        }
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
