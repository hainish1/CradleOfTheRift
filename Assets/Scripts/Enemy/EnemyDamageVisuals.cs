using UnityEngine;

public class EnemyDamageVisuals : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Material hitFlashMaterial;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private int fontSize = 36;
    [SerializeField] private float textDuration = 1.5f;
    [SerializeField] private float riseSpeed = 1.5f;
    [SerializeField] private GameObject takeDamageVFXPrefab;
    [SerializeField] private Transform damageVfxAttachPoint;

    private Renderer meshRenderer;
    private Material originalMaterial;
    private bool isDead = false;
    private bool isDamageTextActive = false;

    private bool canDestroy = false;

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
        if (isDead) return;
        if (target == null || target.gameObject != gameObject) return;
        ShowDamageNumber(damage, elementType);
    }

    private void PlayTakeDamageVFX()
    {
        if(takeDamageVFXPrefab == null) return;
        Transform parent = damageVfxAttachPoint != null ? damageVfxAttachPoint : transform;
        GameObject vfx = Instantiate(takeDamageVFXPrefab, parent.position, parent.rotation, parent);

        Destroy(vfx, 0.5f); // Should prob make the VFX auto destroy instead of doing it here.
    }
    
    public void ShowDamageVisuals(float damage)
    {
        if (isDead) return;
        PlayTakeDamageVFX();
        StartCoroutine(FlashHit());
    }

    private void ShowDamageNumber(float damage, ElementType elementType)
    {
        float randomX = Random.Range(-0.5f, 0.5f);
        float randomZ = Random.Range(-0.5f, 0.5f);
        Vector3 pos = transform.position + Vector3.up * 2f + new Vector3(randomX, 0, randomZ);

        DamageNumbers.Spawn(transform, pos, damage, GetElementColor(elementType), fontSize, textDuration, riseSpeed);
    }

    private Color GetElementColor(ElementType elementType)
    {
        return elementType switch
        {
            ElementType.None      => Color.white,
            ElementType.Fire      => Color.red,
            ElementType.Lightning => Color.yellow,
            ElementType.Poison    => Color.green,
            ElementType.Ice       => new Color(0.5f, 0.8f, 1f),
            _                     => Color.white
        };
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

    public void SetDeadForVisuals()
    {
        isDead = true;
    }

    private System.Collections.IEnumerator WaitForDamageTextFinish()
    {

        while (isDamageTextActive)
        {
            yield return null;
        }
    }

    void OnDisable()
    {
        if(meshRenderer != null  && originalMaterial != null)
        {
            meshRenderer.material = originalMaterial;
        }       
    }

    void OnDestroy()
    {
        CombatEvents.DamageDealt -= OnDamageDealt;
    }
}
