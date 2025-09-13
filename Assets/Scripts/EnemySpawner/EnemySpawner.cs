using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    private List<EnemyType> enemies;
    [SerializeField]
    private int maxEnemiesPerWave = 5;
    [SerializeField]
    private int EnemyWaveCapIncrease = 2;
    [SerializeField]
    private float timeBetweenWaves = 8f;
    [SerializeField]
    private float timeBetweenEnemySpawns = 1f;


    [SerializeField]
    private float creditGainRate = 1f;
    [SerializeField]
    private float difficultyScale = 1.01f;
    [SerializeField]
    private float credits = 5f;
    [SerializeField]
    private float maxCredits = 20f;
    [SerializeField]
    private Queue<EnemyType> enemiesToSpawn = new Queue<EnemyType>();


    [SerializeField]
    private Transform playerLocation;
    private float spawnRadius = 10f;


    private float waveCountdown;
    private float enemyCountdown;
    private bool isWaveInProgress = false;
    private int currentWave = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.waveCountdown = this.timeBetweenWaves;
        this.enemyCountdown = 0f;

        // Sort the list of enemies from cheapest to most expensive
        enemies.Sort((a, b) => { return a.cost - b.cost; });
    }

    // Update is called once per frame
    void Update()
    {
        this.credits += this.creditGainRate * Time.deltaTime;
        this.credits = Mathf.Min(this.credits, this.maxCredits);

        // Start a new wave after the countdown has finished
        if (!this.isWaveInProgress)
        {
            this.waveCountdown -= Time.deltaTime;

            if (this.waveCountdown <= 0f)
            {
                StartWave();
            }
        }

        // Spawn enemies until queue is empty
        if (this.enemiesToSpawn.Count > 0)
        {
            this.enemyCountdown -= Time.deltaTime;
            if (this.enemyCountdown <= 0f)
            {
                SpawnEnemy(this.enemiesToSpawn.Dequeue());
                this.enemyCountdown = this.timeBetweenEnemySpawns;
            }
        }

        // Reset properties when finishing a wave
        else if (this.isWaveInProgress)
        {
            EndWave();
        }
    }

    private void StartWave()
    {
        this.isWaveInProgress = true;

        // Have the first enemy spawn immediately
        this.enemyCountdown = 0f;
        this.currentWave++;
        GenerateWave();
    }

    private void EndWave()
    {
        this.isWaveInProgress = false;
        this.waveCountdown = this.timeBetweenWaves;

        this.creditGainRate *= this.difficultyScale;
        this.maxEnemiesPerWave += this.EnemyWaveCapIncrease;
        this.maxCredits *= this.difficultyScale;
    }

    private void GenerateWave()
    {
        int lowestCostEnemy = this.enemies[0].cost;
        int spawnedEnemies = 0;
        while (this.credits >= lowestCostEnemy && spawnedEnemies < this.maxEnemiesPerWave)
        {
            EnemyType randomEnemy = enemies[UnityEngine.Random.Range(0, enemies.Count)];

            if (this.credits >= randomEnemy.cost)
            {
                this.credits -= randomEnemy.cost;
                this.enemiesToSpawn.Enqueue(randomEnemy);
                spawnedEnemies++;
            }
        }
    }

    private void SpawnEnemy(EnemyType enemy)
    {
        Vector3 location = enemy.isFlying ? GetAirLocation() : GetGroundLocation();

        Instantiate(enemy.prefab, location, Quaternion.identity);
    }

    private Vector3 GetGroundLocation()
    {
        Vector2 locationOffset = UnityEngine.Random.insideUnitCircle * this.spawnRadius;
        Vector3 spawnLocation = this.playerLocation.position + new Vector3(locationOffset.x, 0, locationOffset.y);

        float heightOffset = 5f;
        float raycastLength = 40f;

        // Shoot a raycast down to determine the grounds Y position
        if (Physics.Raycast(spawnLocation + Vector3.up * heightOffset, Vector3.down, out RaycastHit hitInfo, raycastLength))
        {
            spawnLocation.y = hitInfo.point.y;
        }

        return spawnLocation;
    }

    private Vector3 GetAirLocation()
    {
        Vector3 locationOffset = UnityEngine.Random.onUnitSphere * this.spawnRadius;
        locationOffset.y = Math.Abs(locationOffset.y);

        return this.playerLocation.position + locationOffset;
    }
}

[Serializable]
public class EnemyType
{
    public string name;
    public GameObject prefab;
    public int cost;
    public bool isFlying;
}