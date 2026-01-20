using System.Collections.Generic;
using UnityEngine;

public class LightningVFX : MonoBehaviour
{
    private LineRenderer lr;
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
    
    public void Initialize(Transform start, Transform end, float range, float duration, Color lightningColor, float startHeightOffset = 0.5f, float endHeightOffset = 0.5f, float extendTime = 0.2f)
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
        
        lr = gameObject.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
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
        if (actualDistance < targetLength)
        {
            float halfDist = actualDistance * 0.5f;
            float halfTarget = targetLength * 0.5f;
            arcHeight = Mathf.Sqrt(halfTarget * halfTarget - halfDist * halfDist);
        }
        
        List<Vector3> baseArcPoints = new List<Vector3>();
        float accumulatedLength = 0f;
        
        for (int i = 0; i < 16; i++)
        {
            float t = i / 15f;
            Vector3 basePos = Vector3.Lerp(start, end, t);
            
            float curveHeight = 0f;
            if (actualDistance < targetLength)
            {
                float arcCurve = Mathf.Sin(t * Mathf.PI);
                curveHeight = arcHeight * arcCurve;
            }
            
            Vector3 arcPos = basePos + Vector3.up * curveHeight;
            baseArcPoints.Add(arcPos);
            
            if (i > 0)
            {
                accumulatedLength += Vector3.Distance(baseArcPoints[i - 1], baseArcPoints[i]);
            }
        }
        
        if (actualDistance < targetLength && accumulatedLength > 0f)
        {
            float scale = targetLength / accumulatedLength;
            for (int i = 1; i < baseArcPoints.Count; i++)
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
}

public static class LightningCore
{
    public static Color CalculateLightningColor()
    {
        List<Color> elementColors = new List<Color>();
        
        var rules = ElementSystem.GetTempRules();
        if (rules.TryGetValue(ElementType.Lightning, out var allowed))
        {
            if (allowed.Contains(ElementType.Poison))
                elementColors.Add(Color.green);
            if (allowed.Contains(ElementType.Fire))
                elementColors.Add(Color.red);
            if (allowed.Contains(ElementType.Ice))
                elementColors.Add(new Color(0.5f, 0.8f, 1f));
        }
        
        if (elementColors.Count == 0) return Color.white;
        
        if (elementColors.Count == 1) return elementColors[0];
        
        Color result = Color.black;
        float weight = 1f / elementColors.Count;
        foreach (var c in elementColors)
            result += c * weight;
        
        return result;
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
        float extendTime = 0.2f)
    {
        if (start == null || end == null) return null;
        
        Color lightningColor = color ?? CalculateLightningColor();
        
        GameObject arcObj = new GameObject("Lightning");
        LightningVFX vfx = arcObj.AddComponent<LightningVFX>();
        vfx.Initialize(start, end, range, duration, lightningColor, startHeightOffset, endHeightOffset, extendTime);
        
        return arcObj;
    }
}
