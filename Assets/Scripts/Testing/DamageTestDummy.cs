using UnityEngine;

/// <summary>
/// Test dummy for damage testing.
/// Also inherits from Enemy to be compatible with player damage systems, but disables AI.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DamageTestDummy : Enemy, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool invincible = false;
    [SerializeField] private bool autoHeal = true;
    [SerializeField] private float healDelay = 3f;
    
    [Header("Visual Feedback")]
    [SerializeField] private bool showDamageNumbers = true;
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private Material hitFlashMaterial;
    [SerializeField] private float flashDuration = 0.1f;
    
    [Header("Damage Number Settings")]
    [SerializeField] private int damageFontSize = 50;
    [SerializeField] private float damageTextDuration = 1.5f;
    [SerializeField] private float damageTextRiseSpeed = 1.5f;
    [SerializeField] private float damageTextSpread = 0.5f;
    [SerializeField] private bool damageTextFadeOut = true;
    
    [Header("Debug")]
    [SerializeField] private bool logDamageToConsole = true;
    
    private float currentHealth;
    private float lastDamageTime;
    private Renderer meshRenderer;
    private Material originalMaterial;
    private float totalDamageTaken = 0f;
    private int hitCount = 0;

    // IDamageable interface implementation
    public bool IsDead => currentHealth <= 0 && !invincible;

    public override void Awake()
    {
        // Disable AI initialization
    }

    void Start()
    {
        currentHealth = maxHealth;
        meshRenderer = GetComponentInChildren<Renderer>();
        if (meshRenderer != null)
        {
            originalMaterial = meshRenderer.material;
        }
    }

    public override void Update()
    {
        if (autoHeal && currentHealth < maxHealth && Time.time - lastDamageTime > healDelay)
        {
            currentHealth = Mathf.Min(currentHealth + Time.deltaTime * (maxHealth / 2f), maxHealth);
        }
    }

    public void TakeDamage(float damage)
    {
        if (invincible) return;

        currentHealth -= damage;
        lastDamageTime = Time.time;
        totalDamageTaken += damage;
        hitCount++;

        if (logDamageToConsole)
        {
            Debug.Log($"[Dummy] Took {damage:F1} damage | Health: {currentHealth:F1}/{maxHealth} | Total Hits: {hitCount} | Total Damage: {totalDamageTaken:F1}");
        }

        if (showDamageNumbers)
        {
            ShowDamageNumber(damage);
        }

        StartCoroutine(FlashHit());

        if (currentHealth <= 0)
        {
            OnDeath();
        }
    }

    private void ShowDamageNumber(float damage)
    {
        GameObject damageText = new GameObject("DamageText");
        
        float randomX = Random.Range(-damageTextSpread, damageTextSpread);
        float randomZ = Random.Range(-damageTextSpread, damageTextSpread);
        damageText.transform.position = transform.position + Vector3.up * 2f + new Vector3(randomX, 0, randomZ);
        
        if (Camera.main != null)
        {
            damageText.transform.rotation = Quaternion.LookRotation(damageText.transform.position - Camera.main.transform.position);
        }
        
        TextMesh textMesh = damageText.AddComponent<TextMesh>();
        textMesh.text = damage.ToString("F0");
        textMesh.fontSize = damageFontSize;
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
        
        float startScale = 1.5f;
        float endScale = 1.0f;
        
        while (elapsed < damageTextDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / damageTextDuration;
            
            damageText.transform.position = startPos + Vector3.up * (damageTextRiseSpeed * elapsed);
            
            float scale = Mathf.Lerp(startScale, endScale, Mathf.Min(progress * 3f, 1f));
            damageText.transform.localScale = Vector3.one * scale;
            
            if (damageTextFadeOut)
            {
                float alpha = 1f - Mathf.Pow(progress, 2f);
                textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }
            
            if (Camera.main != null)
            {
                damageText.transform.rotation = Quaternion.LookRotation(damageText.transform.position - Camera.main.transform.position);
            }
            
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

    private void OnDeath()
    {
        if (logDamageToConsole)
        {
            Debug.Log($"[Dummy] DESTROYED | Total Hits: {hitCount} | Total Damage: {totalDamageTaken:F1}");
        }
        
        currentHealth = maxHealth;
        hitCount = 0;
        totalDamageTaken = 0f;
    }

    public override void Die()
    {
        OnDeath();
    }

    [ContextMenu("Take 10 Damage")]
    private void TestDamage10()
    {
        TakeDamage(10f);
    }

    [ContextMenu("Reset Dummy")]
    private void ResetDummy()
    {
        currentHealth = maxHealth;
        hitCount = 0;
        totalDamageTaken = 0f;
        Debug.Log("[Dummy] Reset to full health");
    }

    [ContextMenu("Print Stats")]
    private void PrintStats()
    {
        Debug.Log($"[Dummy] Health: {currentHealth:F1}/{maxHealth} | Hits: {hitCount} | Total Damage: {totalDamageTaken:F1}");
    }

    private void OnDrawGizmosSelected()
    {
        if (currentHealth > 0)
        {
            Vector3 barPos = transform.position + Vector3.up * 3f;
            float healthPercent = currentHealth / maxHealth;
            
            Gizmos.color = Color.red;
            Gizmos.DrawLine(barPos, barPos + Vector3.right * healthPercent * 2f);
            
            Gizmos.color = Color.green;
            Gizmos.DrawLine(barPos, barPos + Vector3.right * 2f);
        }
    }
}