using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ExtractionZone : MonoBehaviour
{
    public float chargeTime = 10f;
    private float currentCharge = 0f;
    private bool isExtracting = false;
    private bool isInteracted = false;

    public event Action<float> ChargeChanged;
    public event Action ExtractionInteracted;

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
                this.ExtractionInteracted?.Invoke();
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
        if (this.isExtracting & this.currentCharge < this.chargeTime)
        {
            this.currentCharge = Math.Clamp(this.currentCharge + Time.deltaTime, 0, this.chargeTime);
            this.ChargeChanged?.Invoke(this.currentCharge);
        } 
    }
}
