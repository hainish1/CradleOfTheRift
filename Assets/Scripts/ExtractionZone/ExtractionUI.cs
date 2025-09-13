using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ExtractionUI : MonoBehaviour
{
    private ProgressBar extractionBar;

    [SerializeField]
    private ExtractionZone extractionZone;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        String className = "extraction-bar";
        float zeroValue = 0f;

        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        this.extractionBar = root.Q<ProgressBar>(className: className);

        this.extractionBar.lowValue = zeroValue;
        this.extractionBar.highValue = this.extractionZone.ChargeTime;
        this.extractionBar.value = zeroValue;
        this.extractionBar.visible = false;

        this.extractionZone.ChargeChanged += this.OnChargeChanged;
        this.extractionZone.ExtractionInteracted += this.OnExtractionInteracted;
    }

    private void OnChargeChanged(float currentCharge)
    {
        this.extractionBar.value = currentCharge;
    }

    private void OnExtractionInteracted()
    {
        this.extractionBar.visible = true;
    }
}
