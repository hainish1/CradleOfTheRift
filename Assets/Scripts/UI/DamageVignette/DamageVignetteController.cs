using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class DamageVignetteController : MonoBehaviour
{
    [SerializeField] private PlayerHealth health;

    [Header("Flash On Hit")]
    [Tooltip("Peak opacity of the vignette flash on hit (0-1)")]
    [SerializeField] private float flashOpacity = 0.6f;

    [Tooltip("How fast the vignette fades in on hit")]
    [SerializeField] private float fadeInDuration = 0.05f;

    [Tooltip("How long before the vignette starts fading out")]
    [SerializeField] private float holdDuration = 0.1f;

    [Tooltip("How long the vignette takes to fully fade out")]
    [SerializeField] private float fadeOutDuration = 0.4f;
    

    [Header("Critical Pulse")]
    [Tooltip("Health percentage threshold to trigger critical pulse (0-1)")]
    [SerializeField] private float criticalThreshold = 0.25f;

    [Tooltip("Peak opacity of the critical pulse (0-1)")]
    [SerializeField] private float pulseOpacity = 0.3f;

    [Tooltip("How long each pulse fade in/out takes")]
    [SerializeField] private float pulseDuration = 0.6f;

    private VisualElement vignette;
    private Coroutine flashCoroutine;
    private Coroutine pulseCoroutine;
    private float previousHealth;

    void Start()
    {
        if (health == null)
            health = PlayerLocator.FindPlayerComponent<PlayerHealth>();

        if (health == null) return;

        var root = GetComponent<UIDocument>().rootVisualElement;

        vignette = new RadialVignetteElement();
        vignette.style.opacity = 0;
        root.Add(vignette);

        previousHealth = health.CurrentHealth;
        health.healthChanged += OnHealthChanged;
    }

    void OnDestroy()
    {
        if (health != null)
            health.healthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(float current, float max)
    {
        if (current < previousHealth)
            TriggerFlash();

        UpdateLowHealthPulse(current, max);

        previousHealth = current;
    }

    private void TriggerFlash()
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        yield return LerpOpacity(vignette.style.opacity.value, flashOpacity, fadeInDuration);
        yield return new WaitForSeconds(holdDuration);
        yield return LerpOpacity(flashOpacity, pulseCoroutine != null ? pulseOpacity : 0f, fadeOutDuration);

        flashCoroutine = null;
    }

    private void UpdateLowHealthPulse(float current, float max)
    {
        bool isCritical = (current / max) <= criticalThreshold;

        if (isCritical && pulseCoroutine == null)
        {
            pulseCoroutine = StartCoroutine(PulseRoutine());
        }
        else if (!isCritical && pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;

            // Only snap to 0 if no flash is currently playing
            if (flashCoroutine == null)
                vignette.style.opacity = 0;
        }
    }

    private IEnumerator PulseRoutine()
    {
        while (true)
        {
            yield return LerpOpacity(0f, pulseOpacity, pulseDuration);
            yield return LerpOpacity(pulseOpacity, 0f, pulseDuration);
        }
    }

    private IEnumerator LerpOpacity(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            vignette.style.opacity = Mathf.Lerp(from, to, t);
            yield return null;
        }
        vignette.style.opacity = to;
    }
}