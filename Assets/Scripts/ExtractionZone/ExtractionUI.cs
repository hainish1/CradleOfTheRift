using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ExtractionUI : MonoBehaviour
{
    private ProgressBar extractionBar;
    private ExtractionZone activeZoneRef;

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
        
        // Subscribe to the specific zone's progress
        zone.ChargeChanged += OnChargeUpdate;
    }

    private void OnExtractionFinished()
    {
        if (activeZoneRef != null)
            activeZoneRef.ChargeChanged -= OnChargeUpdate;

        this.extractionBar.style.display = DisplayStyle.None;
        activeZoneRef = null;
    }

    private void OnChargeUpdate(float currentCharge)
    {
        this.extractionBar.value = currentCharge;
        float percent = (currentCharge / activeZoneRef.ChargeTime) * 100f;
        this.extractionBar.title = $"Extraction: [{Mathf.RoundToInt(percent)}%]";
    }
}