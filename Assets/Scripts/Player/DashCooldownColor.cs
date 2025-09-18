using System;
using System.Collections;
using UnityEngine;

public class DashCooldownColor : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private Renderer targetRenderer;

    [Header("Colors")]
    [SerializeField] private Color readyColor = Color.white;
    [SerializeField] private Color cooldownColor = new Color(1,1,1); 

    private MaterialPropertyBlock mpb;
    private Coroutine fadeCo;
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        if (!targetRenderer) targetRenderer = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
        SetColorImmediate(readyColor);
        movement = GetComponentInParent<PlayerMovement>();

    }

    void OnEnable()
    {
        if (movement != null)
        {
            movement.DashCooldownStarted += OnDashCooldownStarted;
        }
    }

    void OnDisable()
    {
        if (movement != null)
        {
            movement.DashCooldownStarted -= OnDashCooldownStarted;
        }
    }

    private void OnDashCooldownStarted(float duration)
    {
        if (fadeCo != null) StopCoroutine(fadeCo);
        fadeCo = StartCoroutine(FadeFromCooldownToReady(duration));
    }

private IEnumerator FadeFromCooldownToReady(float duration)
    {
        // first immed set cooldown color
        SetColorImmediate(cooldownColor);
        if (duration <= 0f)
        {
            SetColorImmediate(readyColor);
            yield break;
        }

        float t = 0f;
        // im fadeddd
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);
            Color c = Color.Lerp(cooldownColor, readyColor, a);
            SetColorImmediate(c);
            yield return null;
        }
        SetColorImmediate(readyColor);
        fadeCo = null;
    }

    private void SetColorImmediate(Color c)
    {
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(BaseColorID, c);
        targetRenderer.SetPropertyBlock(mpb);
    }
}
