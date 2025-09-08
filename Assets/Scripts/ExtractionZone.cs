using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ExtractionZone : MonoBehaviour
{
    [SerializeField]
    private float chargeTime = 20f;
    private float currentCharge = 0f;
    private bool isExtracting = false;
    private bool isInteracted = false;

    [SerializeField]
    private UIDocument uIDocument;
    private ProgressBar extractionBar;

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
        this.extractionBar.highValue = chargeTime;
        this.extractionBar.value = zeroValue;
    }

    // Update is called once per frame
    void Update()
    {
        OnExtraction();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController playerController = other.GetComponent<PlayerController>();

        if (playerController != null)
        {
            this.isExtracting = true;

            if (!this.isInteracted)
            {
                this.isInteracted = true;
                this.extractionBar.visible = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController playerController = other.GetComponent<PlayerController>();

        if (playerController != null)
        {
            this.isExtracting = false;
        }
    }

    private void OnExtraction()
    {
        if (this.isExtracting)
        {
            this.currentCharge += Time.deltaTime;
            this.extractionBar.value = this.currentCharge;
        } 
    }
}
