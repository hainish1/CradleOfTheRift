using System;
using System.Collections;
using UnityEngine;

public class FlameTrail : IDisposable
{
    private readonly Entity owner;
    private readonly PlayerMovement movement;
    private readonly float damage;
    private readonly float radius;
    private readonly float poolLifetime;
    private readonly float spawnInterval;
    private readonly float effectLifetime;
    private float lifeTimer;
    private bool disposed;
    private Coroutine activeCoroutine;

    public FlameTrail(Entity owner, float damage, float radius, float poolLifetime, float spawnInterval, float durationSec = -1f)
    {
        this.owner = owner;
        this.damage = damage;
        this.radius = Mathf.Max(0.5f, radius);
        this.poolLifetime = Mathf.Max(0.2f, poolLifetime);
        this.spawnInterval = Mathf.Max(0.05f, spawnInterval);
        this.effectLifetime = durationSec;
        this.lifeTimer = durationSec;

        movement = owner != null ? (owner.GetComponent<PlayerMovement>() ?? owner.GetComponentInParent<PlayerMovement>() ?? owner.GetComponentInChildren<PlayerMovement>()) : null;
        if (movement != null)
        {
            movement.DashCooldownStarted += OnDashStarted;
        }
    }

    public bool IsDisposed => disposed;

    public void Update(float dt)
    {
        if (effectLifetime < 0f || disposed) return;
        lifeTimer -= dt;
        if (lifeTimer <= 0f) Dispose();
    }

    private void OnDashStarted(float dashDuration)
    {
        if (disposed || owner == null || movement == null) return;

        if (activeCoroutine != null)
        {
            movement.StopCoroutine(activeCoroutine);
        }

        activeCoroutine = movement.StartCoroutine(SpawnPoolsAlongDash(dashDuration));
    }

    private IEnumerator SpawnPoolsAlongDash(float dashDuration)
    {
        float elapsed = 0f;
        float nextSpawn = 0f;

        while (elapsed < dashDuration && !disposed && owner != null)
        {
            if (elapsed >= nextSpawn)
            {
                SpawnFirePool();
                nextSpawn += spawnInterval;
            }

            yield return null;
            elapsed += Time.deltaTime;
        }

        activeCoroutine = null;
    }

    private void SpawnFirePool()
    {
        Vector3 pos = GetSpawnPoint(owner.transform.position);

        var obj = new GameObject("FirePool");
        obj.transform.position = pos;

        var pool = obj.AddComponent<FirePool>();
        pool.Initialize(owner, damage, radius, poolLifetime);
    }

    private Vector3 GetSpawnPoint(Vector3 origin)
    {
        Vector3 groundPoint = GetGroundPoint(origin);
        if (groundPoint.y > -9999f) return groundPoint;

        var controller = owner.GetComponent<CharacterController>();
        if (controller != null)
        {
            Vector3 p = origin;
            p.y = controller.bounds.min.y + 0.02f;
            return p;
        }
        origin.y -= 0.1f;
        return origin;
    }

    private Vector3 GetGroundPoint(Vector3 origin)
    {
        Collider[] ownerCols = owner != null ? owner.GetComponentsInChildren<Collider>() : null;
        Vector3 start = origin + Vector3.up * 2f;
        Vector3 dir = Vector3.down;
        float distance = 50f;

        if (!Physics.Raycast(start, dir, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
            return new Vector3(0f, -9999f, 0f);

        if (ownerCols != null)
        {
            for (int i = 0; i < ownerCols.Length; i++)
            {
                if (ownerCols[i] == hit.collider) return new Vector3(0f, -9999f, 0f);
            }
        }
        if (hit.normal.y < 0.2f) return new Vector3(0f, -9999f, 0f);

        return hit.point + Vector3.up * 0.02f;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (movement != null)
        {
            if (activeCoroutine != null)
            {
                movement.StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }
            movement.DashCooldownStarted -= OnDashStarted;
        }
    }
}
