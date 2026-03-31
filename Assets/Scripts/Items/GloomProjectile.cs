using UnityEngine;

public class GloomProjectile : Projectile
{
    void Start()
    {
        if (GloomUpgrade.IsEnabled)
        {
            var owner = GloomUpgrade.Owner;
            if (owner != null)
            {
                Vector3 groundPos = FindGroundBelowPlayer(owner.transform.position);
                SpawnGloomPool(groundPos);
            }
            hasHit = true;
            ReturnToSource();
        }
    }

    private Vector3 FindGroundBelowPlayer(Vector3 origin)
    {
        if (Physics.Raycast(origin + Vector3.up, Vector3.down, out RaycastHit hit, 10f, ~0, QueryTriggerInteraction.Ignore))
            return hit.point + Vector3.up * 0.05f;
        return origin;
    }

    private void EnableGravity()
    {
        if (rb != null)
        {
            rb.useGravity = true;
            rb.linearDamping = 0f;
        }
    }

    public override void Update()
    {
        base.Update();
        if (GloomUpgrade.IsEnabled && rb != null && !hasHit)
            rb.AddForce(Vector3.down * 15f, ForceMode.Acceleration);
    }

    private void HideOriginalProjectileModel()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (r != null)
                r.enabled = false;
        }
    }

    public override void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        if (((1 << collision.gameObject.layer) & hitMask) == 0) return;

        if (!GloomUpgrade.IsEnabled)
        {
            base.OnCollisionEnter(collision);
            return;
        }

        var contact = collision.GetContact(0);
        var hitEnemy = collision.collider.GetComponentInParent<Enemy>();
        Vector3 origin = contact.point;

        if (hitEnemy != null)
            origin = hitEnemy.transform.position + Vector3.up * 0.5f;

        Collider[] excludeFromRaycast = hitEnemy != null
            ? hitEnemy.GetComponentsInChildren<Collider>()
            : null;
        Vector3 spawnPos = GetGroundSpawnPoint(origin, excludeFromRaycast);
        if (spawnPos.y <= -9998f)
        {
            spawnPos = contact.point;
            spawnPos.y -= 0.1f;
        }

        hasHit = true;
        SpawnGloomPool(spawnPos);

        CreateImpactFX();
        ReturnToSource();
    }

    private Vector3 GetGroundSpawnPoint(Vector3 origin, Collider[] excludeFromRaycast)
    {
        Collider myCol = GetComponent<Collider>();

        Vector3 start = origin + Vector3.up * 2f;
        Vector3 dir = Vector3.down;
        float distance = 50f;

        RaycastHit[] hits = Physics.RaycastAll(start, dir, distance, ~0, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0) return new Vector3(0f, -9999f, 0f);

        RaycastHit best = default;
        float bestDist = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == myCol) continue;
            if (excludeFromRaycast != null && IsExcludedCollider(hits[i].collider, excludeFromRaycast)) continue;
            if (hits[i].normal.y < 0.2f) continue;
            if (hits[i].distance < bestDist)
            {
                bestDist = hits[i].distance;
                best = hits[i];
            }
        }

        if (bestDist >= float.MaxValue) return new Vector3(0f, -9999f, 0f);
        return best.point + Vector3.up * 0.02f;
    }

    private bool IsExcludedCollider(Collider col, Collider[] excludeCols)
    {
        if (col == null || excludeCols == null) return false;
        for (int i = 0; i < excludeCols.Length; i++)
        {
            if (excludeCols[i] != null && excludeCols[i] == col) return true;
        }
        return false;
    }

    private void SpawnGloomPool(Vector3 position)
    {
        var poolPrefab = GloomUpgrade.PoolPrefab;
        var owner = GloomUpgrade.Owner;
        if (poolPrefab == null || owner == null) return;

        var obj = Instantiate(poolPrefab, position, poolPrefab.transform.rotation);
        var pool = obj.GetComponent<GloomPool>();
        if (pool == null) pool = obj.AddComponent<GloomPool>();

        pool.Initialize(
            owner,
            GloomUpgrade.Damage,
            GloomUpgrade.TickInterval,
            GloomUpgrade.Radius,
            GloomUpgrade.PoolLifetime,
            GloomUpgrade.AttackSpeedBuff
        );
    }
}
