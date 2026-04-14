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
    private bool hasFinishedExtracting = false;
    [SerializeField] private bool needToKillBoss = false;


    public event Action<float> ChargeChanged;
    public event Action<ExtractionZone> ExtractionInteracted;
    public event Action ExtractionFinished;
    public float ChargeTime => this.chargeTime;

    [SerializeField] private GameObject extractionBeam;
    [SerializeField] private GameObject extractionAmbienceVFX;

    [Header("Beam Grow Settings")]
    [SerializeField] private float beamHeight = 10f;
    [SerializeField] private float beamWidth = 0.5f;
    [SerializeField] private float beamDuration = 1f;

    private bool hasSpawnedBoss = false;
    public event Action BossSpawnRequested;
    private Transform spawnPoint;
    public Transform GetSpawnPoint => this.spawnPoint;

    private bool isBossDead = false;
    private BossSpawner bossSpawner;


    private Coroutine beamGrowRoutine;

    private void OnEnable()
    {
        TimerUI.DisplayExtractionBeam += OnDisplayExtraction;
    }

    private void OnDisable()
    {
        TimerUI.DisplayExtractionBeam -= OnDisplayExtraction;
    }

    private void Awake()
    {
        spawnPoint = transform.Find("BossSpawnPoint");
        if (spawnPoint == null)
            Debug.LogError("SpawnPoint not found!");
        
        this.bossSpawner = GetComponent<BossSpawner>();
        
        if (bossSpawner != null)
            this.bossSpawner.BossDied += OnBossDied;
    }

    private void Start()
    {
        if (ExtractionManager.Instance != null)
        {
            ExtractionManager.Instance.RegisterZone(this);
        }    
    }


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
            // 1. If this is the VERY FIRST time anyone touches this zone
            if (!this.isInteracted && ExtractionManager.Instance.CanStartExtraction())
            {
                this.isInteracted = true;
                this.isExtracting = true;

                ExtractionManager.Instance.OnZoneStarted(this);
                this.ExtractionInteracted?.Invoke(this);

                if (!this.hasSpawnedBoss)
                {
                    hasSpawnedBoss = true;
                    BossSpawnRequested?.Invoke();
                }

                extractionAmbienceVFX.SetActive(false);
            }
            // 2. If the zone was already activated/interacted with, just resume extracting
            else if (this.isInteracted) 
            {
                this.isExtracting = true;
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
    if (this.isExtracting && this.currentCharge < this.chargeTime)
    {
        if(needToKillBoss == false)
            {
                isBossDead = true;
            }
        // Calculate the 99% threshold
        float maxAllowedCharge = this.isBossDead ? this.chargeTime : this.chargeTime * 0.99f;

        // Increment charge
        this.currentCharge += Time.deltaTime;

        // Clamp based on whether the boss is dead or not
        this.currentCharge = Math.Clamp(this.currentCharge, 0, maxAllowedCharge);

        // Check for completion (only possible if isBossDead is true and charge hits 100%)
        if (this.currentCharge >= this.chargeTime && !this.hasFinishedExtracting && this.isBossDead)
        {
            this.hasFinishedExtracting = true;
            this.isExtracting = false;
            this.ExtractionFinished?.Invoke();

            // Disable the entire zone object
            // gameObject.SetActive(false);
        }
    }

    this.ChargeChanged?.Invoke(this.currentCharge);
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
    Vector3 startScale = new Vector3(this.beamWidth, 0f, this.beamWidth);
    Vector3 endScale = new Vector3(this.beamWidth, this.beamHeight, this.beamWidth);

    extractionBeam.transform.localScale = startScale;
    extractionBeam.transform.localPosition = Vector3.zero; 

    float elapsed = 0f;

    while (elapsed < this.beamDuration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / this.beamDuration;

        // Smoothly scale Y
        float yScale = Mathf.Lerp(0f, this.beamHeight, t);
        extractionBeam.transform.localScale = new Vector3(this.beamWidth, yScale, this.beamWidth);

        // Smoothly move up by half the current height
        extractionBeam.transform.localPosition = new Vector3(0f, yScale, 0f);

        yield return null;
    }

    // Ensure final values are exact
    extractionBeam.transform.localScale = endScale;
    extractionBeam.transform.localPosition = new Vector3(0f, this.beamHeight, 0f);
    }

    private void OnDestroy()
    {
        if (this.bossSpawner != null)
            bossSpawner.BossDied -= OnBossDied;
    }



    private void OnDisplayEndGame()
    {
        Debug.Log("Spawner received DisplayEndGame event!");
    }

    private void OnBossDied()
    {
        this.isBossDead = true;
    }
}
