using UnityEngine;

public class PoisonPoolBottleProjectile : Projectile
{
    private GameObject bottleVisual;

    void Start()
    {
        if (PoisonPoolProjectiles.IsEnabled)
        {
            EnableGravity();
            CreateBottleVisual();
        }
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
        if (PoisonPoolProjectiles.IsEnabled && rb != null && !hasHit)
            rb.AddForce(Vector3.down * 15f, ForceMode.Acceleration);
    }

    private void CreateBottleVisual()
    {
        HideOriginalProjectileModel();

        var vfxPrefab = PoisonPoolProjectiles.BottleVFX;
        if (vfxPrefab != null)
        {
            bottleVisual = Instantiate(vfxPrefab, transform);
            bottleVisual.name = "PoisonBottleVFX";
            bottleVisual.transform.localPosition = Vector3.zero;
            bottleVisual.transform.localRotation = Quaternion.identity;
            bottleVisual.transform.localScale = Vector3.one;
        }
        else
        {
            bottleVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bottleVisual.name = "PoisonBottleVisual";
            bottleVisual.transform.SetParent(transform);
            bottleVisual.transform.localPosition = Vector3.zero;
            bottleVisual.transform.localScale = Vector3.one * 0.5f;

            var col = bottleVisual.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(0.1f, 0.5f, 0.15f, 0.9f);
            bottleVisual.GetComponent<Renderer>().material = mat;
        }
    }

    private void HideOriginalProjectileModel()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (r != null && r.gameObject != bottleVisual)
                r.enabled = false;
        }
    }

    public override void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        if (((1 << collision.gameObject.layer) & hitMask) == 0) return;

        if (!PoisonPoolProjectiles.IsEnabled)
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
        SpawnPoisonPool(spawnPos);

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

    private void SpawnPoisonPool(Vector3 position)
    {
        var owner = PoisonPoolProjectiles.Owner;
        if (owner == null) return;

        var obj = new GameObject("PoisonPool");
        obj.transform.position = position;

        var pool = obj.AddComponent<PoisonPool>();
        pool.Initialize(owner, PoisonPoolProjectiles.Radius, PoisonPoolProjectiles.PoolLifetime);
    }
}
