using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ExtractionUI : MonoBehaviour
{

    [SerializeField]
    private UIDocument uIDocument;
    private ProgressBar extractionBar;
    private ExtractionZone extractionZone;

    void Awake()
    {
        extractionZone = GetComponent<ExtractionZone>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        String className = "extraction-bar";
        float zeroValue = 0f;

        VisualElement root = uIDocument.rootVisualElement;
        this.extractionBar = root.Q<ProgressBar>(className: className);

        if (extractionBar == null)
        {
            Debug.LogError("ExtractionBar not found!");
            return;
        }

        this.extractionBar.lowValue = zeroValue;
        this.extractionBar.highValue = this.extractionZone.chargeTime;
        this.extractionBar.value = zeroValue;

        this.extractionZone.ChargeChanged += this.OnChargeChanged;
        this.extractionZone.ExtractionInteracted += this.OnExtractionInteracted;
    }

    private void OnChargeChanged(float currentCharge)
    {
        this.extractionBar.value = this.extractionZone.currentCharge;
    }

    private void OnExtractionInteracted()
    {
        this.extractionBar.visible = true;
    }
}
