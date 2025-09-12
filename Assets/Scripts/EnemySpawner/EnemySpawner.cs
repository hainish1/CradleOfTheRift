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
    private float creditGainRate;
    [SerializeField]
    private float difficultyScale;
    private int enemyCredits;
    private float spawnTimer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void GenerateWave()
    { 
        
    }

    private void SpawnEnemy()
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
