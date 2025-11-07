using UnityEngine;

public class EnemyDamageVisuals : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private Material hitFlashMaterial;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private int fontSize = 50;
    [SerializeField] private float textDuration = 1.5f;
    [SerializeField] private float riseSpeed = 1.5f;

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
    }
    
    public void ShowDamageVisuals(float damage)
    {
        if (isDead) return;
        ShowDamageNumber(damage);
        StartCoroutine(FlashHit());
    }

    private void ShowDamageNumber(float damage)
    {
        // GameObject damageText = new GameObject("DamageText");

        // random spread so numbers dont overlap
        float randomX = Random.Range(-0.5f, 0.5f);
        float randomZ = Random.Range(-0.5f, 0.5f);
        Vector3 pos = transform.position + Vector3.up * 2f + new Vector3(randomX, 0, randomZ);

        DamageNumbers.Spawn(transform, pos, damage, damageColor, fontSize, textDuration, riseSpeed);
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

}
