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

        // // face camera
        // if (Camera.main != null)
        //     damageText.transform.rotation = Quaternion.LookRotation(damageText.transform.position - Camera.main.transform.position);

        // TextMesh textMesh = damageText.AddComponent<TextMesh>();
        // textMesh.text = damage.ToString("F1");
        // textMesh.fontSize = fontSize;
        // textMesh.color = damageColor;
        // textMesh.anchor = TextAnchor.MiddleCenter;
        // textMesh.characterSize = 0.2f;

        // isDamageTextActive = true;
        // StartCoroutine(AnimateDamageText(damageText, textMesh));
        DamageNumbers.Spawn(pos, damage, damageColor, fontSize, textDuration, riseSpeed);
    }

    private System.Collections.IEnumerator AnimateDamageText(GameObject damageText, TextMesh textMesh)
    {
        float elapsed = 0f;
        Vector3 startPos = damageText.transform.position;
        Color startColor = textMesh.color;


        while (elapsed < textDuration)
        {
            if (isDead)
            {
                Destroy(damageText);
                canDestroy = true;
                break;
            }
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
        isDamageTextActive = false;
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

        // if (isDamageTextActive)
        // {
        //     // StartCoroutine(WaitForDamageTextFinish());
        // }
    }

    private System.Collections.IEnumerator WaitForDamageTextFinish()
    {

        while (isDamageTextActive)
        {
            yield return null;
        }
    }

    public bool GetCanDestroy()
    {
        return canDestroy;
    }

    void OnDisable()
    {
        if(meshRenderer != null  && originalMaterial != null)
        {
            meshRenderer.material = originalMaterial;
        }       
    }

}
