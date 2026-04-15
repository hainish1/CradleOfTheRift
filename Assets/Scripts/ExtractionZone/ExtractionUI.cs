using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class ExtractionUI : MonoBehaviour
{
    private ProgressBar extractionBar;
    private ExtractionZone activeZoneRef;

    private bool wasDecaying = false;
    private float lastCharge = 0f;
    private Coroutine flashRoutine;

    void Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        this.extractionBar = root.Q<ProgressBar>("ExtractionBar");
        this.extractionBar.style.display = DisplayStyle.None;

        if (ExtractionManager.Instance != null)
        {
            ExtractionManager.Instance.ExtractionStarted += OnExtractionStarted;
            ExtractionManager.Instance.AllExtractionsFinished += OnExtractionFinished;
        }
    }

    private void OnExtractionStarted(ExtractionZone zone)
    {
        activeZoneRef = zone;
        this.extractionBar.highValue = zone.ChargeTime;
        this.extractionBar.style.display = DisplayStyle.Flex;

        lastCharge = 0f;
        wasDecaying = false;
        StopFlash();

        zone.ChargeChanged += OnChargeUpdate;
    }

    private void OnExtractionFinished()
    {
        if (activeZoneRef != null)
            activeZoneRef.ChargeChanged -= OnChargeUpdate;

        StopFlash();
        this.extractionBar.style.display = DisplayStyle.None;
        activeZoneRef = null;
        wasDecaying = false;
    }

    private void OnChargeUpdate(float currentCharge)
    {
        this.extractionBar.value = currentCharge;

        float percent = (currentCharge / activeZoneRef.ChargeTime) * 100f;
        this.extractionBar.title = $"Extraction: [{Mathf.RoundToInt(percent)}%]";

        UpdateDecayVisual(currentCharge);
    }

    private void UpdateDecayVisual(float currentCharge)
    {
        bool isDecaying = currentCharge < lastCharge;
        lastCharge = currentCharge;

        // Only act when state actually changes — avoids per-frame class churn
        if (isDecaying == wasDecaying) return;
        wasDecaying = isDecaying;

        if (isDecaying)
        {
            this.extractionBar.AddToClassList("decaying");
            flashRoutine = StartCoroutine(FlashLoop());
        }
        else
        {
            StopFlash();
        }
    }

    private void StopFlash()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
        this.extractionBar.RemoveFromClassList("decaying-flash");
        this.extractionBar.RemoveFromClassList("decaying");
    }

    // Asymmetric pulse: fast snap in (0.08s hold), slow fade out (0.55s rest)
    // Feels like a warning heartbeat rather than a mechanical strobe
    private IEnumerator FlashLoop()
    {
        while (true)
        {
            this.extractionBar.AddToClassList("decaying-flash");
            yield return new WaitForSeconds(0.08f);    // hold the bright flare briefly
            this.extractionBar.RemoveFromClassList("decaying-flash");
            yield return new WaitForSeconds(0.55f);    // sit in the smolder state longer
        }
    }
}
