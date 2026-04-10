using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ExtractionUI : MonoBehaviour
{
    private ProgressBar extractionBar;
    private ExtractionZone activeZoneRef;

    // Tracks whether we were previously decaying so we only touch the USS class on change
    private bool wasDecaying = false;

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

        // Reset decay state on a fresh extraction
        wasDecaying = false;
        this.extractionBar.RemoveFromClassList("decaying");

        zone.ChargeChanged += OnChargeUpdate;
    }

    private void OnExtractionFinished()
    {
        if (activeZoneRef != null)
            activeZoneRef.ChargeChanged -= OnChargeUpdate;

        this.extractionBar.RemoveFromClassList("decaying");
        this.extractionBar.style.display = DisplayStyle.None;
        activeZoneRef = null;
        wasDecaying = false;
    }

    private void OnChargeUpdate(float currentCharge)
    {
        this.extractionBar.value = currentCharge;

        float percent = (currentCharge / activeZoneRef.ChargeTime) * 100f;
        this.extractionBar.title = $"Extraction: [{Mathf.RoundToInt(percent)}%]";

        // Determine decay: charge is falling (bar previously had a higher value)
        // ExtractionZone decays when the player is outside — detect this by
        // checking the previous frame's value against the current one.
        bool isDecaying = currentCharge < this.extractionBar.value;

        // Because we set extractionBar.value above, we compare against the stored field instead.
        // Use the delta approach: track last known charge on the component.
        UpdateDecayVisual(currentCharge);
    }

    // Decay detection
    // We store lastCharge so we can tell whether the bar is filling or draining,
    // regardless of the rate — this mirrors exactly what ExtractionZone does
    // (it drains at Time.deltaTime when isExtracting is false).
    private float lastCharge = 0f;

    private void UpdateDecayVisual(float currentCharge)
    {
        bool isDecaying = currentCharge < lastCharge;
        lastCharge = currentCharge;

        // Only touch USS classes when state actually changes. Avoids per-frame churn
        if (isDecaying == wasDecaying) return;
        wasDecaying = isDecaying;

        if (isDecaying)
            this.extractionBar.AddToClassList("decaying");
        else
            this.extractionBar.RemoveFromClassList("decaying");
    }
}
