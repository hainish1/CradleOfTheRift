using System;
using UnityEngine;

public class PoisonCloudEffect : IDisposable
{
    private const float GroundSnapMaxVerticalGap = 14f;

    private readonly Entity owner;
    private readonly float damagePerTick;
    private readonly float damageTickInterval;
    private readonly float cloudRadius;
    private readonly float cloudLifetime;
    private readonly float trailSpacing;
    private readonly float behindDistance;
    private readonly GameObject vfxPrefab;
    private readonly float effectDurationSec;

    private readonly RaycastHit[] groundHitBuffer = new RaycastHit[32];
    private Collider[] ownerColliders;

    private Vector3 lastSpawnPosition;
    private bool hasSpawnedOnce;
    private float lifeTimer;
    private bool disposed;

    public bool IsDisposed => disposed;

    private static float TrailSpacingForRadius(float radius)
    {
        return Mathf.Clamp(radius * 0.25f, 0.35f, 2.5f);
    }

    public PoisonCloudEffect(
        Entity owner,
        float damagePerTick,
        float damageTickInterval,
        float cloudRadius,
        float cloudLifetime,
        float behindDistance,
        GameObject vfxPrefab,
        float durationSec = -1f)
    {
        this.owner = owner;
        this.damagePerTick = Mathf.Max(0f, damagePerTick);
        this.damageTickInterval = Mathf.Max(0.1f, damageTickInterval);
        this.cloudRadius = Mathf.Max(0.25f, cloudRadius);
        this.cloudLifetime = Mathf.Max(0.2f, cloudLifetime);
        this.trailSpacing = TrailSpacingForRadius(this.cloudRadius);
        this.behindDistance = Mathf.Max(0f, behindDistance);
        this.vfxPrefab = vfxPrefab;
        this.effectDurationSec = durationSec;

        lifeTimer = durationSec;
        if (owner != null)
            ownerColliders = owner.GetComponentsInChildren<Collider>();
    }

    public void Update(float dt)
    {
        if (disposed || owner == null) return;

        if (effectDurationSec >= 0f)
        {
            lifeTimer -= dt;
            if (lifeTimer <= 0f)
            {
                Dispose();
                return;
            }
        }

        TrySpawnAlongTrail();
    }

    private void TrySpawnAlongTrail()
    {
        Vector3 flatFwd = owner.transform.forward;
        flatFwd.y = 0f;
        if (flatFwd.sqrMagnitude < 1e-6f)
            flatFwd = Vector3.forward;
        else
            flatFwd.Normalize();

        Vector3 trailOrigin = owner.transform.position - flatFwd * behindDistance;
        Vector3 pos = GetSpawnPoint(trailOrigin);

        if (!hasSpawnedOnce)
        {
            SpawnCloud(pos);
            hasSpawnedOnce = true;
            lastSpawnPosition = pos;
            return;
        }

        if (Vector3.Distance(pos, lastSpawnPosition) < trailSpacing)
            return;

        SpawnCloud(pos);
        lastSpawnPosition = pos;
    }

    private void SpawnCloud(Vector3 worldPos)
    {
        var obj = new GameObject("PoisonCloud");
        obj.transform.position = worldPos;
        var cloud = obj.AddComponent<PoisonCloud>();
        cloud.Initialize(owner, damagePerTick, cloudRadius, cloudLifetime, damageTickInterval, vfxPrefab);
    }

    private Vector3 GetSpawnPoint(Vector3 trailOrigin)
    {
        float playerY = owner.transform.position.y;

        if (TryFindGroundBelow(trailOrigin, playerY, out Vector3 groundPos, out float groundY))
        {
            if (playerY - groundY <= GroundSnapMaxVerticalGap)
                return groundPos;
        }

        return new Vector3(trailOrigin.x, playerY, trailOrigin.z);
    }

    private bool TryFindGroundBelow(Vector3 xzRef, float playerY, out Vector3 groundWorld, out float groundY)
    {
        groundWorld = default;
        groundY = float.NegativeInfinity;

        float startY = Mathf.Max(playerY, xzRef.y) + 6f;
        Vector3 start = new Vector3(xzRef.x, startY, xzRef.z);
        float maxDist = Mathf.Max(320f, startY + 120f);

        int n = Physics.RaycastNonAlloc(start, Vector3.down, groundHitBuffer, maxDist, ~0, QueryTriggerInteraction.Ignore);
        if (n <= 0) return false;

        int best = -1;
        float bestDist = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            RaycastHit h = groundHitBuffer[i];
            if (IsOwnerCollider(h.collider)) continue;
            if (h.normal.y < 0.25f) continue;
            if (h.distance < bestDist)
            {
                bestDist = h.distance;
                best = i;
            }
        }

        if (best < 0) return false;

        groundY = groundHitBuffer[best].point.y;
        groundWorld = groundHitBuffer[best].point + Vector3.up * 0.02f;
        return true;
    }

    private bool IsOwnerCollider(Collider c)
    {
        if (c == null || ownerColliders == null) return false;
        for (int i = 0; i < ownerColliders.Length; i++)
        {
            if (ownerColliders[i] == c) return true;
        }
        return false;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
    }
}
