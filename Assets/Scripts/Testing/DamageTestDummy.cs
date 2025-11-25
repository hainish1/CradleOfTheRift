using UnityEngine;

// dummy for testing damage numbers
// just shows damage, doesn't actually take damage or die
[RequireComponent(typeof(Collider))]
public class DamageTestDummy : Enemy, IDamageable
{
    [Header("Visuals")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private Material hitFlashMaterial;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private int fontSize = 50;
    [SerializeField] private float textDuration = 1.5f;
    [SerializeField] private float riseSpeed = 1.5f;
    
    [Header("Debug")]
    [SerializeField] private bool logDamage = true;
    
    private Renderer meshRenderer;
    private Material originalMaterial;
    private float totalDamageTaken = 0f;
    private int hitCount = 0;

    // always invincible, never dies
    public bool IsDead => false;

    public override void Awake()
    {
        // dont run enemy AI
    }

    new void Start()
    {
        meshRenderer = GetComponentInChildren<Renderer>();
        if (meshRenderer != null)
            originalMaterial = meshRenderer.material;
    }

    public override void Update()
    {
        // nothing to update
    }

    public void TakeDamage(float damage)
    {
        totalDamageTaken += damage;
        hitCount++;

        if (logDamage)
            Debug.Log($"[Dummy] Took {damage:F1} damage | Total Hits: {hitCount} | Total Damage: {totalDamageTaken:F1}");

        ShowDamageNumber(damage);
        StartCoroutine(FlashHit());
    }

    private void ShowDamageNumber(float damage)
    {
        GameObject damageText = new GameObject("DamageText");
        
        // random spread so numbers dont overlap
        float randomX = Random.Range(-0.5f, 0.5f);
        float randomZ = Random.Range(-0.5f, 0.5f);
        damageText.transform.position = transform.position + Vector3.up * 2f + new Vector3(randomX, 0, randomZ);
        
        // face camera
        if (Camera.main != null)
            damageText.transform.rotation = Quaternion.LookRotation(damageText.transform.position - Camera.main.transform.position);
        
        TextMesh textMesh = damageText.AddComponent<TextMesh>();
        textMesh.text = damage.ToString("F1");
        textMesh.fontSize = fontSize;
        textMesh.color = damageColor;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.characterSize = 0.2f;
        
        StartCoroutine(AnimateDamageText(damageText, textMesh));
    }
    
    private System.Collections.IEnumerator AnimateDamageText(GameObject damageText, TextMesh textMesh)
    {
        float elapsed = 0f;
        Vector3 startPos = damageText.transform.position;
        Color startColor = textMesh.color;
        
        while (elapsed < textDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / textDuration;
            
            // move up
            damageText.transform.position = startPos + Vector3.up * (riseSpeed * elapsed);
            
            // scale: start big, shrink to normal
            float scale = Mathf.Lerp(1.5f, 1f, Mathf.Min(t * 3f, 1f));
            damageText.transform.localScale = Vector3.one * scale;
            
            // fade out
            float alpha = 1f - (t * t);  // quadratic fade looks better
            textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            
            // keep facing camera
            if (Camera.main != null)
                damageText.transform.rotation = Quaternion.LookRotation(damageText.transform.position - Camera.main.transform.position);
            
            yield return null;
        }
        
        Destroy(damageText);
    }

    private System.Collections.IEnumerator FlashHit()
    {
        if (meshRenderer != null && hitFlashMaterial != null)
        {
            meshRenderer.material = hitFlashMaterial;
            yield return new WaitForSeconds(flashDuration);
            meshRenderer.material = originalMaterial;
        }
    }

    public override void Die()
    {
        // dummy never dies
    }

    // testing shortcuts
    [ContextMenu("Take 10 Damage")]
    private void TestDamage()
    {
        TakeDamage(10f);
    }

    [ContextMenu("Reset Stats")]
    private void ResetStats()
    {
        hitCount = 0;
        totalDamageTaken = 0f;
        Debug.Log("[Dummy] Stats reset");
    }

    [ContextMenu("Print Stats")]
    private void PrintStats()
    {
        Debug.Log($"[Dummy] Total Hits: {hitCount} | Total Damage: {totalDamageTaken:F1}");
    }
}