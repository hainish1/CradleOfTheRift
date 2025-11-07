using System.Collections;
using UnityEngine;

public class DamageNumbers : MonoBehaviour
{
    private TextMesh textMesh;
    private float duration;
    private float riseSpeed;
    private Vector3 startPos;
    private Color startColor;

    public static void Spawn(Transform parent, Vector3 worldPos, float value, Color color, int fontSize, float duration, float riseSpeed)
    {
        var go = new GameObject("DamageText");
        go.transform.position = worldPos;
        if(parent != null)
        {
            go.transform.SetParent(parent);
        }
        
        var dn = go.AddComponent<DamageNumbers>();
        dn.Setup(value, color, fontSize, duration, riseSpeed);
    }

    private void Setup(float value, Color color, int fontSize, float duration, float riseSpeed)
    {
        this.duration = Mathf.Max(0.01f, duration);
        this.riseSpeed = riseSpeed;
        this.startPos = transform.position;
        textMesh = gameObject.AddComponent<TextMesh>();
        textMesh.text = value.ToString("F1");
        textMesh.fontSize = fontSize;
        textMesh.color = color;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.characterSize = 0.2f;

        startColor = textMesh.color;

        // face caemra
        if (Camera.main != null) transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);

        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        float elapsed = 0f;
        Destroy(gameObject, duration + .25f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // move up
            transform.position = startPos + Vector3.up * (riseSpeed * elapsed);

            // scale: start big, shrink to normal
            float scale = Mathf.Lerp(1.5f, 1f, Mathf.Min(t * 3f, 1f));
            transform.localScale = Vector3.one * scale;

            // fade out
            float alpha = 1f - (t * t);  // quadratic fade looks better
            textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            // keep facing camera
            if (Camera.main != null)
                transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);

            yield return null;
        }

        Destroy(gameObject);
    }
}
