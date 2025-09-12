using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    private List<EnemyType> enemies;
    [SerializeField]
    private float spawnInterval = 4f;
    [SerializeField]
    private float creditGainRate = 1f;
    [SerializeField]
    private float difficultyScale = 1.05f;
    [SerializeField]
    private int credits = 5;
    [SerializeField]
    private Transform playerLocation;
    [SerializeField]
    private float spawnRadius = 10f;
    private float spawnTimer;
    private float waveTimer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.spawnTimer = this.spawnInterval;
    }

    // Update is called once per frame
    void Update()
    {
        this.credits += (int)Math.Ceiling(this.creditGainRate * Time.deltaTime);
        this.creditGainRate *= Mathf.Pow(difficultyScale, Time.deltaTime);

        this.spawnTimer -= Time.deltaTime;
        if (this.spawnTimer <= 0f)
        {
            GenerateWave();

            this.spawnTimer = this.spawnInterval;
        }
    }

    private void GenerateWave()
    {
        List<EnemyType> generatedEnemies = new List<EnemyType>();

        while (this.credits > 0)
        {
            EnemyType randomEnemy = enemies[UnityEngine.Random.Range(0, enemies.Count)];

            if (this.credits >= randomEnemy.cost)
            {
                this.credits -= randomEnemy.cost;
                generatedEnemies.Add(randomEnemy);
            }

            else
            {
                continue;
            }
        }

        foreach (EnemyType enemy in generatedEnemies)
        {
            SpawnEnemy(enemy);
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
        Vector3 locationOffset = UnityEngine.Random.onUnitSphere  * this.spawnRadius;
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