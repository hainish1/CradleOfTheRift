using System.Collections;
using UnityEngine;

public class DamageNumbers : MonoBehaviour
{
    // all these should be world based now
    private const int BASE_FONT_SIZE = 64;
    private const float BASE_CHAR_SIZE = 0.05f; 
    private const float POP_SCALE = 1.35f;      
    private const float SETTLE_SCALE = 1f;       
    private const float POP_DURATION = 0.12f;   
    private const float SHADOW_OFFSET = 0.015f;

    private TextMesh textMesh;
    private TextMesh shadowMesh;
    private float duration;
    private float riseSpeed;
    private Vector3 startPos;
    private Color startColor;
    private float scaleMultiplier;


    public static void Spawn(Transform parent, Vector3 worldPos, float value, Color color, int fontSize, float duration, float riseSpeed)
    {
        var go = new GameObject("DamageText");
        go.transform.position = worldPos;
        var dn = go.AddComponent<DamageNumbers>();
        dn.Setup(value, color, fontSize, duration, riseSpeed);
    }

    private void Setup(float value, Color color, int fontSize, float duration, float riseSpeed)
    {
        this.duration = Mathf.Max(0.01f, duration);
        this.riseSpeed = riseSpeed;
        this.startPos = transform.position;
        scaleMultiplier = Mathf.Max(0.1f, fontSize / (float)BASE_FONT_SIZE);
        shadowMesh = CreateTextMesh(new Color(0f, 0f, 0f, 0.75f));
        shadowMesh.transform.localPosition = new Vector3(SHADOW_OFFSET, -SHADOW_OFFSET, 0.001f);
        textMesh = CreateTextMesh(color);
        shadowMesh.text = value.ToString("F1");
        textMesh.text = value.ToString("F1");

        startColor = color;

        FaceCamera();
        StartCoroutine(Animate());
    }

    private TextMesh CreateTextMesh(Color color)
    {
        var child = new GameObject("Mesh");
        child.transform.SetParent(transform, false);

        var textMesh = child.AddComponent<TextMesh>();
        textMesh.fontSize = BASE_FONT_SIZE;
        textMesh.characterSize = BASE_CHAR_SIZE;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.color = color;
        textMesh.richText = false;

        var meshRen = child.GetComponent<MeshRenderer>();
        if (meshRen != null)
        {
            meshRen.sortingOrder = 5000;
        }
        return textMesh;
    }

    private IEnumerator Animate()
    {
        float elapsed = 0f;
        // safety
        Destroy(gameObject, duration + 0.25f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float time = Mathf.Clamp01(elapsed / duration);

            float rise = 1f - Mathf.Pow(1f - time, 2f);
            transform.position = startPos + Vector3.up * (riseSpeed * duration * rise);

            float popTime = Mathf.Clamp01(elapsed / POP_DURATION);
            float scale = Mathf.Lerp(POP_SCALE, SETTLE_SCALE, popTime) * scaleMultiplier;
            transform.localScale = Vector3.one * scale;

            // fade
            float fadeTime = Mathf.InverseLerp(0.4f, 1f, time);
            float alpha = 1f - (fadeTime * fadeTime);

            var color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            if (textMesh != null) textMesh.color = color;
            if (shadowMesh != null) shadowMesh.color = new Color(0f, 0f, 0f, alpha * 0.75f);

            FaceCamera();
            yield return null;
        }

        Destroy(gameObject);
    }

    private void FaceCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        // face the camera plane
        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, Vector3.up);
    }
}
