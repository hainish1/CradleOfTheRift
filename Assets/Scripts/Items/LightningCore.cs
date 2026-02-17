using System.Collections.Generic;
using UnityEngine;

public class LightningVFX : MonoBehaviour
{
    private static Shader cachedShader;
    private LineRenderer lr;
    private Material lightningMaterial;
    private Transform startTarget;
    private Transform endTarget;
    private float range;
    private float noiseSeed;
    private float lifetime;
    private float elapsedTime;
    private float startHeight;
    private float endHeight;
    private float extendDuration;
    private bool isExtending;
    private bool useArcBend;
    private Vector3[] baseArcPoints = new Vector3[16];
    
    public void Initialize(Transform start, Transform end, float range, float duration, Color lightningColor, float startHeightOffset = 0.5f, float endHeightOffset = 0.5f, float extendTime = 0.2f, bool enableArcBend = false)
    {
        startTarget = start;
        endTarget = end;
        this.range = range;
        this.lifetime = duration;
        this.noiseSeed = Time.time * 10f + UnityEngine.Random.Range(0f, 100f);
        this.elapsedTime = 0f;
        this.startHeight = startHeightOffset;
        this.endHeight = endHeightOffset;
        this.extendDuration = extendTime;
        this.isExtending = extendTime > 0f;
        this.useArcBend = enableArcBend;
        
        lr = gameObject.AddComponent<LineRenderer>();
        
        if (cachedShader == null)
            cachedShader = Shader.Find("Sprites/Default");
        
        lightningMaterial = new Material(cachedShader);
        lr.material = lightningMaterial;
        lr.startColor = lightningColor;
        lr.endColor = new Color(lightningColor.r, lightningColor.g, lightningColor.b, 0.9f);
        lr.startWidth = 0.4f;
        lr.endWidth = 0.2f;
        lr.positionCount = isExtending ? 1 : 16;
        lr.useWorldSpace = true;
    }
    
    void Update()
    {
        if (startTarget == null || endTarget == null)
        {
            Destroy(gameObject);
            return;
        }
        
        elapsedTime += Time.deltaTime;
        if (elapsedTime >= lifetime)
        {
            Destroy(gameObject);
            return;
        }
        
        if (isExtending && elapsedTime < extendDuration)
        {
            float extendProgress = elapsedTime / extendDuration;
            int visiblePoints = Mathf.CeilToInt(16 * extendProgress);
            lr.positionCount = Mathf.Clamp(visiblePoints, 1, 16);
        }
        else if (isExtending && elapsedTime >= extendDuration)
        {
            lr.positionCount = 16;
            isExtending = false;
        }
        
        Vector3 start = startTarget.position + Vector3.up * startHeight;
        Vector3 end = endTarget.position + Vector3.up * endHeight;
        
        UpdateArcPath(start, end);
    }
    
    private void UpdateArcPath(Vector3 start, Vector3 end)
    {
        Vector3 dir = (end - start).normalized;
        Vector3 right = Vector3.Cross(dir, Vector3.up).normalized;
        if (right.sqrMagnitude < 0.1f) right = Vector3.Cross(dir, Vector3.right).normalized;
        Vector3 up = Vector3.Cross(right, dir).normalized;
        
        float actualDistance = Vector3.Distance(start, end);
        float targetLength = range;
        
        float baseNoiseScale = 0.7f;
        
        float arcHeight = 0f;
        if (useArcBend && actualDistance < targetLength)
        {
            float halfDist = actualDistance * 0.5f;
            float halfTarget = targetLength * 0.5f;
            arcHeight = Mathf.Sqrt(halfTarget * halfTarget - halfDist * halfDist);
        }
        
        float accumulatedLength = 0f;
        
        for (int i = 0; i < 16; i++)
        {
            float t = i / 15f;
            Vector3 basePos = Vector3.Lerp(start, end, t);
            
            float curveHeight = 0f;
            if (useArcBend && actualDistance < targetLength)
            {
                float arcCurve = Mathf.Sin(t * Mathf.PI);
                curveHeight = arcHeight * arcCurve;
            }
            
            Vector3 arcPos = basePos + Vector3.up * curveHeight;
            baseArcPoints[i] = arcPos;
            
            if (i > 0)
            {
                accumulatedLength += Vector3.Distance(baseArcPoints[i - 1], baseArcPoints[i]);
            }
        }
        
        if (useArcBend && actualDistance < targetLength && accumulatedLength > 0f)
        {
            float scale = targetLength / accumulatedLength;
            for (int i = 1; i < 16; i++)
            {
                Vector3 prev = baseArcPoints[i - 1];
                Vector3 curr = baseArcPoints[i];
                Vector3 dirToCurr = (curr - prev).normalized;
                float segmentLength = Vector3.Distance(prev, curr) * scale;
                baseArcPoints[i] = prev + dirToCurr * segmentLength;
            }
        }
        
        int pointCount = lr.positionCount;
        for (int i = 0; i < pointCount; i++)
        {
            float t = i / 15f;
            Vector3 basePos = baseArcPoints[i];
            
            float perlinX = t * 8f + noiseSeed + elapsedTime * 5f;
            float perlinY = noiseSeed * 0.5f + elapsedTime * 3f;
            
            float noiseX = (Mathf.PerlinNoise(perlinX, perlinY) * 2f - 1f) * baseNoiseScale;
            float noiseY = (Mathf.PerlinNoise(perlinX + 100f, perlinY + 100f) * 2f - 1f) * baseNoiseScale;
            
            Vector3 offset = right * noiseX + up * noiseY;
            
            float curveIntensity = Mathf.Sin(t * Mathf.PI);
            offset *= curveIntensity;
            
            float jitterX = (Mathf.PerlinNoise(perlinX + 200f, perlinY + 200f) * 2f - 1f) * 0.15f;
            float jitterY = (Mathf.PerlinNoise(perlinX + 300f, perlinY + 300f) * 2f - 1f) * 0.15f;
            float jitterZ = (Mathf.PerlinNoise(perlinX + 400f, perlinY + 400f) * 2f - 1f) * 0.15f;
            offset += new Vector3(jitterX, jitterY, jitterZ) * curveIntensity;
            
            lr.SetPosition(i, basePos + offset);
        }
    }
    
    void OnDestroy()
    {
        if (lightningMaterial != null)
            Destroy(lightningMaterial);
    }
}

public static class LightningCore
{
    private static Collider[] overlapBuffer = new Collider[64];
    private static HashSet<Enemy> treeChainHitBuffer = new HashSet<Enemy>();
    private static Queue<TreeChainNode> treeChainQueueBuffer = new Queue<TreeChainNode>();
    private static List<Enemy> findEnemiesResultBuffer = new List<Enemy>();
    private static HashSet<Enemy> findEnemiesSeenBuffer = new HashSet<Enemy>();
    private static List<(Enemy enemy, float distance)> findEnemiesCandidatesBuffer = new List<(Enemy, float)>();
    private static List<Color> colorCalculationBuffer = new List<Color>(4);
    
    private struct TreeChainNode
    {
        public Enemy enemy;
        public Transform parent;
        public int depth;
        public float damage;
    }

    public static Color CalculateLightningColor()
    {
        colorCalculationBuffer.Clear();
        
        var rules = ElementSystem.GetTempRules();
        if (rules.TryGetValue(ElementType.Lightning, out var allowed))
        {
            if (allowed.Contains(ElementType.Poison))
                colorCalculationBuffer.Add(Color.green);
            if (allowed.Contains(ElementType.Fire))
                colorCalculationBuffer.Add(Color.red);
            if (allowed.Contains(ElementType.Ice))
                colorCalculationBuffer.Add(new Color(0.5f, 0.8f, 1f));
        }
        
        if (colorCalculationBuffer.Count == 0) return Color.white;
        
        if (colorCalculationBuffer.Count == 1) return colorCalculationBuffer[0];
        
        Color result = Color.black;
        float weight = 1f / colorCalculationBuffer.Count;
        for (int i = 0; i < colorCalculationBuffer.Count; i++)
            result += colorCalculationBuffer[i] * weight;
        
        return result;
    }

    public static void CreateTreeChain(
        Entity owner,
        Enemy startEnemy,
        float baseDamage,
        int maxDepth,
        int branchesPerNode,
        float range,
        float damageDecayPerDepth = 0.9f,
        float vfxDuration = 0.25f,
        bool skipRootProcessing = false)
    {
        if (owner == null || startEnemy == null || maxDepth <= 0 || branchesPerNode <= 0) return;

        LayerMask enemyLayer = LayerMask.GetMask("Enemy");
        
        treeChainHitBuffer.Clear();
        treeChainQueueBuffer.Clear();

        TreeChainNode rootNode = new TreeChainNode
        {
            enemy = startEnemy,
            parent = skipRootProcessing ? startEnemy.transform : owner.transform,
            depth = 0,
            damage = baseDamage
        };

        treeChainQueueBuffer.Enqueue(rootNode);
        treeChainHitBuffer.Add(startEnemy);

        while (treeChainQueueBuffer.Count > 0)
        {
            TreeChainNode current = treeChainQueueBuffer.Dequeue();

            if (!skipRootProcessing || current.depth > 0)
            {
                ApplyLightningDamage(owner, current.enemy, current.damage);

                CreateLightningVFX(
                    current.parent,
                    current.enemy.transform,
                    range,
                    vfxDuration,
                    null,
                    0.5f,
                    0.5f,
                    0.15f
                );
            }

            if (current.depth >= maxDepth) continue;

            FindNearestEnemies(
                current.enemy.transform.position,
                range,
                enemyLayer,
                treeChainHitBuffer,
                branchesPerNode
            );

            float nextDamage = current.damage * damageDecayPerDepth;
            int nextDepth = current.depth + 1;

            for (int i = 0; i < findEnemiesResultBuffer.Count; i++)
            {
                Enemy nextEnemy = findEnemiesResultBuffer[i];
                treeChainHitBuffer.Add(nextEnemy);

                TreeChainNode nextNode = new TreeChainNode
                {
                    enemy = nextEnemy,
                    parent = current.enemy.transform,
                    depth = nextDepth,
                    damage = nextDamage
                };

                treeChainQueueBuffer.Enqueue(nextNode);
            }
        }
    }

    private static void FindNearestEnemies(
        Vector3 fromPos,
        float range,
        LayerMask enemyLayer,
        HashSet<Enemy> excludeEnemies,
        int maxCount)
    {
        findEnemiesResultBuffer.Clear();
        findEnemiesCandidatesBuffer.Clear();
        findEnemiesSeenBuffer.Clear();
        
        int hitCount = Physics.OverlapSphereNonAlloc(fromPos, range, overlapBuffer, enemyLayer);

        foreach (Enemy excluded in excludeEnemies)
        {
            findEnemiesSeenBuffer.Add(excluded);
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = overlapBuffer[i];
            Enemy enemy = col.GetComponentInParent<Enemy>();
            if (enemy == null || findEnemiesSeenBuffer.Contains(enemy)) continue;

            IDamageable dmg = enemy.GetComponent<IDamageable>();
            if (dmg == null || dmg.IsDead) continue;

            findEnemiesSeenBuffer.Add(enemy);
            float dist = Vector3.Distance(fromPos, enemy.transform.position);
            findEnemiesCandidatesBuffer.Add((enemy, dist));
        }

        findEnemiesCandidatesBuffer.Sort((a, b) => a.distance.CompareTo(b.distance));

        int count = Mathf.Min(maxCount, findEnemiesCandidatesBuffer.Count);
        for (int i = 0; i < count; i++)
        {
            findEnemiesResultBuffer.Add(findEnemiesCandidatesBuffer[i].enemy);
        }
    }
    
    public static void ApplyLightningDamage(Entity owner, Component target, float damage, ElementType element = ElementType.Lightning)
    {
        if (owner == null || target == null) return;
        
        Enemy enemy = target as Enemy;
        if (enemy == null) return;
        
        IDamageable damageable = enemy.GetComponent<IDamageable>();
        if (damageable == null || damageable.IsDead) return;
        
        damageable.TakeDamage(damage);
        CombatEvents.ReportDamage(owner, enemy, damage, element);
    }
    
    public static GameObject CreateLightningVFX(
        Transform start, 
        Transform end, 
        float range, 
        float duration, 
        Color? color = null,
        float startHeightOffset = 0.5f,
        float endHeightOffset = 0.5f,
        float extendTime = 0.2f,
        bool enableArcBend = false)
    {
        if (start == null || end == null) return null;
        
        Color lightningColor = color ?? CalculateLightningColor();
        
        GameObject arcObj = new GameObject("Lightning");
        LightningVFX vfx = arcObj.AddComponent<LightningVFX>();
        vfx.Initialize(start, end, range, duration, lightningColor, startHeightOffset, endHeightOffset, extendTime, enableArcBend);
        
        return arcObj;
    }
}
