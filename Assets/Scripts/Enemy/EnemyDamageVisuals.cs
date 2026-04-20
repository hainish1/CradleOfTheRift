using UnityEngine;

public class EnemyDamageVisuals : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Material hitFlashMaterial;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private int fontSize = 64;
    [SerializeField] private float textDuration = 1.2f;
    [SerializeField] private float riseSpeed = 1.2f;
    [SerializeField] private GameObject takeDamageVFXPrefab;
    [SerializeField] private Transform damageVfxAttachPoint;

    [Header("Damage text placement")]
    // vertical offset
    [SerializeField] private float textHeightOffset = 2f;
    [SerializeField] private float textSpreadRadius = 0.4f;

    private Renderer meshRenderer;
    private Material originalMaterial;
    private bool isDead = false;

    private void Start()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null)
        {
            originalMaterial = meshRenderer.material;
        }
        CombatEvents.DamageDealt += OnDamageDealt;
    }

    private void OnDamageDealt(Entity attacker, Component target, float damage, ElementType elementType)
    {
        if (target == null || target.gameObject != gameObject) return;
        ShowDamageNumber(damage, elementType);

        // still finish showing even if dead
        if (!isDead)
        {
            PlayTakeDamageVFX();
            StartCoroutine(FlashHit());
        }
    }

    private void PlayTakeDamageVFX()
    {
        if (takeDamageVFXPrefab == null) return;
        Transform parent = damageVfxAttachPoint != null ? damageVfxAttachPoint : transform;

        GameObject vfx;
        if (ObjectPool.instance != null)
        {
            // pool returns detached but thats fine i think
            vfx = ObjectPool.instance.GetObject(takeDamageVFXPrefab, parent);
        }
        else
        {
            vfx = Instantiate(takeDamageVFXPrefab, parent.position, parent.rotation, parent);
        }
        vfx.transform.position = parent.position;
        vfx.transform.rotation = parent.rotation;

        if (ObjectPool.instance != null)
            ObjectPool.instance.ReturnObject(vfx, 0.5f);
        else
            Destroy(vfx, 0.5f);
    }

    public void ShowDamageVisuals(float damage)
    {
        if (isDead) return;
        PlayTakeDamageVFX();
        StartCoroutine(FlashHit());
    }

    private void ShowDamageNumber(float damage, ElementType elementType)
    {
        float randomX = Random.Range(-textSpreadRadius, textSpreadRadius);
        float randomZ = Random.Range(-textSpreadRadius, textSpreadRadius);
        Vector3 pos = transform.position + Vector3.up * textHeightOffset + new Vector3(randomX, 0f, randomZ);
        DamageNumbers.Spawn(transform, pos, damage, GetElementColor(elementType), fontSize, textDuration, riseSpeed);
    }

    private Color GetElementColor(ElementType elementType)
    {
        return elementType switch
        {
            ElementType.None => Color.white,
            ElementType.Fire => new Color(1f, 0.45f, 0.15f),
            ElementType.Lightning => new Color(0.79f, 0.27f, 0.89f),
            ElementType.Poison => new Color(0.55f, 1f, 0.35f),
            ElementType.Ice => new Color(0.3f, 0.85f, 1f),
            _ => Color.white
        };
    }

    private System.Collections.IEnumerator FlashHit()
    {
        if (meshRenderer != null && hitFlashMaterial != null)
        {
            meshRenderer.material = hitFlashMaterial;
            yield return new WaitForSeconds(flashDuration);
            if (meshRenderer != null && originalMaterial != null)
            {
                meshRenderer.material = originalMaterial;
            }
                
        }
    }

    public void SetDeadForVisuals()
    {
        isDead = true;
    }

    void OnDisable()
    {
        if (meshRenderer != null && originalMaterial != null)
        {
            meshRenderer.material = originalMaterial;
        }
    }

    void OnDestroy()
    {
        CombatEvents.DamageDealt -= OnDamageDealt;
    }
}
