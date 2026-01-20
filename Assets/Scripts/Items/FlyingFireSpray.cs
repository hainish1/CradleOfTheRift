using System;
using System.Collections.Generic;
using UnityEngine;

public class FlyingFire : IDisposable
{
    private readonly Entity owner;
    private readonly PlayerMovement movement;
    private readonly float baseDamage;
    private readonly float radius;
    private readonly float tickInterval;
    private readonly float offset;
    private readonly LayerMask enemyLayer;
    private int stacks;
    private float duration;
    private float timer;
    private float nextTickTime;
    private bool disposed;
    private GameObject vfxObj;
    private Transform vfxTransform;
    private Transform outerFlame;
    private Transform innerFlame;
    private MeshRenderer outerRenderer;
    private MeshRenderer innerRenderer;
    private float flickerSeed;

    public FlyingFire(Entity owner, float damage, float radius, float tickInterval, float offset, int initialStacks = 1, float durationSec = -1f)
    {
        this.owner = owner;
        this.baseDamage = damage;
        this.radius = radius;
        this.tickInterval = Mathf.Max(0.05f, tickInterval);
        this.offset = offset;
        this.stacks = initialStacks > 0 ? initialStacks : 1;
        this.duration = durationSec;
        this.timer = durationSec;
        this.nextTickTime = Time.time + this.tickInterval;

        movement = owner != null ? owner.GetComponent<PlayerMovement>() : null;
        enemyLayer = LayerMask.GetMask("Enemy");
    }

    public void AddStack(int count = 1)
    {
        stacks += count;
        if (stacks <= 0) Dispose();
    }

    public void Update(float dt)
    {
        if (disposed || owner == null) return;

        if (duration > 0f)
        {
            timer -= dt;
            if (timer <= 0f)
            {
                Dispose();
                return;
            }
        }

        if (movement == null || !movement.IsFlying)
        {
            UpdateVfx(false);
            return;
        }
        UpdateVfx(true);
        if (Time.time < nextTickTime) return;

        ApplyFire();
        nextTickTime = Time.time + tickInterval;
    }

    private void ApplyFire()
    {
        float damage = baseDamage * stacks;
        Vector3 origin = owner.transform.position + Vector3.down * offset;
        float height = GetGroundHeight(origin);
        float searchRadius = Mathf.Max(height, radius);
        Collider[] hits = Physics.OverlapSphere(origin, searchRadius, enemyLayer);
        HashSet<Enemy> unique = new HashSet<Enemy>();

        foreach (var col in hits)
        {
            var enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null || unique.Contains(enemy)) continue;
            unique.Add(enemy);

            var damageable = enemy.GetComponent<IDamageable>();
            if (damageable == null || damageable.IsDead) continue;
            if (!IsInsideCone(origin, enemy.transform.position, height)) continue;
            damageable.TakeDamage(damage);
            CombatEvents.ReportDamage(owner, enemy, damage, ElementType.Fire);
        }
    }

    private bool IsInsideCone(Vector3 origin, Vector3 targetPos, float height)
    {
        if (height <= 0.01f) return false;
        Vector3 toTarget = targetPos - origin;
        float t = Vector3.Dot(toTarget, Vector3.down);
        if (t <= 0f || t > height) return false;
        float radial = (toTarget - Vector3.down * t).magnitude;
        float allowed = (t / height) * radius;
        return radial <= allowed;
    }

    private float GetGroundHeight(Vector3 origin)
    {
        Collider[] ownerCols = owner != null ? owner.GetComponentsInChildren<Collider>() : null;
        Vector3 start = origin + Vector3.up * 1f;
        Vector3 end = origin + Vector3.down * 50f;
        Vector3 dir = (end - start).normalized;
        float distance = Vector3.Distance(start, end);

        RaycastHit[] hits = Physics.RaycastAll(start, dir, distance, ~0, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0) return 5f;

        RaycastHit best = hits[0];
        float bestDist = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            if (IsOwnerCollider(hits[i].collider, ownerCols)) continue;
            if (hits[i].normal.y < 0.2f) continue;
            if (hits[i].distance < bestDist)
            {
                bestDist = hits[i].distance;
                best = hits[i];
            }
        }

        if (bestDist == float.MaxValue) return 5f;
        return Mathf.Max(0.5f, best.distance);
    }

    private bool IsOwnerCollider(Collider col, Collider[] ownerCols)
    {
        if (col == null || ownerCols == null) return false;
        for (int i = 0; i < ownerCols.Length; i++)
        {
            if (ownerCols[i] == col) return true;
        }
        return false;
    }

    private void UpdateVfx(bool show)
    {
        if (!show)
        {
            if (vfxObj != null) vfxObj.SetActive(false);
            return;
        }

        if (vfxObj == null)
        {
            vfxObj = new GameObject("FlyingFireVFX");
            vfxTransform = vfxObj.transform;
            vfxTransform.SetParent(owner.transform);
            vfxTransform.localRotation = Quaternion.Euler(180f, 0f, 0f);
            vfxTransform.localScale = Vector3.one;

            flickerSeed = UnityEngine.Random.Range(0f, 10f);

            outerFlame = CreateFlame("FlameOuter", new Color(1f, 0.35f, 0.1f, 0.75f), out outerRenderer);
            innerFlame = CreateFlame("FlameInner", new Color(1f, 0.75f, 0.2f, 0.85f), out innerRenderer);
        }

        vfxObj.SetActive(true);
        Vector3 origin = owner.transform.position + Vector3.down * offset;
        float height = GetGroundHeight(origin);
        vfxTransform.position = origin;

        float flicker = 0.85f + Mathf.Abs(Mathf.Sin(Time.time * 12f + flickerSeed)) * 0.2f;
        Vector3 outerScale = new Vector3(radius * 1.1f, height, radius * 1.1f) * flicker;
        Vector3 innerScale = new Vector3(radius * 0.7f, height * 0.9f, radius * 0.7f) * flicker;

        if (outerFlame != null) outerFlame.localScale = outerScale;
        if (innerFlame != null) innerFlame.localScale = innerScale;
        if (outerRenderer != null) outerRenderer.material.color = new Color(1f, 0.35f, 0.1f, 0.75f * flicker);
        if (innerRenderer != null) innerRenderer.material.color = new Color(1f, 0.75f, 0.2f, 0.85f * flicker);
    }

    private Transform CreateFlame(string name, Color color, out MeshRenderer renderer)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(vfxTransform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        var filter = obj.AddComponent<MeshFilter>();
        renderer = obj.AddComponent<MeshRenderer>();
        filter.mesh = BuildConeMesh(18);

        var mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = color;
        renderer.material = mat;

        return obj.transform;
    }

    private Mesh BuildConeMesh(int segments)
    {
        Mesh mesh = new Mesh();
        Vector3[] verts = new Vector3[segments + 1];
        int[] tris = new int[segments * 3];

        verts[0] = Vector3.zero;
        float angleStep = Mathf.PI * 2f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            verts[i + 1] = new Vector3(Mathf.Cos(angle), 1f, Mathf.Sin(angle));
        }

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            int tri = i * 3;
            tris[tri] = 0;
            tris[tri + 1] = i + 1;
            tris[tri + 2] = next + 1;
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (vfxObj != null) UnityEngine.Object.Destroy(vfxObj);
    }
}
