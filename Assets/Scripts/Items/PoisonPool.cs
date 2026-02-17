using System.Collections.Generic;
using UnityEngine;

public class PoisonPool : MonoBehaviour
{
    [SerializeField] private float radius = 4f;
    [SerializeField] private float lifetime = 4f;

    private float nextTickTime;
    private float endTime;
    private Entity owner;
    private bool electrified;
    private float electrifiedUntil;
    private float electrifyDamage;
    private float nextArcTime;
    private SphereCollider trigger;
    private ParticleSystem bubbleSystem;

    private const float ArcInterval = 0.2f;
    private const int ArcCount = 3;
    private const float ArcHeight = 0.2f;
    public void Initialize(Entity owner, float radius, float lifetime)
    {
        this.owner = owner;
        this.radius = radius;
        this.lifetime = lifetime;

        var core = PoisonCore.Active;
        float interval = core != null ? core.TickInterval : 1f;
        nextTickTime = Time.time + interval;
        endTime = Time.time + lifetime;
        SetupTrigger();
        BuildVfx();

        if (core != null && core.HasData)
        {
            ApplyPoison(core);
        }
    }

    private void Update()
    {
        if (Time.time >= endTime)
        {
            Destroy(gameObject);
            return;
        }

        var core = PoisonCore.Active;
        if (core == null || !core.HasData) return;

        if (Time.time >= nextTickTime)
        {
            ApplyPoison(core);
            nextTickTime = Time.time + core.TickInterval;
        }

        if (electrified)
        {
            if (Time.time >= electrifiedUntil)
            {
                SetElectrified(false);
            }
            else if (Time.time >= nextArcTime)
            {
                SpawnElectricArcs();
                nextArcTime = Time.time + ArcInterval;
            }
        }
    }

    private void ApplyPoison(PoisonCore core)
    {
        if (owner == null) return;

        Vector3 center = transform.position;
        Collider[] hits = Physics.OverlapSphere(center, radius);
        HashSet<Enemy> unique = new HashSet<Enemy>();

        foreach (var col in hits)
        {
            var enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null || unique.Contains(enemy)) continue;
            unique.Add(enemy);
            core.ApplyTo(enemy, owner, true);
            if (electrified && electrifyDamage > 0f)
            {
                LightningCore.ApplyLightningDamage(owner, enemy, electrifyDamage);
            }
        }

    }

    private void BuildVfx()
    {
        var baseObj = new GameObject("PoisonPoolVFX");
        baseObj.transform.SetParent(transform);
        baseObj.transform.localPosition = Vector3.zero;
        baseObj.transform.localRotation = Quaternion.identity;

        CreatePoolMesh(baseObj.transform, radius * 2f, new Color(0.05f, 0.25f, 0.08f, 1f), 0.15f, 0f);
        CreatePoolMesh(baseObj.transform, radius * 1.6f, new Color(0.04f, 0.2f, 0.06f, 0.95f), 0.2f, 25f);

        CreateBubbles(baseObj.transform);
    }

    private void CreatePoolMesh(Transform parent, float size, Color color, float jitter, float rotation)
    {
        var obj = new GameObject("PoolMesh");
        obj.transform.SetParent(parent);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        var meshFilter = obj.AddComponent<MeshFilter>();
        var meshRenderer = obj.AddComponent<MeshRenderer>();

        meshFilter.mesh = BuildTerrainAdaptiveMesh(size * 0.5f, jitter, 24, rotation);

        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = color;
        meshRenderer.material = mat;

    }

    private Mesh BuildTerrainAdaptiveMesh(float radius, float jitter, int segments, float rotation)
    {
        Mesh mesh = new Mesh();
        int rings = 2;
        int totalVerts = 1 + segments * rings;
        Vector3[] verts = new Vector3[totalVerts];
        int triCount = segments * 3 + (rings - 1) * segments * 6;
        int[] tris = new int[triCount];

        Vector3 center = transform.position;
        verts[0] = GetGroundOffset(center);
        
        float angleStep = Mathf.PI * 2f / segments;
        float rotRad = rotation * Mathf.Deg2Rad;
        int vertIndex = 1;

        for (int ring = 1; ring <= rings; ring++)
        {
            float ringRadius = (radius / rings) * ring;
            bool isOuterRing = (ring == rings);
            
            for (int i = 0; i < segments; i++)
            {
                float angle = angleStep * i + rotRad;
                float r = ringRadius * (1f + Random.Range(-jitter, jitter));
                Vector3 worldPos = center + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
                
                if (isOuterRing || i % 4 == 0)
                {
                    verts[vertIndex] = GetGroundOffset(worldPos);
                }
                else
                {
                    int prevKey = vertIndex - (i % 4);
                    int nextKey = (i % 4 == 3) ? vertIndex - 3 : vertIndex + (4 - i % 4);
                    if (nextKey >= totalVerts) nextKey = vertIndex;
                    float t = (i % 4) / 4f;
                    verts[vertIndex] = Vector3.Lerp(verts[prevKey], verts[0], t);
                }
                vertIndex++;
            }
        }

        int triIndex = 0;
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            tris[triIndex++] = 0;
            tris[triIndex++] = 1 + next;
            tris[triIndex++] = 1 + i;
        }

        for (int ring = 0; ring < rings - 1; ring++)
        {
            int currentRingStart = 1 + ring * segments;
            int nextRingStart = 1 + (ring + 1) * segments;
            
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                
                tris[triIndex++] = currentRingStart + i;
                tris[triIndex++] = nextRingStart + i;
                tris[triIndex++] = currentRingStart + next;
                
                tris[triIndex++] = currentRingStart + next;
                tris[triIndex++] = nextRingStart + i;
                tris[triIndex++] = nextRingStart + next;
            }
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private Vector3 GetGroundOffset(Vector3 worldPos)
    {
        Vector3 rayStart = worldPos + Vector3.up * 2f;
        Vector3 rayEnd = worldPos + Vector3.down * 5f;
        float rayDistance = 7f;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            Vector3 localPos = transform.InverseTransformPoint(hit.point + Vector3.up * 0.5f);
            return localPos;
        }

        return transform.InverseTransformPoint(worldPos);
    }

    private void CreateBubbles(Transform parent)
    {
        var obj = new GameObject("PoisonBubbles");
        obj.transform.SetParent(parent);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        bubbleSystem = obj.AddComponent<ParticleSystem>();
        var main = bubbleSystem.main;
        main.startLifetime = 1.2f;
        main.startSpeed = 0.4f;
        main.startSize = 0.08f;
        main.startColor = new Color(0.1f, 0.6f, 0.15f, 0.9f);
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 80;

        var emission = bubbleSystem.emission;
        emission.rateOverTime = 12f;

        var shape = bubbleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius * 0.5f;
        
        var colorOverLifetime = bubbleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(new Color(0.05f, 0.35f, 0.1f), 0f),
                new GradientColorKey(new Color(0.08f, 0.4f, 0.15f), 1f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0.9f, 0f), 
                new GradientAlphaKey(0f, 1f) 
            }
        );
        colorOverLifetime.color = gradient;

        var renderer = bubbleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 1;
    }

    private void SetupTrigger()
    {
        trigger = GetComponent<SphereCollider>();
        if (trigger == null) trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = radius;
        trigger.center = Vector3.zero;
    }

    public void Electrify(float damage)
    {
        electrifyDamage = Mathf.Max(electrifyDamage, damage);
        electrifiedUntil = endTime;
        if (!electrified)
        {
            SetElectrified(true);
        }
    }

    private void SetElectrified(bool value)
    {
        electrified = value;
        nextArcTime = Time.time;

        if (bubbleSystem != null)
        {
            var emission = bubbleSystem.emission;
            emission.rateOverTime = value ? 18f : 12f;
        }
    }

    private void SpawnElectricArcs()
    {
        Vector3 center = transform.position;
        for (int i = 0; i < ArcCount; i++)
        {
            Vector2 rnd = Random.insideUnitCircle * radius;
            Vector3 start = center + new Vector3(rnd.x, ArcHeight, rnd.y);
            Vector3 end = center + new Vector3(-rnd.y, ArcHeight, rnd.x);

            GameObject startObj = new GameObject("PoolArcStart");
            startObj.transform.position = start;

            GameObject endObj = new GameObject("PoolArcEnd");
            endObj.transform.position = end;

            LightningCore.CreateLightningVFX(startObj.transform, endObj.transform, radius, 0.2f, null, 0f, 0f, 0.1f, true);
            Destroy(startObj, 0.3f);
            Destroy(endObj, 0.3f);
        }
    }
}

