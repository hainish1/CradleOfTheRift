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


    public event Action<float> ChargeChanged;
    public event Action<ExtractionZone> ExtractionInteracted;
    public event Action ExtractionFinished;
    public float ChargeTime => this.chargeTime;

    [SerializeField] private GameObject extractionBeam;

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
        if (this.isExtracting & this.currentCharge < this.chargeTime)
        {
            this.currentCharge = Math.Clamp(this.currentCharge + Time.deltaTime, 0, this.chargeTime);

            if (this.currentCharge >= this.chargeTime && !this.hasFinishedExtracting && this.isBossDead)
            {
                this.hasFinishedExtracting = true;
                this.isExtracting = false;

                this.ExtractionFinished?.Invoke();
            }
        }
        else
        {
            if (!this.hasFinishedExtracting)
            {
                this.currentCharge = Math.Clamp(this.currentCharge - Time.deltaTime, 0, this.chargeTime);
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
