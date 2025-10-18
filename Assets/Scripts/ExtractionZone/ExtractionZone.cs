using System;
using System.Collections;
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

    [Header("Beam Grow Settings")]
    
    private Coroutine beamGrowRoutine;


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

        // Animate beam growth when it appears
        if (beamGrowRoutine != null)
            StopCoroutine(beamGrowRoutine);

        beamGrowRoutine = StartCoroutine(GrowBeam());
    }

    private IEnumerator GrowBeam()
    {
    float finalHeight = 10f;
    Vector3 startScale = new Vector3(0.5f, 0f, 0.5f);
    Vector3 endScale = new Vector3(0.5f, finalHeight, 0.5f);

    extractionBeam.transform.localScale = startScale;
    extractionBeam.transform.localPosition = Vector3.zero; // start at 0

    float elapsed = 0f;
    float duration = 1f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;

        // Smoothly scale Y
        float yScale = Mathf.Lerp(0f, finalHeight, t);
        extractionBeam.transform.localScale = new Vector3(0.5f, yScale, 0.5f);

        // Smoothly move up by half the current height
        extractionBeam.transform.localPosition = new Vector3(0f, yScale, 0f);

        yield return null;
    }

    // Ensure final values are exact
    extractionBeam.transform.localScale = endScale;
    extractionBeam.transform.localPosition = new Vector3(0f, finalHeight, 0f);
    }


    private void OnDisplayEndGame()
    {
        Debug.Log("Spawner received DisplayEndGame event!");
    }
}
