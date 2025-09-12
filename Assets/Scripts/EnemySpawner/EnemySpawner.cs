using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    private List<EnemyType> enemies;
    [SerializeField]
    private float spawnInterval;
    [SerializeField]
    private float creditGainRate = 1f;
    [SerializeField]
    private float difficultyScale = 1.05f;
    [SerializeField]
    private int credits = 5;
    private float spawnTimer;
    private float waveTimer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        this.credits += (int)Math.Ceiling(this.creditGainRate * Time.deltaTime);

        GenerateWave();

        this.creditGainRate *= difficultyScale * Time.deltaTime;
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

    }
}

[Serializable]
public class EnemyType
{
    public string name;
    public GameObject prefab;
    public int cost;
}
