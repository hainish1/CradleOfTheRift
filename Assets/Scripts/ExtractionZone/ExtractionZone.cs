using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ExtractionZone : MonoBehaviour
{
    [SerializeField]
    private float chargeTime = 10f;
    private float currentCharge = 0f;
    private bool isExtracting = false;
    private bool isInteracted = false;

    public event Action<float> ChargeChanged;
    public event Action ExtractionInteracted;
    public event Action ExtractionFinished;
    public event Action WinScreen;
    public float ChargeTime => this.chargeTime;

    [SerializeField] private TimerUI timerUI;
    [SerializeField] private GameObject extractionBeam;

    // Update is called once per frame
    void Update()
    {
        OnExtraction();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerGold player = other.GetComponent<PlayerGold>();

        if (player != null)
        {
            this.isExtracting = true;

            // Notify UI to display extraction UI
            if (!this.isInteracted)
            {
                this.isInteracted = true;
                this.ExtractionInteracted?.Invoke();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerGold player = other.GetComponent<PlayerGold>();

        if (player != null)
        {
            this.isExtracting = false;
        }
    }

    private void OnExtraction()
    {
        if (this.isExtracting & this.currentCharge < this.chargeTime)
        {
            float zeroValue = 0f;
            this.currentCharge = Math.Clamp(this.currentCharge + Time.deltaTime, zeroValue, this.chargeTime);
            this.ChargeChanged?.Invoke(this.currentCharge);

            if (this.currentCharge == this.chargeTime)
            {
                PlayerHealth.instance.SetCanTakeDamage(false);
                this.WinScreen?.Invoke();
                this.ExtractionFinished?.Invoke();
            }
        }
    }

    private void OnEnable()
    {
        if (timerUI != null)
        {
            timerUI.DisplayExtraction += OnDisplayExtraction;
            timerUI.DisplayEndGame += OnDisplayEndGame;
        }
    }

    private void OnDisable()
    {
        if (timerUI != null)
        {
            timerUI.DisplayExtraction -= OnDisplayExtraction;
            timerUI.DisplayEndGame -= OnDisplayEndGame;
        }
    }

    private void OnDisplayExtraction()
    {
        extractionBeam.SetActive(true);
    }

    private void OnDisplayEndGame()
    {
        Debug.Log("Spawner received DisplayEndGame event!");
    }
}
