using UnityEngine;

public class PoisonPoolOnDash : System.IDisposable
{
    private readonly Entity owner;
    private readonly PlayerMovement movement;
    private readonly float baseRadius;
    private readonly float poolLifetime;
    private int stacks;
    private readonly float lifetime;
    private float lifeTimer;
    private bool disposed;

    public PoisonPoolOnDash(Entity owner, float radius, float poolLifetime, int initialStacks, float durationSec = -1f)
    {
        this.owner = owner;
        this.baseRadius = radius;
        this.poolLifetime = poolLifetime;
        stacks = Mathf.Max(1, initialStacks);
        lifetime = durationSec;
        lifeTimer = durationSec;

        movement = owner != null ? owner.GetComponent<PlayerMovement>() : null;
        if (movement != null)
        {
            movement.DashCooldownStarted += OnDashStarted;
        }
    }

    public void AddStack(int count = 1)
    {
        stacks += count;
        if (stacks <= 0)
        {
            Dispose();
        }
    }

    public void Update(float dt)
    {
        if (lifetime < 0f || disposed) return;
        lifeTimer -= dt;
        if (lifeTimer <= 0f) Dispose();
    }

    private void OnDashStarted(float dashDuration)
    {
        if (disposed || owner == null) return;

        Vector3 spawnPos = GetSpawnPoint(owner.transform.position);
        float radius = baseRadius * (1f + 0.1f * (stacks - 1));

        var obj = new GameObject("PoisonPool");
        obj.transform.position = spawnPos;

        var pool = obj.AddComponent<PoisonPool>();
        pool.Initialize(owner, radius, poolLifetime);
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
        Vector3 end = origin + Vector3.down * 50f;
        Vector3 dir = (end - start).normalized;
        float distance = Vector3.Distance(start, end);

        RaycastHit[] hits = Physics.RaycastAll(start, dir, distance, ~0, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0) return new Vector3(0f, -9999f, 0f);

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

        if (bestDist == float.MaxValue) return new Vector3(0f, -9999f, 0f);
        return best.point + Vector3.up * 0.02f;
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

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (movement != null)
        {
            movement.DashCooldownStarted -= OnDashStarted;
        }
    }
}

