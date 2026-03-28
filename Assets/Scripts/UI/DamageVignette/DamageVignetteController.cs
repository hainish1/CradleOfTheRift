using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class DamageVignetteController : MonoBehaviour
{
    public enum CriticalHealthMode
    {
        None,           // Marvel Rivals — flash on hit only, nothing extra at low health
        StaticVignette, // Risk of Rain 2 — persistent dark vignette at low health
        Pulse           // CoD/Halo — vignette pulses in and out at low health
    }

    [SerializeField] private PlayerHealth health;

    [Header("Vignette Appearance")]
    [SerializeField] private Color vignetteColor = new Color(0.6f, 0f, 0f, 1f);
    [Tooltip("How far from the edge the vignette starts (0 = full screen, 1 = edges only)")]
    [SerializeField] private float innerRadiusFraction = 0.35f;
    [Tooltip("How sharply the vignette falls off toward the center. Higher = more concentrated at edges")]
    [SerializeField] private float falloffPower = 2f;

    [Header("Flash On Hit")]
    [Tooltip("Peak opacity of the vignette flash on hit (0-1)")]
    [SerializeField] private float flashOpacity = 0.6f;
    [Tooltip("How fast the vignette fades in on hit")]
    [SerializeField] private float fadeInDuration = 0.05f;
    [Tooltip("How long before the vignette starts fading out")]
    [SerializeField] private float holdDuration = 0.1f;
    [Tooltip("How long the vignette takes to fully fade out")]
    [SerializeField] private float fadeOutDuration = 0.4f;

    [Header("Critical Health")]
    [SerializeField] private CriticalHealthMode criticalMode = CriticalHealthMode.Pulse;
    [Tooltip("Health percentage threshold to trigger critical effect (0-1)")]
    [SerializeField] private float criticalThreshold = 0.25f;
    [Tooltip("Opacity used by both Static and Pulse modes (0-1)")]
    [SerializeField] private float criticalOpacity = 0.3f;
    [Tooltip("How long each pulse fade in/out takes (Pulse mode only)")]
    [SerializeField] private float pulseDuration = 0.6f;
    [Tooltip("How long the vignette takes to fade out when leaving critical health")]
    [SerializeField] private float criticalFadeOutDuration = 0.8f;

    private VisualElement vignette;
    private Coroutine flashCoroutine;
    private Coroutine pulseCoroutine;
    private Coroutine fadeOutCoroutine;
    private float previousHealth;

    void Start()
    {
        if (health == null)
            health = PlayerLocator.FindPlayerComponent<PlayerHealth>();

        if (health == null) return;

        var root = GetComponent<UIDocument>().rootVisualElement;

        vignette = new RadialVignetteElement
        {
            vignetteColor       = vignetteColor,
            innerRadiusFraction = innerRadiusFraction,
            falloffPower        = falloffPower
        };
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

        UpdateCriticalHealth(current, max);

        previousHealth = current;
    }

    private void TriggerFlash()
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        // If a fade out was in progress, cancel it since we're flashing again
        if (fadeOutCoroutine != null)
        {
            StopCoroutine(fadeOutCoroutine);
            fadeOutCoroutine = null;
        }

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        yield return LerpOpacity(vignette.style.opacity.value, flashOpacity, fadeInDuration);
        yield return new WaitForSeconds(holdDuration);

        float fadeTarget = 0f;

        if (pulseCoroutine != null)
        {
            // Pulse mode — hand off to the pulse
            fadeTarget = criticalOpacity;
        }
        else if (criticalMode == CriticalHealthMode.StaticVignette && (previousHealth / health.MaxHealth) <= criticalThreshold)
        {
            // Static mode and still critical — settle at the persistent opacity
            fadeTarget = criticalOpacity;
        }

        yield return LerpOpacity(flashOpacity, fadeTarget, fadeOutDuration);

        flashCoroutine = null;
    }

    private void UpdateCriticalHealth(float current, float max)
    {
        bool isCritical = (current / max) <= criticalThreshold;

        switch (criticalMode)
        {
            case CriticalHealthMode.None:
                StopPulseIfRunning();
                break;

            case CriticalHealthMode.StaticVignette:
                StopPulseIfRunning();
                if (flashCoroutine == null)
                {
                    if (isCritical)
                        vignette.style.opacity = criticalOpacity;
                    else
                        StartFadeOut();
                }
                break;

            case CriticalHealthMode.Pulse:
                if (isCritical && pulseCoroutine == null)
                    pulseCoroutine = StartCoroutine(PulseRoutine());
                else if (!isCritical)
                    StopPulseIfRunning();
                break;
        }
    }

    private void StopPulseIfRunning()
    {
        if (pulseCoroutine == null) return;

        StopCoroutine(pulseCoroutine);
        pulseCoroutine = null;

        if (flashCoroutine == null)
            StartFadeOut();
    }

    private void StartFadeOut()
    {
        if (fadeOutCoroutine != null)
            StopCoroutine(fadeOutCoroutine);

        fadeOutCoroutine = StartCoroutine(FadeOutVignette());
    }

    private IEnumerator FadeOutVignette()
    {
        yield return LerpOpacity(vignette.style.opacity.value, 0f, criticalFadeOutDuration);
        fadeOutCoroutine = null;
    }

    private IEnumerator PulseRoutine()
    {
        while (true)
        {
            yield return LerpOpacity(0f, criticalOpacity, pulseDuration);
            yield return LerpOpacity(criticalOpacity, 0f, pulseDuration);
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