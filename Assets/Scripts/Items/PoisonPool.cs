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

        CreatePoolMesh(baseObj.transform, radius, new Color(0.25f, 0.05f, 0.3f, 1f));
        CreatePoolMesh(baseObj.transform, radius * 0.8f, new Color(0.2f, 0.04f, 0.25f, 0.95f));

        CreateBubbles(baseObj.transform);
    }

    private void CreatePoolMesh(Transform parent, float meshRadius, Color color)
    {
        var obj = new GameObject("PoolMesh");
        obj.transform.SetParent(parent);
        obj.transform.localPosition = Vector3.up * 0.02f;
        obj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        obj.transform.localScale = Vector3.one;

        var meshFilter = obj.AddComponent<MeshFilter>();
        var meshRenderer = obj.AddComponent<MeshRenderer>();

        meshFilter.mesh = BuildCircleMesh(meshRadius, 32);

        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = color;
        meshRenderer.material = mat;
    }

    private Mesh BuildCircleMesh(float r, int segments)
    {
        Mesh mesh = new Mesh();
        int vertCount = segments + 1;
        Vector3[] verts = new Vector3[vertCount];
        int[] tris = new int[segments * 3];

        verts[0] = Vector3.zero;
        float angleStep = Mathf.PI * 2f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = angleStep * i;
            verts[i + 1] = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f);
        }

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = next + 1;
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
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
        main.startColor = new Color(0.5f, 0.1f, 0.6f, 0.9f);
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
                new GradientColorKey(new Color(0.3f, 0.05f, 0.35f), 0f),
                new GradientColorKey(new Color(0.35f, 0.08f, 0.4f), 1f)
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

